using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class TrayPopupPlacementPolicyTests
{
    [Fact]
    public void Place_BottomTrayInvocationKeepsPopupCloseAboveCursor()
    {
        var placement = TrayPopupPlacementPolicy.Place(
            cursorX: 1900,
            cursorY: 1040,
            popupWidth: 392,
            popupHeight: 542,
            workAreaLeft: 0,
            workAreaTop: 0,
            workAreaRight: 1920,
            workAreaBottom: 1040);

        Assert.Equal(1522, placement.X);
        Assert.Equal(494, placement.Y);
        Assert.Equal(4, 1040 - (placement.Y + 542));
    }

    [Fact]
    public void Place_TopTrayInvocationOpensBelowCursor()
    {
        var placement = TrayPopupPlacementPolicy.Place(
            cursorX: 1200,
            cursorY: 24,
            popupWidth: 392,
            popupHeight: 542,
            workAreaLeft: 0,
            workAreaTop: 40,
            workAreaRight: 1920,
            workAreaBottom: 1080);

        Assert.Equal(828, placement.X);
        Assert.Equal(46, placement.Y);
    }

    [Fact]
    public void Place_ClampsPopupInsideSmallWorkArea()
    {
        var placement = TrayPopupPlacementPolicy.Place(
            cursorX: 780,
            cursorY: 570,
            popupWidth: 392,
            popupHeight: 542,
            workAreaLeft: 0,
            workAreaTop: 0,
            workAreaRight: 800,
            workAreaBottom: 600);

        Assert.Equal(402, placement.X);
        Assert.Equal(24, placement.Y);
        Assert.InRange(placement.Y + 542, 0, 594);
    }

    [Fact]
    public void Place_InvalidWorkAreaIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TrayPopupPlacementPolicy.Place(
                cursorX: 10,
                cursorY: 10,
                popupWidth: 392,
                popupHeight: 542,
                workAreaLeft: 100,
                workAreaTop: 0,
                workAreaRight: 100,
                workAreaBottom: 600));
    }
}
