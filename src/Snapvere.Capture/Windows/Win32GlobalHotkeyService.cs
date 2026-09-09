using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Snapvere.Capture.Hotkeys;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Registers system-wide capture hotkeys on an isolated native message
/// window. The WinUI UI thread never owns the Win32 message pump.
/// </summary>
public sealed class Win32GlobalHotkeyService : IGlobalHotkeyService
{
    private const uint WindowMessageHotkey = 0x0312;
    private const uint WindowMessageClose = 0x0010;
    private const uint WindowMessageDestroy = 0x0002;
    private static readonly nint MessageOnlyWindowParent = new(-3);

    private readonly object _gate = new();
    private readonly CaptureHotkeyBinding[] _bindings;
    private readonly IReadOnlyDictionary<int, CaptureHotkeyBinding> _bindingsById;
    private readonly NativeMethods.WindowProcedure _windowProcedure;
    private readonly ManualResetEventSlim _startupSignal = new(false);

    private Thread? _thread;
    private Exception? _startupException;
    private GlobalHotkeyRegistrationReport? _registrationReport;
    private nint _windowHandle;
    private bool _disposed;

    public Win32GlobalHotkeyService(IEnumerable<CaptureHotkeyBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        _bindings = bindings.ToArray();
        if (_bindings.Length == 0)
        {
            throw new ArgumentException("At least one global hotkey binding is required.", nameof(bindings));
        }

        if (_bindings.Select(binding => binding.Id).Distinct().Count() != _bindings.Length)
        {
            throw new ArgumentException("Global hotkey IDs must be unique.", nameof(bindings));
        }

        _bindingsById = _bindings.ToDictionary(binding => binding.Id);
        _windowProcedure = WindowProcedure;
    }

    public event EventHandler<CaptureHotkeyPressedEventArgs>? HotkeyPressed;

    public GlobalHotkeyRegistrationReport Start()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_registrationReport is not null)
            {
                return _registrationReport;
            }

            if (_thread is null)
            {
                _thread = new Thread(MessageLoop)
                {
                    IsBackground = true,
                    Name = "SNAPVERE Global Hotkeys"
                };
                _thread.Start();
            }
        }

        _startupSignal.Wait();

        if (_startupException is not null)
        {
            ExceptionDispatchInfo.Capture(_startupException).Throw();
        }

        return _registrationReport
            ?? throw new InvalidOperationException("The SNAPVERE hotkey host did not publish its registration state.");
    }

    public void Dispose()
    {
        Thread? thread;
        nint windowHandle;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            thread = _thread;
            windowHandle = _windowHandle;
        }

        if (windowHandle != nint.Zero)
        {
            _ = NativeMethods.PostMessage(windowHandle, WindowMessageClose, nuint.Zero, nint.Zero);
        }

        if (thread is not null && thread != Thread.CurrentThread && thread.IsAlive)
        {
            _ = thread.Join(TimeSpan.FromSeconds(2));
        }

        _startupSignal.Dispose();
    }

    private void MessageLoop()
    {
        var instance = NativeMethods.GetModuleHandle(null);
        var className = $"SNAPVERE_Hotkeys_{Environment.ProcessId}_{Guid.NewGuid():N}";
        ushort classAtom = 0;
        var registered = new List<CaptureHotkeyBinding>();

        try
        {
            var windowClass = new NativeMethods.WindowClassEx
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.WindowClassEx>(),
                Instance = instance,
                WindowProcedure = Marshal.GetFunctionPointerForDelegate(_windowProcedure),
                ClassName = className
            };

            classAtom = NativeMethods.RegisterClassEx(ref windowClass);
            if (classAtom == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterClassEx failed for the global-hotkey host.");
            }

            _windowHandle = NativeMethods.CreateWindowEx(
                0,
                className,
                "SNAPVERE Global Hotkeys",
                0,
                0,
                0,
                0,
                0,
                MessageOnlyWindowParent,
                nint.Zero,
                instance,
                nint.Zero);

            if (_windowHandle == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateWindowEx failed for the global-hotkey host.");
            }

            var conflicts = new List<HotkeyRegistrationFailure>();
            foreach (var binding in _bindings)
            {
                if (NativeMethods.RegisterHotKey(
                        _windowHandle,
                        binding.Id,
                        (uint)binding.Modifiers,
                        binding.VirtualKey))
                {
                    registered.Add(binding);
                }
                else
                {
                    conflicts.Add(new HotkeyRegistrationFailure(
                        binding,
                        Marshal.GetLastWin32Error()));
                }
            }

            _registrationReport = new GlobalHotkeyRegistrationReport(
                registered.AsReadOnly(),
                conflicts.AsReadOnly());
        }
        catch (Exception exception)
        {
            _startupException = exception;
        }
        finally
        {
            _startupSignal.Set();
        }

        if (_startupException is not null)
        {
            CleanupNativeResources(instance, className, classAtom, registered);
            return;
        }

        try
        {
            while (true)
            {
                var getMessageResult = NativeMethods.GetMessage(
                    out var message,
                    nint.Zero,
                    0,
                    0);

                if (getMessageResult == 0)
                {
                    break;
                }

                if (getMessageResult < 0)
                {
                    break;
                }

                _ = NativeMethods.TranslateMessage(ref message);
                _ = NativeMethods.DispatchMessage(ref message);
            }
        }
        finally
        {
            CleanupNativeResources(instance, className, classAtom, registered);
        }
    }

    private void CleanupNativeResources(
        nint instance,
        string className,
        ushort classAtom,
        IReadOnlyList<CaptureHotkeyBinding> registered)
    {
        var windowHandle = _windowHandle;

        if (windowHandle != nint.Zero)
        {
            foreach (var binding in registered)
            {
                _ = NativeMethods.UnregisterHotKey(windowHandle, binding.Id);
            }

            _ = NativeMethods.DestroyWindow(windowHandle);
            _windowHandle = nint.Zero;
        }

        if (classAtom != 0)
        {
            _ = NativeMethods.UnregisterClass(className, instance);
        }
    }

    private nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam)
    {
        if (message == WindowMessageHotkey)
        {
            var id = unchecked((int)wParam);
            if (_bindingsById.TryGetValue(id, out var binding))
            {
                try
                {
                    HotkeyPressed?.Invoke(this, new CaptureHotkeyPressedEventArgs(binding));
                }
                catch
                {
                    // A subscriber must not terminate the native message loop.
                }
            }

            return nint.Zero;
        }

        if (message == WindowMessageClose)
        {
            _ = NativeMethods.DestroyWindow(window);
            return nint.Zero;
        }

        if (message == WindowMessageDestroy)
        {
            NativeMethods.PostQuitMessage(0);
            return nint.Zero;
        }

        return NativeMethods.DefWindowProc(window, message, wParam, lParam);
    }

    private static class NativeMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            internal int X;
            internal int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Message
        {
            internal nint Window;
            internal uint Id;
            internal nuint WParam;
            internal nint LParam;
            internal uint Time;
            internal Point CursorPosition;
            internal uint Private;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WindowClassEx
        {
            internal uint Size;
            internal uint Style;
            internal nint WindowProcedure;
            internal int ClassExtraBytes;
            internal int WindowExtraBytes;
            internal nint Instance;
            internal nint Icon;
            internal nint Cursor;
            internal nint BackgroundBrush;
            internal string? MenuName;
            internal string ClassName;
            internal nint SmallIcon;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern nint GetModuleHandle(string? moduleName);

        [DllImport("user32.dll", EntryPoint = "RegisterClassExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern ushort RegisterClassEx(ref WindowClassEx windowClass);

        [DllImport("user32.dll", EntryPoint = "UnregisterClassW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterClass(string className, nint instance);

        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern nint CreateWindowEx(
            uint extendedStyle,
            string className,
            string windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            nint parent,
            nint menu,
            nint instance,
            nint parameter);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyWindow(nint window);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(nint window, int id);

        [DllImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
        internal static extern int GetMessage(out Message message, nint window, uint filterMin, uint filterMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TranslateMessage(ref Message message);

        [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
        internal static extern nint DispatchMessage(ref Message message);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        internal static extern nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);

        [DllImport("user32.dll")]
        internal static extern void PostQuitMessage(int exitCode);
    }
}
