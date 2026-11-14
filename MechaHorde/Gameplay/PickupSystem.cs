using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MechaHorde.Gameplay;

/// <summary>
/// The Scraplight pickups: drops them from robots as they're shot and destroyed, moves them,
/// adds them to the player's <see cref="Scraplight"/> when collected, and draws them as 3D models.
/// </summary>
/// <remarks>
/// <para>
/// The models (in <c>MechaHorde.Content/Assets/Models/</c>) are each centred on their origin
/// with their largest side 2 units long, so a model is drawn <see cref="Pickup.Size"/> level
/// pixels across by scaling it by half that.
/// </para>
/// <para>
/// The level is 2D, drawn in level pixels with Y pointing down. The models are drawn with an
/// orthographic projection that maps level pixels to the same place on screen as the
/// SpriteBatch's level transform, so they sit in the level exactly like the sprites do.
/// </para>
/// </remarks>
public class PickupSystem
{
    /// <summary>Chance that a bullet hit knocks a piece of scrap off a robot</summary>
    private const float ScrapOnHitChance = 0.2f;

    // The lights: a warm key light from the upper left (the red dusk) and a cool rim light from
    // behind on the right, so the metal catches highlights on both sides. The models are lit in
    // screen space, where Y points down, so a light shining downwards has a positive Y.
    private static readonly Vector3 KeyLightDirection = Vector3.Normalize(new Vector3(0.6f, 0.5f, -0.6f));
    private static readonly Vector3 KeyLightColor = new(1.0f, 0.75f, 0.65f);
    private static readonly Vector3 RimLightDirection = Vector3.Normalize(new Vector3(-0.7f, 0.2f, 0.7f));
    private static readonly Vector3 RimLightColor = new(0.35f, 0.45f, 0.8f);
    private static readonly Vector3 AmbientColor = new(0.25f, 0.16f, 0.18f);

    private static readonly Vector3 ScrapColor = new(0.72f, 0.7f, 0.68f);

    /// <summary>
    /// The sparks' looks: each is a glowing box from the Block Pack, with its model (in
    /// Models/), its texture (in Textures/Pickups/, the same name), and the color it glows.
    /// A dropped spark is one of these at random.
    /// </summary>
    private static readonly (string Name, Vector3 Glow)[] SparkStyles =
    {
        ("spark_block", new Vector3(0.2f, 1f, 0.3f)),   // green
        ("spark_hip", new Vector3(1f, 0.25f, 1f)),      // magenta
        ("spark_kick", new Vector3(1f, 0.55f, 0.1f)),   // orange
        ("spark_bomb", new Vector3(1f, 0.15f, 0.1f)),   // red
        ("spark_nuke", new Vector3(1f, 0.2f, 0.15f)),   // red
        ("spark_orb", new Vector3(0.6f, 0.3f, 1f)),     // purple
    };

    private readonly List<Pickup> _pickups = new();
    private readonly GraphicsDevice _graphics;

    private Model _gear;
    private Model _propeller;
    private Model[] _sparks;
    private Texture2D[] _sparkTextures;
    private Texture2D _glow;

    /// <param name="graphics">The graphics device (for drawing the models)</param>
    /// <param name="wallet">Where collected Scraplight goes</param>
    public PickupSystem(GraphicsDevice graphics, Scraplight wallet)
    {
        _graphics = graphics;
        Wallet = wallet;
    }

    /// <summary>The player's Scraplight</summary>
    public Scraplight Wallet { get; }

    /// <summary>The pickups in the level</summary>
    public IReadOnlyList<Pickup> Pickups => _pickups;

    public void LoadContent(ContentManager content)
    {
        _gear = content.Load<Model>("Models/scrap_gear");
        _propeller = content.Load<Model>("Models/scrap_propeller");
        _sparks = new Model[SparkStyles.Length];
        _sparkTextures = new Texture2D[SparkStyles.Length];
        for (int i = 0; i < SparkStyles.Length; i++)
        {
            _sparks[i] = content.Load<Model>("Models/" + SparkStyles[i].Name);
            _sparkTextures[i] = content.Load<Texture2D>("Textures/Pickups/" + SparkStyles[i].Name);
        }
        _glow = CreateGlow(_graphics);

        // Every mesh part of these models uses a BasicEffect (the ModelProcessor's default);
        // the lighting is the same for all of them, so it's set once here
        foreach (var model in new[] { _gear, _propeller })
            SetUpLighting(model, null);
        for (int i = 0; i < _sparks.Length; i++)
            SetUpLighting(_sparks[i], _sparkTextures[i]);
    }

    public void Unload() => _glow?.Dispose();

    /// <summary>
    /// A robot was shot: sometimes a piece of scrap flies off it
    /// </summary>
    public void OnEnemyDamaged(Enemy enemy)
    {
        if (enemy.IsAlive && RandomHelper.NextFloat() < ScrapOnHitChance)
            Drop(RandomScrap(), enemy, 1);
    }

    /// <summary>
    /// A robot was destroyed: it bursts into a pile of scrap (one part per point), and
    /// sometimes drops a spark. Sparks only ever come from destroyed robots.
    /// </summary>
    public void OnEnemyDestroyed(Enemy enemy)
    {
        // How many parts the pile has (min and max, inclusive), and the chance of a spark
        (int min, int max, float sparkChance) = enemy.EnemyType switch
        {
            Enemy.Type.drone => (1, 2, 0.04f),
            Enemy.Type.ranged => (2, 3, 0.10f),
            Enemy.Type.medium => (3, 4, 0.08f),
            Enemy.Type.heavy => (5, 7, 0.5f),
            _ => (1, 2, 0.05f)
        };
        int scrap = RandomHelper.Next(min, max + 1);

        for (int i = 0; i < scrap; i++)
            Drop(RandomScrap(), enemy, scrap);
        if (RandomHelper.NextFloat() < sparkChance)
            Drop(PickupKind.Spark, enemy, 1, RandomHelper.Next(SparkStyles.Length));
    }

    /// <summary>
    /// Moves the pickups, and collects those that reach the player
    /// </summary>
    public void Update(GameTime gameTime, Player player)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _pickups.Count - 1; i >= 0; i--)
        {
            var pickup = _pickups[i];
            if (pickup.Update(dt, player.Center))
                Wallet.Collect(pickup.Kind);
            if (pickup.IsCollected || pickup.IsExpired)
                _pickups.RemoveAt(i);
        }
    }

    /// <summary>
    /// Removes every pickup (e.g. when a new game starts)
    /// </summary>
    public void Clear() => _pickups.Clear();

    /// <summary>
    /// Draws the sparks' glow (call inside the level's SpriteBatch, before the models)
    /// </summary>
    public void DrawGlow(SpriteBatch spriteBatch)
    {
        foreach (var pickup in _pickups)
        {
            if (pickup.Kind != PickupKind.Spark || !pickup.IsVisible)
                continue;
            float pulse = 0.55f + 0.25f * MathF.Sin(pickup.Age * 4f);
            float size = pickup.Size * 3.2f;
            var center = pickup.Position + new Vector2(0, pickup.Bob);
            var glow = SparkStyles[pickup.Style].Glow;
            spriteBatch.Draw(_glow, center, null, new Color(glow) * pulse, 0f,
                new Vector2(_glow.Width / 2f, _glow.Height / 2f), size / _glow.Width, SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// Draws the pickups' 3D models (call between SpriteBatches, not inside one)
    /// </summary>
    /// <param name="levelScale">How many screen pixels a level pixel covers</param>
    /// <param name="levelOffset">Where level (0, 0) is on screen</param>
    public void DrawModels(float levelScale, Vector2 levelOffset)
    {
        if (_pickups.Count == 0)
            return;

        // Level pixels to screen pixels, as the SpriteBatch's level transform does. Z is scaled
        // too (unlike the SpriteBatch's), so the models keep their depth for lighting and the depth test.
        Matrix levelToScreen = Matrix.CreateScale(levelScale) * Matrix.CreateTranslation(levelOffset.X, levelOffset.Y, 0f);

        var viewport = _graphics.Viewport;

        // Screen pixels to clip space, with Y pointing down like the SpriteBatch. The depth range
        // only has to hold the models, which are at most a few level pixels deep.
        float depth = 64f * levelScale;
        var projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, -depth, depth);

        // SpriteBatch leaves its own states set; the models need depth testing and culling
        _graphics.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
        _graphics.BlendState = BlendState.Opaque;
        _graphics.DepthStencilState = DepthStencilState.Default;
        // The flips between the model's space and the screen's make the triangles' winding hard
        // to reason about; the models are closed, so drawing both sides with depth testing looks the same
        _graphics.RasterizerState = RasterizerState.CullNone;
        _graphics.SamplerStates[0] = SamplerState.LinearWrap;

        foreach (var pickup in _pickups)
        {
            if (!pickup.IsVisible)
                continue;

            Model model = pickup.Kind switch
            {
                PickupKind.ScrapGear => _gear,
                PickupKind.ScrapPropeller => _propeller,
                _ => _sparks[pickup.Style]
            };

            // The model is 2 units across, so half its size scales it to Size level pixels.
            // Y is flipped: the level's Y points down, the model's up.
            float half = pickup.Size / 2f;
            Matrix world = pickup.Rotation
                * Matrix.CreateScale(half, -half, half)
                * Matrix.CreateTranslation(pickup.Position.X, pickup.Position.Y + pickup.Bob, 0f)
                * levelToScreen;

            if (pickup.Kind == PickupKind.Spark)
            {
                // The spark's glow pulses
                float pulse = 0.25f + 0.2f * MathF.Sin(pickup.Age * 4f);
                foreach (var mesh in model.Meshes)
                    foreach (var effect in mesh.Effects)
                        if (effect is BasicEffect basic)
                            basic.EmissiveColor = SparkStyles[pickup.Style].Glow * pulse;
            }

            model.Draw(world, Matrix.Identity, projection);
        }

        // Put back what SpriteBatch expects to find (it sets these itself on Begin, but other
        // drawing this frame might not)
        _graphics.DepthStencilState = DepthStencilState.None;
        _graphics.RasterizerState = RasterizerState.CullCounterClockwise;
        _graphics.BlendState = BlendState.AlphaBlend;
        _graphics.SamplerStates[0] = SamplerState.LinearClamp;
    }

    /// <summary>
    /// Lights every part of a model: bare metal for scrap (no texture), textured for a spark
    /// </summary>
    private void SetUpLighting(Model model, Texture2D sparkTexture)
    {
        foreach (var mesh in model.Meshes)
            foreach (var effect in mesh.Effects)
                if (effect is BasicEffect basic)
                    SetUpLighting(basic, sparkTexture);
    }

    private void SetUpLighting(BasicEffect effect, Texture2D sparkTexture)
    {
        bool isSpark = sparkTexture != null;
        effect.LightingEnabled = true;
        effect.PreferPerPixelLighting = true;
        effect.AmbientLightColor = isSpark ? AmbientColor * 2.5f : AmbientColor;

        effect.DirectionalLight0.Enabled = true;
        effect.DirectionalLight0.Direction = KeyLightDirection;
        effect.DirectionalLight0.DiffuseColor = KeyLightColor;
        effect.DirectionalLight0.SpecularColor = KeyLightColor;

        effect.DirectionalLight1.Enabled = true;
        effect.DirectionalLight1.Direction = RimLightDirection;
        effect.DirectionalLight1.DiffuseColor = RimLightColor;
        effect.DirectionalLight1.SpecularColor = RimLightColor;

        effect.DirectionalLight2.Enabled = false;

        if (isSpark)
        {
            effect.TextureEnabled = true;
            effect.Texture = sparkTexture;
            effect.DiffuseColor = Vector3.One;
            effect.SpecularColor = new Vector3(0.5f);
            effect.SpecularPower = 24f;
        }
        else
        {
            // Bare metal: grey, with sharp bright highlights
            effect.TextureEnabled = false;
            effect.DiffuseColor = ScrapColor;
            effect.SpecularColor = new Vector3(0.9f);
            effect.SpecularPower = 18f;
            effect.EmissiveColor = Vector3.Zero;
        }
    }

    private static PickupKind RandomScrap() =>
        RandomHelper.Next(2) == 0 ? PickupKind.ScrapGear : PickupKind.ScrapPropeller;

    /// <summary>
    /// Throws a pickup out of an enemy: upwards and to a side, more spread out the more are dropped
    /// </summary>
    private void Drop(PickupKind kind, Enemy enemy, int count, int style = 0)
    {
        var bounds = enemy.Bounds;
        var from = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
        float spread = 25f + 8f * Math.Min(count, 5);
        var velocity = new Vector2(RandomHelper.NextFloat(-spread, spread), RandomHelper.NextFloat(-110f, -60f));
        _pickups.Add(new Pickup(kind, from, velocity, style));
    }

    /// <summary>
    /// A soft round glow, white in the middle fading out to the edges (tinted when drawn)
    /// </summary>
    private static Texture2D CreateGlow(GraphicsDevice graphics)
    {
        const int size = 64;
        var data = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = MathF.Min(1f, MathF.Sqrt(dx * dx + dy * dy));
                float a = MathF.Pow(1f - d, 2f);
                data[y * size + x] = Color.White * a;
            }
        }
        var texture = new Texture2D(graphics, size, size);
        texture.SetData(data);
        return texture;
    }
}
