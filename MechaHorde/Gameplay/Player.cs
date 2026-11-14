using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MechaHorde.Gameplay.Collisions;

namespace MechaHorde.Gameplay;

/// <summary>
/// The player: Space Sara. Runs, jumps, crouches and aims her gun at the mouse.
/// </summary>
/// <remarks>
/// <para>
/// All positions and speeds are in level pixels (the Battlefield scales the level to fit the screen).
/// <see cref="position"/> is where Sara's feet touch the ground, horizontally centred on her body.
/// </para>
/// <para>
/// Movement is velocity based and frame-rate independent: horizontal speed accelerates and
/// decelerates instead of snapping, and jumps use a buffered press, a shorter hop when the
/// button is released early, and stronger gravity on the way down so they feel snappy.
/// </para>
/// <para>
/// Sprites ("Space Sara" by Emcee Flesher, CC-BY 3.0) are 48x48 cells in two sheets:
/// <list type="bullet">
/// <item>SaraMain - one animation per row (idle, run, jumps, falls, landing, ...)</item>
/// <item>SaraShooting - one pose per column (0 stand, 1 crouch, 2-11 run, 12 wall cling,
/// 13-16 fall), and two rows per aim angle (without / with muzzle flash), from straight down
/// (rows 0-1) to straight up (rows 16-17)</item>
/// </list>
/// Sara faces right in both sheets; she is flipped to face left.
/// </para>
/// </remarks>
public class Player
{
    public enum PlayerState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Land,
        Crouch
    }

    #region Tuning

    /// <summary>Top running speed (the enemies are tuned around about 120)</summary>
    private const float MaxRunSpeed = 120f;
    private const float GroundAcceleration = 1100f;
    private const float GroundDeceleration = 1500f;
    /// <summary>Extra push when changing direction on the ground, so turns feel responsive</summary>
    private const float TurnAcceleration = 2200f;
    private const float AirAcceleration = 750f;
    private const float AirDeceleration = 300f;

    private const float JumpVelocity = -215f;
    private const float Gravity = 750f;
    /// <summary>Gravity is multiplied by this when falling, for a less floaty arc</summary>
    private const float FallGravityMultiplier = 1.6f;
    /// <summary>Releasing jump while rising multiplies the upward speed by this (short hops)</summary>
    private const float JumpCutMultiplier = 0.45f;
    private const float MaxFallSpeed = 400f;
    /// <summary>A jump pressed this long before landing still happens</summary>
    private const float JumpBufferTime = 0.12f;

    /// <summary>Sara stays in her aiming poses this long after firing, then relaxes</summary>
    private const float CombatPoseTime = 1.5f;
    /// <summary>How long the muzzle flash frame shows after a shot</summary>
    private const float MuzzleFlashTime = 0.07f;

    /// <summary>The level pixels covered by one run frame, so her feet don't slide</summary>
    private const float StrideLength = MaxRunSpeed / 12.5f;

    #endregion

    #region Sprite layout

    private const int Cell = 48;
    /// <summary>Sara's feet, within a cell: the bottom edge of her soles, so they rest exactly on the ground</summary>
    private static readonly Vector2 Origin = new(24, 48);
    /// <summary>The 48px sprite is drawn at 1/3 scale: 16 level pixels tall</summary>
    public const float SpriteScale = 1f / 3f;

    // SaraMain rows
    private const int RowIdle = 0, RowRunLoop = 4, RowRunningJump = 6, RowJumpLoop = 8,
        RowFallLoop = 11, RowLand = 12;

    // SaraShooting columns
    private const int ColStand = 0, ColCrouch = 1, ColRun = 2, ColFall = 13;

    /// <summary>
    /// Where the gun's muzzle is relative to Sara's feet (in sprite pixels, facing right) for
    /// each of the 9 aim angles, measured from the muzzle flashes on the shooting sheet
    /// </summary>
    private static readonly Vector2[] StandMuzzle =
    {
        new(4, -10), new(10, -12), new(14, -16), new(17, -22), new(17, -27),
        new(16, -33), new(13, -37), new(8, -40), new(3, -40)
    };
    private static readonly Vector2[] RunMuzzle =
    {
        new(0, -10), new(7, -12), new(12, -15), new(15, -21), new(16, -25),
        new(15, -32), new(12, -36), new(6, -39), new(1, -40)
    };
    private static readonly Vector2[] CrouchMuzzle =
    {
        new(17, -13), new(17, -13), new(17, -13), new(17, -13), new(17, -13),
        new(16, -20), new(13, -24), new(7, -28), new(2, -29)
    };

    /// <summary>Where Sara aims from: her shoulder, above her feet (level pixels)</summary>
    private const float ShoulderHeight = 9f;

    #endregion

    /// <summary>
    /// The Y Sara's feet stand on: the same ground the walking robots use
    /// </summary>
    public const float GroundY = Enemy.GroundY;

    private Texture2D _mainSheet;
    private Texture2D _shootingSheet;

    private KeyboardState _keyboard;
    private KeyboardState _priorKeyboard;

    /// <summary>Where Sara's feet are, in level pixels</summary>
    public Vector2 position = new(60, GroundY);

    private Vector2 _velocity;
    private bool _grounded = true;
    private bool _jumpHeld;
    private float _jumpBuffer;
    private float _airTime;

    private bool _facingLeft;
    private Vector2 _aimTarget;
    private int _aimIndex = 4;
    private bool _aimLeft;

    private float _sinceShot = CombatPoseTime;
    private float _flashTimer;

    private float _animationTime;
    private float _runDistance;
    private float _landTimer;
    private int _stepCount;

    public PlayerState State { get; private set; } = PlayerState.Idle;

    /// <summary>
    /// Raised when one of Sara's feet comes down while she runs (for footstep sounds)
    /// </summary>
    public event Action Footstep;

    /// <summary>
    /// Raised when Sara lands after a jump or a fall
    /// </summary>
    public event Action Landed;

    public Vector2 Velocity => _velocity;

    public bool IsGrounded => _grounded;

    /// <summary>1 when facing right, -1 when facing left</summary>
    public int Facing => _facingLeft ? -1 : 1;

    /// <summary>The middle of Sara's body, for enemies to aim at</summary>
    public Vector2 Center => position + new Vector2(0, State == PlayerState.Crouch ? -4f : -6.5f);

    /// <summary>The area enemies hit, in level pixels</summary>
    public BoundingRectangle Bounds => State == PlayerState.Crouch
        ? new BoundingRectangle(position.X - 4f, position.Y - 8f, 8f, 8f)
        : new BoundingRectangle(position.X - 4f, position.Y - 13f, 8f, 13f);

    /// <summary>The color overlay</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>True while Sara is in her aiming poses (she faces the mouse)</summary>
    private bool InCombat => _sinceShot < CombatPoseTime || State == PlayerState.Crouch;

    /// <summary>The unit direction from Sara's shoulder to where she is aiming</summary>
    public Vector2 AimDirection
    {
        get
        {
            Vector2 d = _aimTarget - Shoulder;
            return d.LengthSquared() < 0.0001f ? new Vector2(Facing, 0) : Vector2.Normalize(d);
        }
    }

    private Vector2 Shoulder => position - new Vector2(0, State == PlayerState.Crouch ? 5f : ShoulderHeight);

    /// <summary>
    /// Where bullets come out of the gun, for the pose Sara is in (or is about to be in, if she
    /// fires this frame)
    /// </summary>
    public Vector2 MuzzlePosition
    {
        get
        {
            Vector2[] table = State switch
            {
                PlayerState.Crouch => CrouchMuzzle,
                PlayerState.Run or PlayerState.Jump or PlayerState.Fall => RunMuzzle,
                _ => StandMuzzle
            };
            int index = State == PlayerState.Crouch ? Math.Max(_aimIndex, 4) : _aimIndex;
            Vector2 offset = table[index];
            if (_aimLeft) offset.X = -offset.X;
            return position + offset * SpriteScale;
        }
    }

    /// <summary>
    /// Loads the sprite sheets
    /// </summary>
    public void LoadContent(ContentManager content)
    {
        _mainSheet = content.Load<Texture2D>("Textures/Sprites/Sara/SaraMain");
        _shootingSheet = content.Load<Texture2D>("Textures/Sprites/Sara/SaraShooting");
    }

    /// <summary>
    /// Call when the gun fires, so Sara shows the muzzle flash and takes her aiming pose
    /// </summary>
    public void NotifyShot()
    {
        _sinceShot = 0f;
        _flashTimer = MuzzleFlashTime;
        _facingLeft = _aimLeft;
    }

    /// <summary>
    /// Keeps Sara inside the level, stopping her against the edges
    /// </summary>
    public void ClampX(float min, float max)
    {
        if (position.X < min) { position.X = min; if (_velocity.X < 0) _velocity.X = 0; }
        if (position.X > max) { position.X = max; if (_velocity.X > 0) _velocity.X = 0; }
    }

    /// <summary>
    /// Moves Sara and advances her animation
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="aimTarget">Where she aims, in level pixels (the mouse)</param>
    public void Update(GameTime gameTime, Vector2 aimTarget)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _priorKeyboard = _keyboard;
        _keyboard = Keyboard.GetState();

        bool left = Down(Keys.A) || Down(Keys.Left);
        bool right = Down(Keys.D) || Down(Keys.Right);
        bool crouch = Down(Keys.S) || Down(Keys.Down);
        bool jumpDown = Down(Keys.W) || Down(Keys.Up) || Down(Keys.Space);
        bool jumpPressed = Pressed(Keys.W) || Pressed(Keys.Up) || Pressed(Keys.Space);

        float input = (right ? 1f : 0f) - (left ? 1f : 0f);

        // ---- Aim ----
        _aimTarget = aimTarget;
        Vector2 toAim = aimTarget - Shoulder;
        _aimLeft = toAim.X < 0;
        float angle = MathF.Atan2(-toAim.Y, MathF.Abs(toAim.X)); // -90 (down) .. 90 (up) degrees, in radians
        _aimIndex = Math.Clamp((int)MathF.Round((MathHelper.ToDegrees(angle) + 90f) / 22.5f), 0, 8);

        // ---- Horizontal movement ----
        bool crouching = _grounded && crouch;
        float targetSpeed = crouching ? 0f : input * MaxRunSpeed;
        float rate;
        if (_grounded)
        {
            if (input != 0 && Math.Sign(input) != Math.Sign(_velocity.X) && _velocity.X != 0) rate = TurnAcceleration;
            else rate = targetSpeed != 0 ? GroundAcceleration : GroundDeceleration;
        }
        else
        {
            rate = input != 0 ? AirAcceleration : AirDeceleration;
        }
        _velocity.X = Approach(_velocity.X, targetSpeed, rate * dt);

        // ---- Jumping ----
        if (jumpPressed) _jumpBuffer = JumpBufferTime;
        else _jumpBuffer = Math.Max(0f, _jumpBuffer - dt);

        if (_jumpBuffer > 0 && _grounded && !crouching)
        {
            _velocity.Y = JumpVelocity;
            _grounded = false;
            _jumpHeld = true;
            _jumpBuffer = 0;
            _airTime = 0;
            _landTimer = 0;
        }

        // Let go early for a short hop
        if (_jumpHeld && !jumpDown)
        {
            _jumpHeld = false;
            if (_velocity.Y < 0) _velocity.Y *= JumpCutMultiplier;
        }

        // ---- Gravity ----
        if (!_grounded)
        {
            _airTime += dt;
            float g = _velocity.Y > 0 ? Gravity * FallGravityMultiplier : Gravity;
            _velocity.Y = Math.Min(_velocity.Y + g * dt, MaxFallSpeed);
        }

        // ---- Move ----
        position += _velocity * dt;

        if (position.Y >= GroundY)
        {
            position.Y = GroundY;
            if (!_grounded)
            {
                _grounded = true;
                _jumpHeld = false;
                if (_airTime > 0.1f) Landed?.Invoke();
                // Only show the landing animation after a real fall
                if (_airTime > 0.25f) _landTimer = 4 / 16.5f;
            }
            _velocity.Y = 0;
        }

        // ---- Timers ----
        _sinceShot += dt;
        _flashTimer = Math.Max(0f, _flashTimer - dt);
        _landTimer = Math.Max(0f, _landTimer - dt);
        if (_grounded) _runDistance += MathF.Abs(_velocity.X) * dt;

        // ---- State ----
        var previous = State;
        if (!_grounded)
            State = _velocity.Y < 0 ? PlayerState.Jump : PlayerState.Fall;
        else if (crouching)
            State = PlayerState.Crouch;
        else if (MathF.Abs(_velocity.X) > 8f || input != 0)
            State = PlayerState.Run;
        else if (_landTimer > 0)
            State = PlayerState.Land;
        else
            State = PlayerState.Idle;

        // Any movement cancels the landing animation
        if (State != PlayerState.Land) _landTimer = 0;

        // The run cycle is 10 frames, one per stride, with a foot coming down every 5
        int steps = (int)(_runDistance / (StrideLength * 5f));
        if (steps != _stepCount && State == PlayerState.Run)
            Footstep?.Invoke();
        _stepCount = steps;

        if (State != previous && !(previous == PlayerState.Jump && State == PlayerState.Fall))
            _animationTime = 0;
        _animationTime += dt;

        // ---- Facing ----
        // In combat she faces where she aims (and may run backwards); otherwise where she runs
        if (InCombat) _facingLeft = _aimLeft;
        else if (input != 0) _facingLeft = input < 0;
    }

    /// <summary>
    /// Draws Sara
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch to draw with (already begun with the level's transform)</param>
    /// <param name="pixelsPerUnit">How many screen pixels one level pixel covers, to snap to whole pixels</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, float pixelsPerUnit)
    {
        var (sheet, source) = CurrentFrame();
        var effects = _facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        // Snap to whole screen pixels so the pixel art doesn't shimmer as she moves
        Vector2 drawPosition = new(
            MathF.Round(position.X * pixelsPerUnit) / pixelsPerUnit,
            MathF.Round(position.Y * pixelsPerUnit) / pixelsPerUnit);

        // Flipped sprites mirror around the cell, so mirror the origin too
        Vector2 origin = _facingLeft ? new Vector2(Cell - Origin.X, Origin.Y) : Origin;

        spriteBatch.Draw(sheet, drawPosition, source, Color, 0f, origin, SpriteScale, effects, 0f);
    }

    /// <summary>
    /// The sheet and cell for the pose and animation frame Sara is in
    /// </summary>
    private (Texture2D, Rectangle) CurrentFrame()
    {
        bool flash = _flashTimer > 0;
        int aimRow = _aimIndex * 2 + (flash ? 1 : 0);

        switch (State)
        {
            case PlayerState.Crouch:
                // She can't aim down while crouched: those cells are empty
                return Shooting(ColCrouch, Math.Max(_aimIndex, 4) * 2 + (flash ? 1 : 0));

            case PlayerState.Run:
            {
                // Frames follow the distance covered, so her feet match her speed
                int frame = (int)(_runDistance / StrideLength) % 10;
                if (InCombat)
                {
                    // Running away from where she aims: play the run backwards
                    bool backwards = _velocity.X != 0 && Math.Sign(_velocity.X) != Facing;
                    if (backwards) frame = 9 - frame;
                    return Shooting(ColRun + frame, aimRow);
                }
                return Main(frame, RowRunLoop);
            }

            case PlayerState.Jump:
            case PlayerState.Fall:
            {
                if (InCombat)
                    return Shooting(ColFall + Loop(4, 12.5f), aimRow);
                if (_airTime < 4 / 12.5f && State == PlayerState.Jump)
                    return Main(Once(4, 12.5f), RowRunningJump);
                if (_velocity.Y < 80f)
                    return Main(Loop(4, 12.5f), RowJumpLoop);
                return Main(Loop(4, 12.5f), RowFallLoop);
            }

            case PlayerState.Land:
                if (InCombat) return Shooting(ColStand, aimRow);
                return Main(Once(4, 16.5f), RowLand);

            default:
                if (InCombat) return Shooting(ColStand, aimRow);
                return Main(Loop(4, 4.2f), RowIdle);
        }
    }

    private int Loop(int frames, float fps) => (int)(_animationTime * fps) % frames;

    private int Once(int frames, float fps) => Math.Min((int)(_animationTime * fps), frames - 1);

    private (Texture2D, Rectangle) Main(int column, int row) =>
        (_mainSheet, new Rectangle(column * Cell, row * Cell, Cell, Cell));

    private (Texture2D, Rectangle) Shooting(int column, int row) =>
        (_shootingSheet, new Rectangle(column * Cell, row * Cell, Cell, Cell));

    private bool Down(Keys key) => _keyboard.IsKeyDown(key);

    private bool Pressed(Keys key) => _keyboard.IsKeyDown(key) && !_priorKeyboard.IsKeyDown(key);

    private static float Approach(float value, float target, float step) =>
        value < target ? Math.Min(value + step, target) : Math.Max(value - step, target);
}
