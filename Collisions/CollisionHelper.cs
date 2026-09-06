using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace gameproject0
{
    public static class CollisionHelper
    {
        /// <summary>
        /// Detects a collision between two bounding circles
        /// </summary>
        /// <param name="a">the first bounding circle</param>
        /// <param name="b">the second</param>
        /// <returns>true for colision, false otherwise</returns>
        public static bool Collides( BoundingCircle a, BoundingCircle b)
        {
            return Math.Pow(a.Radius + b.Radius, 2) >= 
                Math.Pow(a.Center.X - b.Center.X, 2) +
                Math.Pow(a.Center.Y - b.Center.Y, 2);
            
        }

        /// <summary>
        /// Detects a collision between two bounding rectahgles
        /// </summary>
        /// <param name="a">the first bounding retangle</param>
        /// <param name="b">the second</param>
        /// <returns>true for colision, false otherwise</returns>
        public static bool Collides( BoundingRectangle a, BoundingRectangle b)
        {
            return !(a.Right < b.Left || a.Left > b.Right || a.Top < b.Bottom || a.Bottom < b.Top);
            
        }

        /// <summary>
        /// detects collision between rectangle and circle
        /// </summary>
        /// <param name="c">the bounding circle</param>
        /// <param name="r">the bounding rectangle</param>
        /// <returns>true for collision, false otherwise</returns>
        public static bool Collides( BoundingCircle c, BoundingRectangle r)
        {
            float nearestX = MathHelper.Clamp(c.Center.X, r.Left, r.Right);
            float nearestY = MathHelper.Clamp(c.Center.Y, r.Top, r.Bottom);
             return Math.Pow(c.Radius, 2) >= 
                Math.Pow(c.Center.X - nearestX, 2) +
                Math.Pow(c.Center.Y - nearestY, 2);
        }

        public static bool Collides( BoundingRectangle r, BoundingCircle c) => Collides(c, r);
    }
}