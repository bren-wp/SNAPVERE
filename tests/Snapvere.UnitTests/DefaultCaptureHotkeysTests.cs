using Snapvere.Capture.Hotkeys;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class DefaultCaptureHotkeysTests
{
    [Fact]
    public void All_DefaultBindingsMatchDocumentedCaptureModes()
    {
        Assert.Collection(
            DefaultCaptureHotkeys.All,
            binding => AssertBinding(binding, CaptureMode.Region, 0x2C, "Print Screen", expectStandardModifiers: false),
            binding => AssertBinding(binding, CaptureMode.Region, 0x31, "Ctrl+Shift+1", expectStandardModifiers: true),
            binding => AssertBinding(binding, CaptureMode.Window, 0x32, "Ctrl+Shift+2", expectStandardModifiers: true),
            binding => AssertBinding(binding, CaptureMode.Scrolling, 0x33, "Ctrl+Shift+3", expectStandardModifiers: true),
            binding => AssertBinding(binding, CaptureMode.FullScreen, 0x34, "Ctrl+Shift+4", expectStandardModifiers: true));
    }

    [Fact]
    public void ImplementedNow_RegistersRegionWindowAndFullScreenBindings()
    {
        var bindings = DefaultCaptureHotkeys.ImplementedNow;

        Assert.Equal(4, bindings.Count);
        Assert.Equal(2, bindings.Count(binding => binding.Mode == CaptureMode.Region));
        Assert.Single(bindings, binding => binding.Mode == CaptureMode.Window);
        Assert.Single(bindings, binding => binding.Mode == CaptureMode.FullScreen);
        Assert.DoesNotContain(bindings, binding => binding.Mode == CaptureMode.Scrolling);
        Assert.Contains(bindings, binding => binding.GestureText == "Print Screen");
        Assert.Contains(bindings, binding => binding.GestureText == "Ctrl+Shift+1");
        Assert.Contains(bindings, binding => binding.GestureText == "Ctrl+Shift+2");
        Assert.Contains(bindings, binding => binding.GestureText == "Ctrl+Shift+4");
    }

    [Fact]
    public void BindingsUseUniqueIdsAndNoRepeatModifier()
    {
        var bindings = DefaultCaptureHotkeys.All;

        Assert.Equal(bindings.Count, bindings.Select(binding => binding.Id).Distinct().Count());
        Assert.All(
            bindings,
            binding => Assert.True(binding.Modifiers.HasFlag(HotkeyModifiers.NoRepeat)));
    }

    private static void AssertBinding(
        CaptureHotkeyBinding binding,
        CaptureMode mode,
        uint virtualKey,
        string gesture,
        bool expectStandardModifiers)
    {
        Assert.Equal(mode, binding.Mode);
        Assert.Equal(virtualKey, binding.VirtualKey);
        Assert.Equal(gesture, binding.GestureText);
        Assert.True(binding.Modifiers.HasFlag(HotkeyModifiers.NoRepeat));

        if (expectStandardModifiers)
        {
            Assert.True(binding.Modifiers.HasFlag(HotkeyModifiers.Control));
            Assert.True(binding.Modifiers.HasFlag(HotkeyModifiers.Shift));
        }
        else
        {
            Assert.False(binding.Modifiers.HasFlag(HotkeyModifiers.Control));
            Assert.False(binding.Modifiers.HasFlag(HotkeyModifiers.Shift));
        }
    }
}
