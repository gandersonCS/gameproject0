using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gameproject0
{
    public class Enemy
    {
        public enum Type { drone, ranged, medium, heavy }
        
        public Type EnemyType { get; private set; }
        public Vector2 Position;
        private Texture2D texture;
        private float speed;
        private float groundLevel = 150f; // Matches the player's ground floor

        public Enemy(Type type, Vector2 startPosition, Texture2D tex)
        {
            EnemyType = type;
            Position = startPosition;
            texture = tex;
            
            // Drones are slightly faster than heavy walkers
            speed = (type == Type.drone) ? 80f : 50f; 
        }

        public void Update(GameTime gameTime, Player player)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (EnemyType == Type.drone)
            {
                // Drone Logic: Fly directly at the player's X and Y
                Vector2 direction = player.position - Position;
                
                if (direction != Vector2.Zero)
                {
                    direction.Normalize(); // Ensure diagonal movement isn't faster
                    Position += direction * speed * deltaTime;
                }
            }
            else
            {
                // Walker Logic: Lock to the ground, move horizontally toward player's X
                Position.Y = groundLevel;
                
                if (Position.X < player.position.X)
                    Position.X += speed * deltaTime;
                else if (Position.X > player.position.X)
                    Position.X -= speed * deltaTime;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // For now, draw the raw texture. 
            // You can add source rectangles for the "image_eadf82.png" sprite sheet animation later.
            spriteBatch.Draw(texture, Position, Color.White);
        }
    }
}