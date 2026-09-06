using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace gameproject0
{
    public class PlanetSprite
    {
        //animation frames
        private Texture2D textureFrame1;
        private Texture2D textureFrame2;
        private Texture2D textureFrame3;

        // the current texture of planet being displayed
        private Texture2D currentTexture;

        private Vector2 position = new Vector2(200, 200);
        
        // controls movement direction and speed
        private float speed = .2f;

// timer inbetween swapping textures
        private double animationTimer = 0;
    


    private BoundingCircle bounds = new BoundingCircle(new Vector2(1,1),1);

    public BoundingCircle Bounds => bounds;

    /// <summary>
    /// the color overlay
    /// </summary>
    public Color Color {get; set;} = Color.White;

    /// <summary>
    /// Loads the sprite texture using the provided ContentManager
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
       textureFrame1 = content.Load<Texture2D>("planet1");
       textureFrame2 = content.Load<Texture2D>("Beta 6"); 
            
            textureFrame3 = content.Load<Texture2D>("noShadow");
            
            currentTexture = textureFrame1;
    }

    /// <summary>
    /// Updates the sprite's position and animation
    /// </summary>
    /// <param name="gameTime">The GameTime</param>
    public void Update(GameTime gameTime, int screenWidth)
    {
        
        //Update the sprite's location
position.X += speed;

            // bounce off the right or left edge of the screen
        
            float visualWidth = currentTexture.Width * 0.25f;

            if (position.X > screenWidth - visualWidth || position.X < 0)
            {
                position.X = screenWidth - visualWidth;
                speed *= -.1f; 
            }
            else if (position.X < 0)
{
    // fixing wall collision
    position.X = 0; 
    speed *= -.1f;
}

            // animation handling
            animationTimer += gameTime.ElapsedGameTime.TotalSeconds;

            // next frame every 0.5 seconds
            if (animationTimer > 0.5)

    
    if (currentTexture == textureFrame1)
    {
        currentTexture = textureFrame2;
    }
    else if (currentTexture == textureFrame2)
    {
        currentTexture = textureFrame3;
    }
    else 
    {
        currentTexture = textureFrame1;
    }

    animationTimer = 0;
       
    }

    /// <summary>
    /// Draws the sprite using the supplied SpriteBatch
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The spritebatch to render with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        
        spriteBatch.Draw(currentTexture, position, null, Color, 0, new Vector2(64, 64), 0.25f, SpriteEffects.None, 0);
    }
    }
}