using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace MechaHorde.Gameplay
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

/// <summary>
/// Updates the gun and its bullets
/// </summary>
/// <param name="gameTime">The game time</param>
/// <param name="muzzlePosition">Where bullets come out of the gun</param>
/// <param name="direction">The unit direction to fire in</param>
/// <returns>True if the gun fired this frame</returns>
public bool Update(GameTime gameTime, Vector2 muzzlePosition, Vector2 direction)
{
    bool fired = false;
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (cooldownTimer > 0) cooldownTimer -= dt;

    MouseState currentMouseState = Mouse.GetState();

    // Check for left click and fire rate cooldown
    if (currentMouseState.LeftButton == ButtonState.Pressed && priorMouseState.LeftButton == ButtonState.Released && cooldownTimer <= 0)
    {
        Shoot(muzzlePosition, direction);
        cooldownTimer = fireRate;
        fired = true;
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

    return fired;
}

private void Shoot(Vector2 muzzlePosition, Vector2 direction)
{
    if (direction == Vector2.Zero) direction = new Vector2(1, 0); // Fallback to shooting right
    ActiveBullets.Add(new Bullet(bulletTexture, muzzlePosition, Vector2.Normalize(direction) * bulletSpeed));
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