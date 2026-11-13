using System;
using MechaHorde.Gameplay.Collisions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MechaHorde.Gameplay
{
    /// <summary>
    /// A robot that comes for the player. Walkers stay on the ground and close in on the
    /// player's X; flyers move freely through the air.
    /// </summary>
    /// <remarks>
    /// All positions are in level pixels (the Battlefield scales the level to fit the screen). For walkers
    /// <see cref="Position"/> is where their feet touch the ground; for flyers it is their centre.
    /// </remarks>
    public class Enemy
    {
        public enum Type { drone, ranged, medium, heavy }

        /// <summary>
        /// The Y the walkers' feet stand on (where the player's feet are when on the ground)
        /// </summary>
        public const float GroundY = 97f;

        /// <summary>
        /// How high above the ground the ranged robots hover
        /// </summary>
        private const float HoverHeight = 45f;

        /// <summary>
        /// How far to the side of the player the ranged robots hover (so they can shoot later)
        /// </summary>
        private const float HoverStandoff = 70f;

        /// <summary>
        /// How long an enemy flashes red after it is hit
        /// </summary>
        private const float HitFlashTime = 0.08f;

        public Type EnemyType { get; }
        public Vector2 Position;

        public int Health { get; private set; }
        public int MaxHealth { get; }
        public bool IsAlive => Health > 0;
        public bool IsFlying => EnemyType == Type.drone || EnemyType == Type.ranged;

        private readonly EnemySprites _sprites;
        private readonly float _speed;
        private readonly float _stopDistance;

        private bool _facingLeft;
        private bool _moving;
        private float _animationTimer;
        private int _animationFrame;
        private float _hitFlash;
        private float _age;

        public Enemy(Type type, Vector2 startPosition, EnemySprites sprites)
        {
            EnemyType = type;
            Position = startPosition;
            _sprites = sprites;

            // Speeds are in level pixels per second (the player runs at about 120)
            (_speed, MaxHealth, _stopDistance) = type switch
            {
                Type.drone => (60f, 1, 0f),
                Type.ranged => (45f, 2, 2f),
                Type.medium => (35f, 3, 8f),
                Type.heavy => (18f, 8, 14f),
                _ => (40f, 1, 8f)
            };
            Health = MaxHealth;

            if (!IsFlying)
                Position.Y = GroundY;
        }

        /// <summary>
        /// The area bullets hit and the player touches, in level pixels
        /// </summary>
        public BoundingRectangle Bounds => EnemyType switch
        {
            Type.drone => new BoundingRectangle(Position.X - 3.25f, Position.Y - 6.25f, 6.5f, 12.5f),
            Type.ranged => new BoundingRectangle(Position.X - 5.5f, Position.Y - 7.25f, 11f, 14.5f),
            Type.medium => new BoundingRectangle(Position.X - 5.5f, Position.Y - 14.5f, 11f, 14.5f),
            Type.heavy => new BoundingRectangle(Position.X - 7f, Position.Y - 25f, 14f, 25f),
            _ => new BoundingRectangle(Position.X - 5f, Position.Y - 10f, 10f, 10f)
        };

        /// <summary>
        /// Damages the enemy
        /// </summary>
        public void Hit(int damage)
        {
            Health = Math.Max(0, Health - damage);
            _hitFlash = HitFlashTime;
        }

        public void Update(GameTime gameTime, Player player)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _age += dt;
            if (_hitFlash > 0) _hitFlash -= dt;

            // player.position is at the player's feet, so aim at the middle of their body
            Vector2 playerCenter = player.Center;

            switch (EnemyType)
            {
                case Type.drone:
                    // Fly straight at the player
                    MoveTowards(playerCenter, dt);
                    break;

                case Type.ranged:
                    // Hover off to the player's side, above the ground
                    float side = Position.X < playerCenter.X ? -1f : 1f;
                    var hoverSpot = new Vector2(playerCenter.X + side * HoverStandoff, GroundY - HoverHeight);
                    MoveTowards(hoverSpot, dt);
                    _facingLeft = playerCenter.X < Position.X;
                    break;

                default:
                    // Walk along the ground towards the player, stopping when close enough
                    Position.Y = GroundY;
                    float dx = playerCenter.X - Position.X;
                    _facingLeft = dx < 0;
                    _moving = Math.Abs(dx) > _stopDistance;
                    if (_moving)
                        Position.X += Math.Sign(dx) * Math.Min(_speed * dt, Math.Abs(dx) - _stopDistance);
                    break;
            }

            Animate(dt);
        }

        private void MoveTowards(Vector2 target, float dt)
        {
            Vector2 toTarget = target - Position;
            float distance = toTarget.Length();
            _moving = distance > _stopDistance;
            if (!_moving)
                return;

            float step = Math.Min(_speed * dt, distance);
            Position += toTarget / distance * step;
            if (EnemyType == Type.drone)
                _facingLeft = toTarget.X < 0;
        }

        private void Animate(float dt)
        {
            (float frameTime, int frameCount) = EnemyType switch
            {
                Type.drone => (0.12f, 5),    // red scanning eye
                Type.ranged => (0.08f, 2),   // jet flicker
                Type.medium => (0.25f, 2),   // stomping
                _ => (0f, 1)
            };
            if (frameCount <= 1)
                return;

            // Walkers stand still on their first frame
            if (!_moving && !IsFlying)
            {
                _animationFrame = 0;
                _animationTimer = 0;
                return;
            }

            _animationTimer += dt;
            while (_animationTimer >= frameTime)
            {
                _animationTimer -= frameTime;
                _animationFrame = (_animationFrame + 1) % frameCount;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Color color = _hitFlash > 0 ? Color.Red : Color.White;
            Texture2D texture;
            Rectangle source;
            Vector2 origin;
            float scale;
            Vector2 offset = Vector2.Zero;

            switch (EnemyType)
            {
                case Type.drone:
                    texture = _sprites.SecuriBot;
                    source = EnemySprites.Frame(5 + _animationFrame, EnemySprites.SecuriBotFrameWidth, EnemySprites.SecuriBotFrameHeight);
                    origin = new Vector2(6.5f, 14.5f);
                    scale = 0.5f;
                    offset.Y = MathF.Sin(_age * 5f) * 1.5f; // gentle float
                    break;

                case Type.ranged:
                    texture = _sprites.RobotGunner;
                    source = EnemySprites.Frame(3 + _animationFrame, EnemySprites.RobotGunnerFrameWidth, EnemySprites.RobotGunnerFrameHeight);
                    origin = new Vector2(30f, 33f); // its middle, since it's in the air
                    scale = 1f / 3f;
                    offset.Y = MathF.Sin(_age * 3f) * 2f;
                    break;

                case Type.medium:
                    texture = _sprites.RobotGunner;
                    source = EnemySprites.Frame(_animationFrame, EnemySprites.RobotGunnerFrameWidth, EnemySprites.RobotGunnerFrameHeight);
                    origin = new Vector2(30f, 54f); // feet
                    scale = 1f / 3f;
                    break;

                default: // heavy
                    texture = _sprites.GreenMech;
                    // The sheet has a left-facing and a right-facing stand frame, so no flipping
                    source = EnemySprites.Frame(_facingLeft ? 0 : 1, EnemySprites.GreenMechFrameWidth, EnemySprites.GreenMechFrameHeight);
                    origin = new Vector2(70f, 108f); // feet
                    scale = 0.25f;
                    if (_moving)
                        offset.Y = -MathF.Abs(MathF.Sin(_age * 6f)) * 1f; // heavy stomp
                    break;
            }

            spriteBatch.Draw(texture, Position + offset, source, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
