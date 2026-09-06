using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace gameproject0
{
    /// <summary>
    /// struct represneting circular bounds
    /// </summary>
    public struct BoundingCircle
    {
        /// <summary>
        /// the center of the circle
        /// </summary>
        public Vector2 Center;

        /// <summary>
        /// radius of circle
        /// </summary>
        public float Radius;

        public BoundingCircle(Vector2 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>
    /// Tests for a collision between this and another bounding circle
    /// </summary>
    /// <param name="other">The other bounding circle</param>
    /// <returns>true for collision, false otherwise</returns>
    public bool CollidesWith(BoundingCircle other)
        {
            return CollisionHelper.Collides(this, other);
        }
    public bool CollidesWith(BoundingRectangle other)
        {
            return CollisionHelper.Collides(this, other);
        }

    }
}