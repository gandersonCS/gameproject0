using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;



namespace gameproject0
{
    public class MenuButtonSprite
    {
        private Vector2 position;

        private Texture2D texture;
        public Vector2 Scale { get; set; } = new Vector2(.2f, .2f);
        protected MouseState currentMouseState;

        protected MouseState priorMouseState;

        //field for handling button clicks in game.cs
        public Action OnClick { get; set; }

        // private BoundingRectangle bounds = new BoundingRectangle(new Vector2(200-16, 200-16), 32, 32);

        // public BoundingRectangle Bounds => bounds;

        // Using a standard Rectangle for easy mouse collision, since bounding rectangle was causing issues
        private Rectangle bounds;

        /// <summary>
        /// the color overlay
        /// </summary>
        public Color Color { get; set; } = Color.White;
        public string Text { get; set; } = "";
        public Color TextColor { get; set; } = Color.White;

        private SpriteFont buttonFont;
        /// <summary>
        /// constructor for the button
        /// </summary>
        /// <param name="startPosition">where the button starts at</param>
        public MenuButtonSprite(Vector2 startPosition)
        {
            position = startPosition;
        }

        /// <summary>
        /// Loads the sprite texture using the provided ContentManager
        /// </summary>
        /// <param name="content">The ContentManager to load with</param>
        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("futureui1");
            buttonFont = content.Load<SpriteFont>("robotheroes");
            bounds = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
        }

        /// <summary>
        /// Updates the sprite's position based on user input
        /// </summary>
        /// <param name="gameTime">The GameTime</param>
        public void Update(GameTime gameTime)
        {
            currentMouseState = Mouse.GetState();
            Point mousePosition = new Point(currentMouseState.X, currentMouseState.Y);

            //Look of button
            int actualWidth = (int)(texture.Width * 0.2f);
            int actualHeight = (int)(texture.Height * 0.2f);
            Color = Color.White;
            Scale = new Vector2(.2f, .2f);

            //Setting bounds for screen
            bounds = new Rectangle((int)position.X, (int)position.Y, actualWidth, actualHeight);
            //Hover check for mouse button
            if (bounds.Contains(mousePosition))
            {
                Color = Color.CornflowerBlue;
                Scale = new Vector2(.25f, .22f);

                // 3. Check for CLICK while hovering
                if (currentMouseState.LeftButton == ButtonState.Pressed && priorMouseState.LeftButton == ButtonState.Released)
                {
                    // TODO: click logic for button
                    OnClick?.Invoke();
                }
            }

            priorMouseState = currentMouseState;
        }

        /// <summary>
        /// Draws the sprite using the supplied SpriteBatch
        /// </summary>
        /// <param name="gameTime">The game time</param>
        /// <param name="spriteBatch">The spritebatch to render with</param>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {

            spriteBatch.Draw(texture, position, null, Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            if (buttonFont != null && !string.IsNullOrEmpty(Text))
            {

                Vector2 textOffset = new Vector2(position.X + 25, position.Y + 15);

                spriteBatch.DrawString(buttonFont, Text, textOffset, TextColor);
            }
        }

    }
}