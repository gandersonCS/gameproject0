using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace gameproject0;

/// <summary>
/// A class representing a slime ghost
/// </summary>
public class Player
{
    public enum PlayerState
    {
        Idle,
        Left,
        Right,
        Up
    }

    public PlayerState State = PlayerState.Idle;
    private double animationTimer;
    private int animationFrame;
    private GamePadState gamePadState;

    private KeyboardState keyboardState;

    private Texture2D texture;

    public Vector2 position = new Vector2(500, groundLevel);

    private Vector2 velocity = new Vector2(0,0);

    //How fast the player is pulled down
private float gravity = 800f; 

//The upward burst of speed 
private float jumpSpeed = -400f; 
private bool isJumping = false;
//The Y position of thed floor
static private float groundLevel = Constants.GAME_HEIGHT; 

    private bool flipped;

    private BoundingRectangle bounds = new BoundingRectangle(new Vector2(200-16, 200-16), 32, 32);

    public BoundingRectangle Bounds => bounds;

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
        texture = content.Load<Texture2D>("M484SpaceSoldier");
    }

    /// <summary>
    /// Updates the sprite's position based on user input
    /// </summary>
    /// <param name="gameTime">The GameTime</param>
    public void Update(GameTime gameTime)
    {
        gamePadState = GamePad.GetState(0);
        keyboardState = Keyboard.GetState();
        bool isMoving = false;

        // Apply the gamepad movement with inverted Y axis
        // position += gamePadState.ThumbSticks.Left * new Vector2(1, -1);
        // if (gamePadState.ThumbSticks.Left.X < 0) flipped = true;
        // if (gamePadState.ThumbSticks.Left.X > 0) flipped = false;

        // Apply keyboard movement
if ((keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W)) && !isJumping)
{
    velocity.Y = jumpSpeed; // Apply upward force
    isJumping = true;       // Prevent infinite double jumps
}

// 2. Apply Gravity (if in the air)
if (isJumping)
{
    State = PlayerState.Up;
    // Gravity constantly increases the downward velocity over time
    velocity.Y += gravity * (float)gameTime.ElapsedGameTime.TotalSeconds;
}

// 3. Move the Player
position.Y += velocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds;

// 4. Ground Collision
if (position.Y >= groundLevel)
{
    // Snap exactly to the floor
    position.Y = groundLevel; 
    isJumping = false;
    // Stop moving downward
    velocity.Y = 0f;          
}
        velocity.X = 5f;
        // if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S)) position += new Vector2(0, 1);
        if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
        { 
            
            //Trying out velocity for X to make it sync with jumping better
            position.X -= velocity.X;
            flipped = true;
            State = PlayerState.Left;
            isMoving = true;
        }
        else if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
        {
            
            // position += new Vector2(1, 0);
            position.X += velocity.X;
            flipped = false;
            State = PlayerState.Right;
            isMoving = true;
        }
        else
        {
            isMoving = false;
            State = PlayerState.Idle;
        }

        // 2. Handle Animation Framing
        if (isMoving)
        {
            animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
            
            // Switch frames every 0.1 seconds for a brisk run speed
            if (animationTimer > 0.1) 
            {
                animationFrame++;
                // The running animations have 8 frames (0 through 7)
                if (animationFrame > 7) animationFrame = 0; 
                animationTimer -= 0.1;
            }
        }
        else
        {
            // Reset to the first frame when standing still
            animationFrame = 0; 
            animationTimer = 0;
        }
        //Final check for jump animation
        if (isJumping)
        {
            State = PlayerState.Up;
        }

        //Update the bounds
        bounds.X = position.X - 16;
        bounds.Y = position.Y - 16;
    }

    /// <summary>
    /// Draws the sprite using the supplied SpriteBatch
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The spritebatch to render with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        SpriteEffects spriteEffects = (flipped) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        animationTimer += gameTime.ElapsedGameTime.TotalSeconds;

    // 2. Update animation Frame
    if (animationTimer > 0.1)
    {
        animationFrame++;
        // The soldier sprite has 8 frames per running animation
        if (animationFrame > 7) animationFrame = 0; 
        animationTimer -= 0.1;
    }

    //Determine the correct row based on the PlayerState
    //For some reason, 11 was the lucky number. Could not figure out how to get the right value to avoid the red square
    int rowPix = 11; 
    switch (State)
    {
        case PlayerState.Idle:
        animationFrame = 0;
            rowPix = 11;
            break;
        case PlayerState.Up:
            animationFrame = 4;
            break;
        case PlayerState.Right:
            rowPix = 67;          
            break;
        case PlayerState.Left:
            rowPix = 67;           
            break;
    }

    //Calculate source rectangle 
    int xPos;
    int yPos;
    if(State == PlayerState.Up)
        {
            animationFrame = 4;
            xPos = 8 + (animationFrame * 51);
            yPos = 11;
            
        }
        else
        {
            xPos = 8 + (animationFrame * 51);
            yPos = rowPix;
             
        }
    
    

    var source = new Rectangle(xPos, yPos, 50, 50);
        spriteBatch.Draw(texture, position, source, Color, 0, new Vector2(64, 64), 2f, spriteEffects, 0);
    }
}
