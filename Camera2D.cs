using Microsoft.Xna.Framework;

namespace gameproject0
{
    public class Camera2D
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; }

        public Camera2D()
        {
            Position = Vector2.Zero;
            Zoom = 1.0f;
        }

        // This matrix is what satisfies your SpriteBatch Transform requirement
        public Matrix GetViewMatrix(Vector2 screenCenter)
        {
            return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
                   Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                   Matrix.CreateTranslation(new Vector3(screenCenter.X, screenCenter.Y, 0));
        }
    }
}