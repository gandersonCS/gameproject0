using Microsoft.Xna.Framework;

namespace MechaHorde.Gameplay
{
    public class LevelCamera
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; }

        public LevelCamera()
        {
            Position = Vector2.Zero;
            Zoom = .5f;
        }

        
        public Matrix GetViewMatrix(Vector2 screenCenter)
        {
            return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
                   Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                   Matrix.CreateTranslation(new Vector3(screenCenter.X, screenCenter.Y, 0));
        }
    }
}