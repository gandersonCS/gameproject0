using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace gameproject0
{
    public class RainParticleSystem : ParticleSystem
    {
        Rectangle _source;

        public bool IsRaining {get; set;} = true;

        public RainParticleSystem(Game game, Rectangle source) : base(game, 5000)
        {
            _source = source;
        }

        protected override void InitializeConstants()
        {
            textureFilename = "drop";
            // Increase particle counts to fill the much larger spawn area
            minNumParticles = 50; 
            maxNumParticles = 100;
        }

        protected override void InitializeParticle(ref Particle p, Vector2 where)
        {
            // Vector2(X, Y): Adding a positive X value pushes the rain to the right. 
            // Use a negative X value (e.g., -150) to make it blow to the left.
            Vector2 windVelocity = new Vector2(150, 1000); 

            p.Initialize(where, windVelocity, Vector2.Zero, Color.DarkRed, scale: RandomHelper.NextFloat(0.1f, 0.4f), lifetime: 1);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if(IsRaining) AddParticles(_source);
        }
    }
}