using System;
using System.Collections.Generic;
using MechaHorde.Gameplay.Collisions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MechaHorde.Gameplay;

/// <summary>
/// Sends the enemies in, in waves, from just off the edges of the screen.
/// </summary>
/// <remarks>
/// Each wave has a number of enemies to send, one every <see cref="SpawnInterval"/> seconds,
/// with no more than <see cref="MaxAlive"/> on the field at once. When they have all been sent
/// and destroyed there is a short break, then the next (bigger, faster) wave starts. Which
/// enemies a wave can contain is decided in <see cref="PickType"/>.
/// All positions are in level pixels.
/// </remarks>
public class EnemySpawner
{
    /// <summary>
    /// Seconds before the first wave starts
    /// </summary>
    private const float FirstWaveDelay = 2f;

    /// <summary>
    /// Seconds between the end of one wave and the start of the next
    /// </summary>
    private const float TimeBetweenWaves = 4f;

    /// <summary>
    /// How far past the edge of the screen enemies appear (so even the mech starts hidden)
    /// </summary>
    private const float SpawnMargin = 40f;

    private readonly EnemySprites _sprites = new();
    private readonly List<Enemy> _enemies = new();
    private readonly float _levelLeft;
    private readonly float _levelRight;

    private int _leftToSpawn;
    private float _spawnTimer;
    private float _breakTimer = FirstWaveDelay;

    /// <param name="levelLeft">The left edge of the level</param>
    /// <param name="levelRight">The right edge of the level</param>
    public EnemySpawner(float levelLeft, float levelRight)
    {
        _levelLeft = levelLeft;
        _levelRight = levelRight;
    }

    /// <summary>
    /// The current wave (0 until the first one starts)
    /// </summary>
    public int Wave { get; private set; }

    /// <summary>
    /// The enemies on the field
    /// </summary>
    public IReadOnlyList<Enemy> Enemies => _enemies;

    /// <summary>
    /// The enemies left in this wave: those on the field and those still to come
    /// </summary>
    public int EnemiesRemaining => _enemies.Count + _leftToSpawn;

    /// <summary>
    /// How many enemies the player has destroyed
    /// </summary>
    public int Kills { get; private set; }

    /// <summary>
    /// True between waves (and before the first)
    /// </summary>
    public bool IsBetweenWaves => _breakTimer > 0;

    /// <summary>
    /// Seconds until the next wave starts (0 during a wave)
    /// </summary>
    public float TimeUntilNextWave => Math.Max(0, _breakTimer);

    /// <summary>
    /// Raised when an enemy is destroyed (e.g. for a score, sounds or the gib sprites)
    /// </summary>
    public event Action<Enemy> EnemyDestroyed;

    private float SpawnInterval => Math.Max(0.35f, 1.5f - 0.1f * (Wave - 1));
    private int MaxAlive => Math.Min(20, 5 + Wave);

    public void LoadContent(ContentManager content) => _sprites.LoadContent(content);

    /// <summary>
    /// Runs the waves, moves the enemies, and checks for the player's bullets hitting them
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="player">The player the enemies chase</param>
    /// <param name="viewLeft">The X of the left edge of the screen, in level pixels</param>
    /// <param name="viewRight">The X of the right edge of the screen, in level pixels</param>
    /// <param name="bullets">The player's bullets</param>
    public void Update(GameTime gameTime, Player player, float viewLeft, float viewRight, IList<Bullet> bullets)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Between waves: count down to the next one
        if (_breakTimer > 0)
        {
            _breakTimer -= dt;
            if (_breakTimer <= 0)
                StartWave(Wave + 1);
        }
        // During a wave: send in the next enemy when it's time (and there's room)
        else if (_leftToSpawn > 0)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0 && _enemies.Count < MaxAlive)
            {
                Spawn(PickType(), viewLeft, viewRight);
                _leftToSpawn--;
                _spawnTimer = SpawnInterval;
            }
        }

        foreach (var enemy in _enemies)
            enemy.Update(gameTime, player);

        CheckBullets(bullets, dt);

        // Remove the destroyed enemies
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (!_enemies[i].IsAlive)
            {
                var enemy = _enemies[i];
                _enemies.RemoveAt(i);
                Kills++;
                EnemyDestroyed?.Invoke(enemy);
            }
        }

        // The wave is over once everything has been sent and destroyed
        if (Wave > 0 && _breakTimer <= 0 && EnemiesRemaining == 0)
            _breakTimer = TimeBetweenWaves;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        foreach (var enemy in _enemies)
            enemy.Draw(gameTime, spriteBatch);
    }

    /// <summary>
    /// The enemies touching <paramref name="bounds"/> (e.g. the player's, for damage)
    /// </summary>
    public IEnumerable<Enemy> EnemiesTouching(BoundingRectangle bounds)
    {
        foreach (var enemy in _enemies)
            if (enemy.IsAlive && enemy.Bounds.CollidesWith(bounds))
                yield return enemy;
    }

    private void StartWave(int wave)
    {
        Wave = wave;
        _leftToSpawn = 4 + 3 * (wave - 1);
        _spawnTimer = 0;
    }

    /// <summary>
    /// Picks the type of the next enemy. Later waves bring in tougher enemies, more often.
    /// </summary>
    private Enemy.Type PickType()
    {
        int drone = 4;
        int medium = 3;
        int ranged = Wave >= 2 ? 2 : 0;
        int heavy = Wave >= 3 ? 1 + (Wave - 3) / 2 : 0;

        int roll = RandomHelper.Next(drone + medium + ranged + heavy);
        if ((roll -= drone) < 0) return Enemy.Type.drone;
        if ((roll -= medium) < 0) return Enemy.Type.medium;
        if ((roll -= ranged) < 0) return Enemy.Type.ranged;
        return Enemy.Type.heavy;
    }

    private void Spawn(Enemy.Type type, float viewLeft, float viewRight)
    {
        // Come in from whichever side has level to come from (or either, when both do)
        bool canLeft = viewLeft > _levelLeft + 1f;
        bool canRight = viewRight < _levelRight - 1f;
        bool fromLeft = canLeft == canRight ? RandomHelper.Next(2) == 0 : canLeft;

        float x = fromLeft ? viewLeft - SpawnMargin : viewRight + SpawnMargin;
        float y = type switch
        {
            Enemy.Type.drone => RandomHelper.NextFloat(-30f, 60f),
            Enemy.Type.ranged => RandomHelper.NextFloat(20f, 60f),
            _ => Enemy.GroundY
        };

        _enemies.Add(new Enemy(type, new Vector2(x, y), _sprites));
    }

    /// <summary>
    /// Damages the enemies the player's bullets hit, and stops those bullets
    /// </summary>
    private void CheckBullets(IList<Bullet> bullets, float dt)
    {
        foreach (var bullet in bullets)
        {
            if (!bullet.IsActive)
                continue;

            // Bullets move about 20 level pixels a frame, which is more than a drone is wide,
            // so test the path the bullet took this frame rather than just where it is now
            Vector2 from = bullet.Position - bullet.Velocity * dt;
            foreach (var enemy in _enemies)
            {
                if (enemy.IsAlive && SegmentHits(from, bullet.Position, enemy.Bounds))
                {
                    enemy.Hit(1);
                    bullet.IsActive = false;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Whether the line from <paramref name="a"/> to <paramref name="b"/> passes through
    /// <paramref name="r"/> (the Liang-Barsky clipping test)
    /// </summary>
    private static bool SegmentHits(Vector2 a, Vector2 b, BoundingRectangle r)
    {
        Vector2 d = b - a;
        float tMin = 0f, tMax = 1f;

        bool Clip(float p, float q)
        {
            if (p == 0) return q >= 0;
            float t = q / p;
            if (p < 0) { if (t > tMax) return false; if (t > tMin) tMin = t; }
            else { if (t < tMin) return false; if (t < tMax) tMax = t; }
            return true;
        }

        return Clip(-d.X, a.X - r.Left) && Clip(d.X, r.Right - a.X)
            && Clip(-d.Y, a.Y - r.Top) && Clip(d.Y, r.Bottom - a.Y);
    }
}
