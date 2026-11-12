using System;
using Microsoft.Xna.Framework;
using CIS580.Feedback;

namespace CIS580.Views;

/// <summary>
/// A 2D camera: which part of the world is shown, and where on screen.
/// </summary>
/// <remarks>
/// The camera looks at <see cref="Position"/> (a point in world units), which appears at the
/// center of its <see cref="Viewport"/> (a rectangle on screen, in pixels). <see cref="Zoom"/>
/// is how many pixels one world unit takes up. Pass <see cref="Transform"/> to
/// SpriteBatch.Begin to draw in world units.
/// </remarks>
public sealed class Camera2D
{
    /// <summary>
    /// The point in the world shown at the center of the viewport
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// How many pixels one world unit takes up on screen
    /// </summary>
    public float Zoom { get; set; } = 1f;

    /// <summary>
    /// The camera's rotation, in radians
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// The area of the screen the camera draws to, in pixels
    /// </summary>
    public Rectangle Viewport { get; set; }

    /// <summary>
    /// Shakes the camera's view; add trauma to it for impacts. (Updated by the WorldLayer.)
    /// </summary>
    public ScreenShake Shake { get; } = new();

    /// <summary>
    /// Transforms world coordinates into coordinates within the viewport. Use this as the
    /// SpriteBatch's transform while the graphics device's viewport is set to <see cref="Viewport"/>.
    /// It includes the current <see cref="Shake"/>.
    /// </summary>
    public Matrix Transform =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0)
        * Matrix.CreateRotationZ(-Rotation - Shake.Angle)
        * Matrix.CreateScale(Zoom, Zoom, 1)
        * Matrix.CreateTranslation(Viewport.Width / 2f + Shake.Offset.X, Viewport.Height / 2f + Shake.Offset.Y, 0);

    /// <summary>
    /// Converts a point in the world to a point on the screen (e.g. to draw a label over an entity)
    /// </summary>
    public Vector2 WorldToScreen(Vector2 world)
        => Vector2.Transform(world, Transform) + Viewport.Location.ToVector2();

    /// <summary>
    /// Converts a point on the screen to a point in the world (e.g. to find what the mouse is over)
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screen)
        => Vector2.Transform(screen - Viewport.Location.ToVector2(), Matrix.Invert(Transform));

    /// <summary>
    /// Centers the camera on an area of the world, zoomed so the whole area fits in the viewport.
    /// If the area's shape differs from the viewport's, the extra space shows beyond the area's edges.
    /// </summary>
    /// <param name="area">The area of the world to show, in world units</param>
    public void FitToArea(RectangleF area)
    {
        if (area.Width <= 0 || area.Height <= 0 || Viewport.Width <= 0 || Viewport.Height <= 0)
            return;

        Position = area.Center;
        Zoom = Math.Min(Viewport.Width / area.Width, Viewport.Height / area.Height);
    }

    /// <summary>
    /// Moves the camera smoothly toward a target
    /// </summary>
    /// <param name="target">The point to move toward</param>
    /// <param name="stiffness">How quickly to catch up; higher is faster</param>
    /// <param name="deltaSeconds">The time since the last update, in seconds</param>
    public void Follow(Vector2 target, float stiffness, float deltaSeconds)
    {
        float t = 1f - MathF.Exp(-stiffness * deltaSeconds);
        Position = Vector2.Lerp(Position, target, t);
    }
}

/// <summary>
/// A rectangle with floating-point position and size, for areas of the world
/// </summary>
public readonly record struct RectangleF(float X, float Y, float Width, float Height)
{
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);
}
