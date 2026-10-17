using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace gameproject0
{
    public class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public bool IsActive;
        
        private Texture2D texture;
        private Color plasmaColor = Color.OrangeRed;

        public Bullet(Texture2D tex, Vector2 startPosition, Vector2 velocity)
        {
            texture = tex;
            Position = startPosition;
            Velocity = velocity;
            Lifetime = 2.0f; // Dissipate after 2 seconds
            IsActive = true;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * dt;
            Lifetime -= dt;

            if (Lifetime <= 0) 
            {
                IsActive = false;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Calculate the angle of the bullet based on its velocity to orient the plasma trail
            float angle = (float)Math.Atan2(Velocity.Y, Velocity.X);
            
            // Stretch the 1x1 pixel into a long, thin laser trail (e.g., 20px long, 3px thick)
            Vector2 scale = new Vector2(20f, 3f);
            
            spriteBatch.Draw(texture, Position, null, plasmaColor, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}