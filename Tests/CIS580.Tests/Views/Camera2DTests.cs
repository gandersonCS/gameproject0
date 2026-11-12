using Microsoft.Xna.Framework;
using CIS580.Views;

namespace CIS580.Tests.Views;

public class Camera2DTests
{
    private static void AssertNear(Vector2 expected, Vector2 actual)
    {
        Assert.Equal(expected.X, actual.X, 0.01f);
        Assert.Equal(expected.Y, actual.Y, 0.01f);
    }

    [Fact]
    public void ThePosition_AppearsAtTheCenterOfTheViewport()
    {
        var camera = new Camera2D { Position = new Vector2(100, 50), Viewport = new Rectangle(0, 0, 800, 600) };

        AssertNear(new Vector2(400, 300), camera.WorldToScreen(new Vector2(100, 50)));
    }

    [Fact]
    public void ViewportOffset_IsIncludedInScreenCoordinates()
    {
        // e.g. the right half of a split screen
        var camera = new Camera2D { Position = Vector2.Zero, Viewport = new Rectangle(400, 0, 400, 600) };

        AssertNear(new Vector2(600, 300), camera.WorldToScreen(Vector2.Zero));
    }

    [Fact]
    public void Zoom_ScalesDistancesFromTheCenter()
    {
        var camera = new Camera2D { Zoom = 2, Viewport = new Rectangle(0, 0, 800, 600) };

        AssertNear(new Vector2(420, 300), camera.WorldToScreen(new Vector2(10, 0)));
    }

    [Fact]
    public void ScreenToWorld_UndoesWorldToScreen()
    {
        var camera = new Camera2D
        {
            Position = new Vector2(37, -12), Zoom = 1.5f, Rotation = 0.3f,
            Viewport = new Rectangle(100, 50, 640, 360),
        };
        var world = new Vector2(55, 21);

        AssertNear(world, camera.ScreenToWorld(camera.WorldToScreen(world)));
    }

    [Fact]
    public void FitToArea_ShowsTheWholeArea_Centered()
    {
        // A 2:1 area in a 4:3 viewport: limited by width, with space above and below
        var camera = new Camera2D { Viewport = new Rectangle(0, 0, 800, 600) };

        camera.FitToArea(new RectangleF(0, 0, 400, 200));

        Assert.Equal(2f, camera.Zoom, 0.001f);
        AssertNear(new Vector2(0, 100), camera.WorldToScreen(new Vector2(0, 0)));
        AssertNear(new Vector2(800, 500), camera.WorldToScreen(new Vector2(400, 200)));
    }

    [Fact]
    public void Follow_MovesTowardTheTarget_WithoutOvershooting()
    {
        var camera = new Camera2D();

        camera.Follow(new Vector2(100, 0), stiffness: 5, deltaSeconds: 0.1f);

        Assert.InRange(camera.Position.X, 1f, 99f);
    }
}
