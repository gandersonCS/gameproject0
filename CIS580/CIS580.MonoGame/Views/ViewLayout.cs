using System;
using Microsoft.Xna.Framework;

namespace CIS580.Views;

/// <summary>
/// Decides where on screen each view of the world goes
/// </summary>
public abstract class ViewLayout
{
    /// <summary>
    /// Divides the screen among the given number of views
    /// </summary>
    /// <param name="screen">The whole screen area</param>
    /// <param name="count">The number of views</param>
    /// <returns>The screen area for each view</returns>
    public abstract Rectangle[] Arrange(Rectangle screen, int count);
}

/// <summary>
/// Every view uses the whole screen. Use this with a single camera for single player games,
/// and for multiplayer games where everyone shares one view.
/// </summary>
public sealed class SharedLayout : ViewLayout
{
    public override Rectangle[] Arrange(Rectangle screen, int count)
    {
        var areas = new Rectangle[count];
        Array.Fill(areas, screen);
        return areas;
    }
}

/// <summary>
/// Split screen: each view gets its own part of the screen. Two views are placed side by side
/// (or one above the other); more views are placed in a grid.
/// </summary>
public sealed class SplitLayout : ViewLayout
{
    /// <summary>
    /// If true, two views are stacked one above the other rather than side by side
    /// </summary>
    public bool StackTwoViews { get; set; }

    public override Rectangle[] Arrange(Rectangle screen, int count)
    {
        if (count <= 0)
            return Array.Empty<Rectangle>();

        int columns, rows;
        if (count == 2)
            (columns, rows) = StackTwoViews ? (1, 2) : (2, 1);
        else
        {
            columns = (int)Math.Ceiling(Math.Sqrt(count));
            rows = (int)Math.Ceiling(count / (double)columns);
        }

        var areas = new Rectangle[count];
        for (int i = 0; i < count; i++)
        {
            int column = i % columns, row = i / columns;

            // Compute edges (rather than sizes) so the areas exactly cover the screen, with no gaps
            int left = screen.X + screen.Width * column / columns;
            int right = screen.X + screen.Width * (column + 1) / columns;
            int top = screen.Y + screen.Height * row / rows;
            int bottom = screen.Y + screen.Height * (row + 1) / rows;
            areas[i] = new Rectangle(left, top, right - left, bottom - top);
        }
        return areas;
    }
}
