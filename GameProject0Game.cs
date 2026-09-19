using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gameproject0;

public class GameProject0Game : Game
{
    
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    
    PlanetSprite planet;
    private SpriteFont titleFont;
    private SpriteFont buttonFont;
    MenuButtonSprite campaignButton;
MenuButtonSprite multiplayerButton;
MenuButtonSprite optionsButton;

MenuButtonSprite exitButton;

private float gameTimer = 0f;
private bool started;

private Player player;

    private MenuButtonSprite[] menuButtons;
    private MoonRockSprite[] rocks;

    /// <summary>
    /// A game demonstrating collision detection
    /// </summary>
    public GameProject0Game()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        graphics.PreferredBackBufferWidth = Constants.GAME_WIDTH;
        graphics.PreferredBackBufferHeight = Constants.GAME_HEIGHT;
    }

    /// <summary>
    /// Initializes the game 
    /// </summary>
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        System.Random random = new System.Random();
        rocks = new MoonRockSprite[random.Next(5, 10)];
 for (int i = 0; i < rocks.Length; i++)
{
    rocks[i] = new MoonRockSprite(); 
    
    // Pick a random location
    Vector2 randomSpawn = new Vector2(random.Next(50, 680), random.Next(50, 310));
    
    rocks[i].Center = randomSpawn;
    rocks[i].Position = randomSpawn; // FORCE the drawing position to match!
    
    float vx = (float)(random.NextDouble() * 100 - 500);
    float vy = (float)(random.NextDouble() * 100 - 500);
    rocks[i].Velocity = new Vector2(vx, vy);
    rocks[i].Mass = random.Next(50, 100); 
}
        player = new Player();
        planet = new PlanetSprite();
        int leftMargin = 50;
        //Space between button list and title
    int startingY = 150;
    //Space between button
    int verticalSpacing = 75;
        menuButtons = new MenuButtonSprite[]
        {
campaignButton = new MenuButtonSprite(new Vector2(leftMargin, startingY)) { Text = "CAMPAIGN", OnClick = () => started = true},
    multiplayerButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + verticalSpacing)) { Text = "MULTIPLAYER" },
    optionsButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + (verticalSpacing * 2))) { Text = "OPTIONS" },
    exitButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + (verticalSpacing * 3))) { Text = "EXIT(Press ESC)", OnClick = () => Exit()}
        };


        base.Initialize();
    }

    /// <summary>
    /// Loads content for the game
    /// </summary>
    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        player.LoadContent(Content);
        planet.LoadContent(Content);
        foreach (var r in rocks) r.LoadContent(Content);
        // TODO: use this.Content to load your game content here
        foreach (var but in menuButtons) but.LoadContent(Content);
        titleFont = Content.Load<SpriteFont>("aquire");
        buttonFont = Content.Load<SpriteFont>("robotheroes");
    }

    /// <summary>
    /// Updates the game world
    /// </summary>
    /// <param name="gameTime">The game time</param>
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        Exit();

    if (started)
    {
        player.Update(gameTime);
        gameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds; 
    }
    else
    {
        foreach(var but in menuButtons) but.Update(gameTime);
        planet.Update(gameTime, GraphicsDevice.Viewport.Width);
    }

    // Move each rock
foreach (var rock in rocks) rock.Update(gameTime);

//Detect Rock vs. Rock collisions
for (int i = 0; i < rocks.Length; i++) 
{
    for(int j = i + 1; j < rocks.Length; j++)
    {
        if (rocks[i].CollidesWith(rocks[j]))
        {
            rocks[i].Colliding = true;
            rocks[j].Colliding = true;

            //Find the distance and direction between them
            Vector2 collisionAxis = rocks[i].Center - rocks[j].Center;
            float distance = collisionAxis.Length();

            // Failsafe: if they spawn on the exact same pixel, fake a distance to prevent a crash
            if (distance == 0) 
            {
                collisionAxis = new Vector2(1, 0);
                distance = 1;
            }

            collisionAxis.Normalize();

            // 2. PHYSICALLY PUSH THEM APART
            float overlap = (rocks[i].Mass + rocks[j].Mass) - distance;
            if (overlap > 0)
            {
                // Push each rock away by half the overlap + 1 pixel to guarantee they break contact
                rocks[i].Center += collisionAxis * (overlap / 2f + 1f);
                rocks[j].Center -= collisionAxis * (overlap / 2f + 1f);
            }

            // 3. Calculate bounce trajectory (with clamped Dot product to prevent NaN errors)
            float dot = MathHelper.Clamp(Vector2.Dot(collisionAxis, Vector2.UnitX), -1f, 1f);
            float angle = (float)System.Math.Acos(dot);

            float m0 = rocks[i].Mass;
            float m1 = rocks[j].Mass;

            Vector2 u0 = Vector2.Transform(rocks[i].Velocity, Matrix.CreateRotationZ(angle));
            Vector2 u1 = Vector2.Transform(rocks[j].Velocity, Matrix.CreateRotationZ(angle));

            Vector2 v0;
            Vector2 v1;
            
            v0.X = ((m0 - m1) / (m0 + m1)) * u0.X + ((2 * m1) / (m0 + m1)) * u1.X;
            v1.X = ((2 * m0) / (m0 + m1)) * u0.X + ((m1 - m0) / (m0 + m1)) * u1.X; 
            v0.Y = u0.Y;
            v1.Y = u1.Y;

            rocks[i].Velocity = Vector2.Transform(v0, Matrix.CreateRotationZ(-angle));
            rocks[j].Velocity = Vector2.Transform(v1, Matrix.CreateRotationZ(-angle));
        }
    }
}


//Detect Rock vs. Player collisions
foreach (var rock in rocks)
{
    if (rock.CollidesWith(player)) 
    {
        rock.Colliding = true;
        //player.Color = Color.Red;
        //Step the rock back so it doesn't get stuck inside the player
        rock.Center -= rock.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // everse the rock's velocity so it bounces off the player's box
        rock.Velocity *= -1; 
        
        //Knockback logic
        //Find the direction pointing away from the rock towards the player
        Vector2 knockbackDir = new Vector2(player.position.X - rock.Center.X, player.position.Y - rock.Center.Y);
        
        //Normalize it
        if (knockbackDir != Vector2.Zero) 
            knockbackDir.Normalize();
            
        //Push the player away
        player.position += knockbackDir * 50f; 

        
    }
}

    base.Update(gameTime);
    }

    /// <summary>
    /// Draws the game world
    /// </summary>
    /// <param name="gameTime">The game time</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Navy);

        // TODO: Add your drawing code here
        
spriteBatch.Begin();
    
    if (started)
    {
        //Draw only the player and the timer
        player.Draw(gameTime, spriteBatch);
        foreach(var rock in rocks) rock.Draw(gameTime, spriteBatch);
        //Formats the float to 2 decimal places
        string timerText = $"Time: {gameTimer:F2}"; 
        spriteBatch.DrawString(titleFont, timerText, new Vector2(2, 2), Color.Red);
    }
    else
    {
        //Draw the planet, buttons, and title for the menu
        planet.Draw(gameTime, spriteBatch);
        foreach (var but in menuButtons) but.Draw(gameTime, spriteBatch);
        
        spriteBatch.DrawString(titleFont, "GAME TITLE", new Vector2(2, 2), Color.Gold);
    }
    
    spriteBatch.End();

    base.Draw(gameTime);
    }
}
