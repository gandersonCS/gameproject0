using CIS580.UI.Widgets;

namespace CIS580.Tests.UI;

public class MenuScrollTests
{
    private static Menu MenuWith(int items, int visible)
    {
        var menu = new Menu { MaxVisibleItems = visible };
        for (int i = 0; i < items; i++)
            menu.Add(new ButtonItem($"Item {i}", null));
        return menu;
    }

    [Fact]
    public void ShortMenus_DontScroll()
    {
        var menu = MenuWith(3, visible: 5);
        menu.SelectedIndex = 2;
        Assert.Equal(3, menu.ScrollToSelection());
        Assert.Equal(0, menu.FirstVisibleIndex);
    }

    [Fact]
    public void MovingPastTheBottom_ScrollsDown_ThenBackUp()
    {
        var menu = MenuWith(10, visible: 4);

        menu.SelectedIndex = 5;
        Assert.Equal(4, menu.ScrollToSelection());
        Assert.Equal(2, menu.FirstVisibleIndex);     // Items 2-5 shown

        menu.SelectedIndex = 3;
        menu.ScrollToSelection();
        Assert.Equal(2, menu.FirstVisibleIndex);     // Still in view: no scrolling

        menu.SelectedIndex = 0;
        menu.ScrollToSelection();
        Assert.Equal(0, menu.FirstVisibleIndex);
    }

    [Fact]
    public void WrappingToTheEnd_ShowsTheLastItems()
    {
        var menu = MenuWith(10, visible: 4);
        menu.SelectedIndex = 9;
        menu.ScrollToSelection();
        Assert.Equal(6, menu.FirstVisibleIndex);
    }
}
