using System.Linq;
using Microsoft.Xna.Framework;
using CIS580.Views;

namespace CIS580.Tests.Views;

public class ViewLayoutTests
{
    private static readonly Rectangle Screen = new(0, 0, 801, 601);   // Odd sizes, to catch rounding gaps

    [Fact]
    public void SharedLayout_GivesEveryViewTheWholeScreen()
    {
        Assert.All(new SharedLayout().Arrange(Screen, 3), area => Assert.Equal(Screen, area));
    }

    [Fact]
    public void SplitLayout_OneView_GetsTheWholeScreen()
    {
        Assert.Equal(Screen, Assert.Single(new SplitLayout().Arrange(Screen, 1)));
    }

    [Fact]
    public void SplitLayout_TwoViews_AreSideBySide()
    {
        var areas = new SplitLayout().Arrange(Screen, 2);

        Assert.Equal(new Rectangle(0, 0, 400, 601), areas[0]);
        Assert.Equal(new Rectangle(400, 0, 401, 601), areas[1]);
    }

    [Fact]
    public void SplitLayout_TwoViews_CanBeStacked()
    {
        var areas = new SplitLayout { StackTwoViews = true }.Arrange(Screen, 2);

        Assert.Equal(0, areas[0].Y);
        Assert.Equal(areas[0].Bottom, areas[1].Y);
        Assert.Equal(Screen.Width, areas[1].Width);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void SplitLayout_ThreeOrFourViews_UseAGridOfQuarters(int count)
    {
        var areas = new SplitLayout().Arrange(Screen, count);

        Assert.Equal(count, areas.Length);
        Assert.All(areas, a => Assert.InRange(a.Width, 400, 401));
        Assert.All(areas, a => Assert.InRange(a.Height, 300, 301));
        Assert.Equal(count, areas.Distinct().Count());
    }

    [Fact]
    public void SplitLayout_FourViews_CoverTheScreenExactly()
    {
        var areas = new SplitLayout().Arrange(Screen, 4);

        Assert.Equal(Screen.Width * Screen.Height, areas.Sum(a => a.Width * a.Height));
    }
}
