using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace gameproject0
{
    public class Gun
    {
        public List<Bullet> ActiveBullets = new List<Bullet>();
        
        private Texture2D bulletTexture;
        private float fireRate = 0.15f; // Time between shots
        private float cooldownTimer = 0f;
        private float bulletSpeed = 1200f; // Fast plasma
        
        private MouseState priorMouseState;

        public Gun(Texture2D baseBulletTexture)
        {
            bulletTexture = baseBulletTexture;
        }

public void Update(GameTime gameTime, Vector2 playerPosition, Vector2 targetPosition)
{
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (cooldownTimer > 0) cooldownTimer -= dt;

    MouseState currentMouseState = Mouse.GetState();

    // Check for left click and fire rate cooldown
    if (currentMouseState.LeftButton == ButtonState.Pressed && priorMouseState.LeftButton == ButtonState.Released && cooldownTimer <= 0)
    {
        Shoot(playerPosition, targetPosition);
        cooldownTimer = fireRate;
    }

    priorMouseState = currentMouseState;

    // Update all bullets and remove dead ones natively
    for (int i = ActiveBullets.Count - 1; i >= 0; i--)
    {
        ActiveBullets[i].Update(gameTime);
        if (!ActiveBullets[i].IsActive)
        {
            ActiveBullets.RemoveAt(i);
        }
    }
}

private void Shoot(Vector2 startPos, Vector2 targetPos)
{
    // Adjust spawn so the bullet comes out of the gun/chest, not the player's feet
    Vector2 gunBarrelOffset = new Vector2(0, -15); 
    Vector2 actualStartPos = startPos + gunBarrelOffset;

    // Calculate the direction from the gun to the mouse
    Vector2 direction = targetPos - actualStartPos;
    
    // Normalize ensures the bullet always travels at the exact bulletSpeed regardless of how far away the mouse is
    if (direction != Vector2.Zero)
    {
        direction.Normalize();
    }
    else
    {
        direction = new Vector2(1, 0); // Fallback to shooting right
    }
    
    Bullet newBullet = new Bullet(bulletTexture, actualStartPos, direction * bulletSpeed);
    ActiveBullets.Add(newBullet);
}

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var bullet in ActiveBullets)
            {
                bullet.Draw(spriteBatch);
            }
        }
    }
}