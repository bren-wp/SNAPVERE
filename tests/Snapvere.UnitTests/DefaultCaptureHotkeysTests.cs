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
            binding => AssertBinding(binding, CaptureMode.Region, 0x31, "Ctrl+Shift+1"),
            binding => AssertBinding(binding, CaptureMode.Window, 0x32, "Ctrl+Shift+2"),
            binding => AssertBinding(binding, CaptureMode.Scrolling, 0x33, "Ctrl+Shift+3"),
            binding => AssertBinding(binding, CaptureMode.FullScreen, 0x34, "Ctrl+Shift+4"));
    }

    [Fact]
    public void ImplementedNow_RegistersOnlyRealCaptureWorkflows()
    {
        var modes = DefaultCaptureHotkeys.ImplementedNow.Select(binding => binding.Mode).ToArray();

        Assert.Equal([CaptureMode.Region, CaptureMode.FullScreen], modes);
        Assert.DoesNotContain(CaptureMode.Window, modes);
        Assert.DoesNotContain(CaptureMode.Scrolling, modes);
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
        string gesture)
    {
        Assert.Equal(mode, binding.Mode);
        Assert.Equal(virtualKey, binding.VirtualKey);
        Assert.Equal(gesture, binding.GestureText);
        Assert.True(binding.Modifiers.HasFlag(HotkeyModifiers.Control));
        Assert.True(binding.Modifiers.HasFlag(HotkeyModifiers.Shift));
    }
}
