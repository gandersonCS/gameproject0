using System;
using Microsoft.Xna.Framework;

namespace MechaHorde.Gameplay;

/// <summary>
/// A piece of Scraplight lying in the level (or flying off a robot): a 3D model that tumbles
/// out, bounces on the floor, then bobs and spins until the player picks it up.
/// </summary>
/// <remarks>
/// Positions are in level pixels, like everything else in the level. <see cref="Position"/> is
/// the model's centre. The <see cref="PickupSystem"/> moves and draws pickups.
/// </remarks>
public class Pickup
{
    /// <summary>Downwards acceleration while falling (level pixels per second squared)</summary>
    private const float Gravity = 260f;

    /// <summary>How much of its speed a pickup keeps when it bounces off the floor</summary>
    private const float Bounciness = 0.45f;

    /// <summary>How much of its sideways speed a pickup keeps each bounce</summary>
    private const float Friction = 0.6f;

    /// <summary>How long a pickup lies there before it disappears (seconds)</summary>
    public const float Lifetime = 20f;

    /// <summary>A pickup blinks for this long before disappearing (seconds)</summary>
    private const float BlinkTime = 4f;

    /// <summary>Within this distance of the player, a landed pickup is pulled towards them</summary>
    private const float MagnetRadius = 26f;

    /// <summary>How fast a pickup flies to the player at the edge of the magnet's reach</summary>
    private const float MagnetSpeed = 90f;

    /// <summary>Within this distance of the player's centre, a pickup is collected</summary>
    private const float CollectRadius = 7f;

    /// <summary>Pickups can't be collected for this long after they drop, so they're seen flying out</summary>
    private const float CollectDelay = 0.35f;

    public PickupKind Kind { get; }

    /// <summary>
    /// Which model a spark uses (an index into the <see cref="PickupSystem"/>'s spark styles).
    /// Every style is worth the same; they're just different looking robot hearts.
    /// </summary>
    public int Style { get; }

    /// <summary>The model's centre, in level pixels</summary>
    public Vector2 Position;

    /// <summary>How big the model is drawn (its largest side, in level pixels)</summary>
    public float Size { get; }

    public bool IsCollected { get; private set; }
    public bool IsExpired => _age >= Lifetime;

    /// <summary>Whether to draw it this frame (it blinks just before it disappears)</summary>
    public bool IsVisible => _age < Lifetime - BlinkTime || MathF.Sin(_age * 25f) > -0.3f;

    /// <summary>The model's rotation (a tumbling spin while flying, a steady one once landed)</summary>
    public Matrix Rotation => Matrix.CreateRotationX(_tilt) * Matrix.CreateRotationZ(_roll) * Matrix.CreateRotationY(_spin);

    /// <summary>
    /// How far it is floating above its resting place (level pixels, negative is up). Once
    /// landed, scrap bobs gently near the floor; a spark rises up and floats up and down like a
    /// classic pickup.
    /// </summary>
    public float Bob
    {
        get
        {
            if (!_landed)
                return 0f;
            float rise = MathF.Min(1f, _landedTime * 3f);
            return Kind == PickupKind.Spark
                ? rise * (-4f + MathF.Sin(_age * 2.5f + _phase) * 1.5f)
                : rise * (-1f + MathF.Sin(_age * 3f + _phase) * 0.6f);
        }
    }

    /// <summary>Seconds since it dropped (used for the spark's glow pulse)</summary>
    public float Age => _age;

    private Vector2 _velocity;
    private bool _landed;
    private float _landedTime;
    private float _age;
    private float _spin;
    private float _tilt;
    private float _roll;
    private readonly float _spinSpeed;
    private readonly float _tumbleSpeed;
    private readonly float _phase;
    private readonly float _restingTilt;

    /// <param name="kind">What it is</param>
    /// <param name="position">Where it drops from (level pixels)</param>
    /// <param name="velocity">How fast it flies out (level pixels per second)</param>
    /// <param name="style">Which model a spark uses</param>
    public Pickup(PickupKind kind, Vector2 position, Vector2 velocity, int style = 0)
    {
        Kind = kind;
        Style = style;
        Position = position;
        _velocity = velocity;
        // Small, like coins: Sara is 16 level pixels tall
        Size = kind == PickupKind.Spark ? 3.5f : 2.75f;

        _spin = RandomHelper.NextFloat(0f, MathHelper.TwoPi);
        _tilt = RandomHelper.NextFloat(0f, MathHelper.TwoPi);
        _roll = RandomHelper.NextFloat(0f, MathHelper.TwoPi);
        _spinSpeed = RandomHelper.NextFloat(1.2f, 2.2f) * (RandomHelper.Next(2) == 0 ? -1 : 1);
        _tumbleSpeed = RandomHelper.NextFloat(6f, 12f);
        _phase = RandomHelper.NextFloat(0f, MathHelper.TwoPi);
        // Once landed, scrap lies tipped towards the camera so its shape reads; the spark sits upright
        _restingTilt = kind == PickupKind.Spark ? 0.35f : RandomHelper.NextFloat(0.6f, 1.0f);
    }

    /// <summary>
    /// Moves the pickup, and collects it if it reaches the player
    /// </summary>
    /// <returns>True the frame it is collected</returns>
    public bool Update(float dt, Vector2 playerCenter)
    {
        if (IsCollected)
            return false;
        _age += dt;

        Vector2 toPlayer = playerCenter - Position;
        float distance = toPlayer.Length();

        if (_age >= CollectDelay && distance < CollectRadius)
        {
            IsCollected = true;
            return true;
        }

        if (_landed && distance < MagnetRadius && distance > 0.01f)
        {
            // Pulled in, faster the closer it gets
            float pull = MagnetSpeed * (1.5f - distance / MagnetRadius);
            Position += toPlayer / distance * Math.Min(pull * dt, distance);
            _spin += _spinSpeed * 3f * dt;
            return false;
        }

        if (!_landed)
        {
            // Flying: fall, tumble, and bounce on the floor until it settles
            _velocity.Y += Gravity * dt;
            Position += _velocity * dt;
            _tilt += _tumbleSpeed * dt;
            _roll += _tumbleSpeed * 0.6f * dt;
            _spin += _spinSpeed * 2f * dt;

            float floor = Enemy.GroundY - Size / 2f;
            if (Position.Y >= floor)
            {
                Position.Y = floor;
                if (_velocity.Y > 30f)
                {
                    _velocity.Y = -_velocity.Y * Bounciness;
                    _velocity.X *= Friction;
                }
                else
                {
                    _landed = true;
                    _velocity = Vector2.Zero;
                }
            }
        }
        else
        {
            // Landed: rise into its float, settle into a slow spin, ease the tumble out
            _landedTime += dt;
            _spin += _spinSpeed * dt;
            float settle = 1f - MathF.Exp(-6f * dt);
            _tilt = MathHelper.Lerp(WrapNear(_tilt, _restingTilt), _restingTilt, settle);
            _roll = MathHelper.Lerp(WrapNear(_roll, 0f), 0f, settle);
        }
        return false;
    }

    /// <summary>
    /// <paramref name="angle"/> moved by whole turns to within half a turn of <paramref name="target"/>,
    /// so easing towards the target doesn't spin the long way round
    /// </summary>
    private static float WrapNear(float angle, float target) => target + MathHelper.WrapAngle(angle - target);
}
