using System;
using Microsoft.Xna.Framework;
using CIS580.Feedback;
using CIS580.Views;

namespace CIS580.Tests.Feedback;

public class ScreenShakeTests
{
    private static readonly TimeSpan Frame = TimeSpan.FromSeconds(1 / 60.0);

    [Fact]
    public void NoTrauma_NoShake()
    {
        var shake = new ScreenShake();
        shake.Update(Frame);
        Assert.Equal(Vector2.Zero, shake.Offset);
        Assert.Equal(0f, shake.Angle);
    }

    [Fact]
    public void Trauma_ShakesWithinTheLimits()
    {
        var shake = new ScreenShake { Recovery = 0 };
        shake.Add(1f);

        bool moved = false;
        for (int i = 0; i < 60; i++)
        {
            shake.Update(Frame);
            Assert.InRange(shake.Offset.X, -shake.MaxOffset, shake.MaxOffset);
            Assert.InRange(shake.Angle, -shake.MaxAngle, shake.MaxAngle);
            moved |= shake.Offset != Vector2.Zero;
        }
        Assert.True(moved);
    }

    [Fact]
    public void Trauma_WearsOff_AndIsCapped()
    {
        var shake = new ScreenShake { Recovery = 1f };
        shake.Add(0.8f);
        shake.Add(0.8f);
        Assert.Equal(1f, shake.Trauma);

        for (int i = 0; i < 70; i++)
            shake.Update(Frame);

        Assert.Equal(0f, shake.Trauma);
        Assert.Equal(Vector2.Zero, shake.Offset);
    }

    [Fact]
    public void TheCamera_IsMovedByItsShake()
    {
        var camera = new Camera2D { Viewport = new Rectangle(0, 0, 800, 600) };
        var still = camera.WorldToScreen(Vector2.Zero);

        camera.Shake.Recovery = 0;
        camera.Shake.Add(1f);
        for (int i = 0; i < 5; i++)
            camera.Shake.Update(Frame);

        Assert.NotEqual(still, camera.WorldToScreen(Vector2.Zero));
        Assert.Equal(Vector2.Zero, camera.ScreenToWorld(camera.WorldToScreen(Vector2.Zero)), new Vector2Comparer());
    }

    private sealed class Vector2Comparer : System.Collections.Generic.IEqualityComparer<Vector2>
    {
        public bool Equals(Vector2 a, Vector2 b) => Vector2.Distance(a, b) < 0.01f;
        public int GetHashCode(Vector2 v) => 0;
    }
}
