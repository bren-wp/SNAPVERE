using Snapvere.Shared;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Snapvere.App.Services;

public enum TrayCommand
{
    Show,
    ShowMenu,
    RegionCapture,
    WindowCapture,
    ScreenCapture,
    StartScreenRecording,
    StopScreenRecording,
    OpenCaptureFolder,
    About,
    Exit
}

public sealed class TrayCommandEventArgs(TrayCommand command) : EventArgs
{
    public TrayCommand Command { get; } = command;
}

public interface ITrayIconService : IDisposable
{
    event EventHandler<TrayCommandEventArgs>? CommandInvoked;

    void Start();
    void SetScreenRecordingState(bool active);
}

/// <summary>
/// Native notification-area host. A normal left click starts Region Capture
/// immediately; a right click delegates to the branded WinUI tray menu.
/// SNAPVERE therefore stays capture-first without opening a dashboard.
/// </summary>
public sealed class Win32TrayIconService : ITrayIconService
{
    private const uint CallbackMessage = 0x8000 + 0x53;
    private const uint WindowMessageRefreshTooltip = 0x8000 + 0x54;
    private const uint WindowMessageRetryNotificationIcon = 0x8000 + 0x55;
    private const uint WindowMessageClose = 0x0010;
    private const uint WindowMessageDestroy = 0x0002;
    private const uint WindowMessageContextMenu = 0x007B;
    private const uint WindowMessageLeftButtonUp = 0x0202;
    private const uint WindowMessageLeftButtonDoubleClick = 0x0203;
    private const uint WindowMessageRightButtonUp = 0x0205;
    private const uint NotificationSelect = 0x0400;
    private const uint NotificationKeySelect = 0x0401;

    private const uint NotifyIconAdd = 0x00000000;
    private const uint NotifyIconModify = 0x00000001;
    private const uint NotifyIconDelete = 0x00000002;
    private const uint NotifyIconSetVersion = 0x00000004;
    private const uint NotifyIconMessage = 0x00000001;
    private const uint NotifyIconIcon = 0x00000002;
    private const uint NotifyIconTip = 0x00000004;
    private const uint NotifyIconShowTip = 0x00000080;
    private const uint NotifyIconVersion4 = 4;
    private const uint CallbackEventMask = 0x0000FFFF;

    private static readonly nint MessageOnlyWindowParent = new(-3);

    private readonly object _gate = new();
    private readonly NativeMethods.WindowProcedure _windowProcedure;
    private readonly ManualResetEventSlim _startupSignal = new(false);

    private Thread? _thread;
    private Timer? _trayRecoveryTimer;
    private Exception? _startupException;
    private nint _windowHandle;
    private nint _iconHandle;
    private uint _taskbarCreatedMessage;
    private bool _started;
    private bool _disposed;
    private int _trayRecoveryAttempt;
    private int _trayRecoveryToken;
    private long _lastRegionClickTicks;
    private int _screenRecordingActive;

    public Win32TrayIconService()
    {
        _windowProcedure = WindowProcedure;
        SnapvereLanguageState.CurrentLanguageChanged += OnCurrentLanguageChanged;
    }

    public event EventHandler<TrayCommandEventArgs>? CommandInvoked;

    public void SetScreenRecordingState(bool active)
    {
        _ = Interlocked.Exchange(ref _screenRecordingActive, active ? 1 : 0);

        nint windowHandle;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            windowHandle = _windowHandle;
        }

        if (windowHandle != nint.Zero)
        {
            _ = NativeMethods.PostMessage(
                windowHandle,
                WindowMessageRefreshTooltip,
                nuint.Zero,
                nint.Zero);
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_started)
            {
                return;
            }

            _thread = new Thread(MessageLoop)
            {
                IsBackground = true,
                Name = "SNAPVERE Tray"
            };
            _thread.Start();
        }

        _startupSignal.Wait();

        if (_startupException is not null)
        {
            ExceptionDispatchInfo.Capture(_startupException).Throw();
        }

        lock (_gate)
        {
            _started = true;
        }
    }

    public void Dispose()
    {
        SnapvereLanguageState.CurrentLanguageChanged -= OnCurrentLanguageChanged;

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

        CancelTrayRecoveryRetry();

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
        var className = $"SNAPVERE_Tray_{Environment.ProcessId}_{Guid.NewGuid():N}";
        ushort classAtom = 0;

        try
        {
            _taskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");
            if (_taskbarCreatedMessage == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterWindowMessage failed for TaskbarCreated.");
            }

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
                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterClassEx failed for the tray host.");
            }

            _windowHandle = NativeMethods.CreateWindowEx(
                0,
                className,
                "SNAPVERE Tray",
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
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateWindowEx failed for the tray host.");
            }

            _iconHandle = TrayIconFactory.CreateSnapvereIcon();
            AddNotificationIcon();
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
            Cleanup(instance, className, classAtom);
            return;
        }

        try
        {
            while (true)
            {
                var result = NativeMethods.GetMessage(out var message, nint.Zero, 0, 0);
                if (result == 0)
                {
                    break;
                }

                if (result < 0)
                {
                    break;
                }

                _ = NativeMethods.TranslateMessage(ref message);
                _ = NativeMethods.DispatchMessage(ref message);
            }
        }
        finally
        {
            Cleanup(instance, className, classAtom);
        }
    }

    private void AddNotificationIcon()
    {
        var data = CreateNotifyIconData();
        if (!NativeMethods.ShellNotifyIcon(NotifyIconAdd, ref data))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Shell_NotifyIcon could not add the SNAPVERE tray icon.");
        }

        data.TimeoutOrVersion = NotifyIconVersion4;
        if (!NativeMethods.ShellNotifyIcon(NotifyIconSetVersion, ref data))
        {
            _ = NativeMethods.ShellNotifyIcon(NotifyIconDelete, ref data);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Shell_NotifyIcon could not enable the version 4 tray protocol.");
        }
    }

    private void BeginTrayRecovery()
    {
        CancelTrayRecoveryRetry();
        _ = Interlocked.Increment(ref _trayRecoveryToken);
        _trayRecoveryAttempt = 0;
        TryRecoverNotificationIcon();
    }

    private void TryRecoverNotificationIcon()
    {
        lock (_gate)
        {
            if (_disposed || _windowHandle == nint.Zero)
            {
                return;
            }
        }

        _trayRecoveryAttempt++;
        try
        {
            AddNotificationIcon();
            _trayRecoveryAttempt = 0;
            _ = Interlocked.Increment(ref _trayRecoveryToken);
            CancelTrayRecoveryRetry();
            StartupDiagnostics.WriteLine("SNAPVERE tray icon restored after Explorer notification-area recreation.");
        }
        catch (Win32Exception exception)
        {
            var delay = TrayIconRecoveryPolicy.GetRetryDelayAfterFailure(_trayRecoveryAttempt);
            if (delay is null)
            {
                StartupDiagnostics.Record("Restore tray icon after Explorer restart", exception);
                return;
            }

            ScheduleTrayRecoveryRetry(delay.Value);
        }
    }

    private void ScheduleTrayRecoveryRetry(TimeSpan delay)
    {
        Timer? previousTimer;
        var token = Interlocked.Increment(ref _trayRecoveryToken);

        lock (_gate)
        {
            if (_disposed || _windowHandle == nint.Zero)
            {
                return;
            }

            previousTimer = _trayRecoveryTimer;
            _trayRecoveryTimer = new Timer(
                _ =>
                {
                    nint currentWindow;
                    lock (_gate)
                    {
                        if (_disposed || token != Volatile.Read(ref _trayRecoveryToken))
                        {
                            return;
                        }

                        currentWindow = _windowHandle;
                    }

                    if (currentWindow != nint.Zero)
                    {
                        _ = NativeMethods.PostMessage(
                            currentWindow,
                            WindowMessageRetryNotificationIcon,
                            unchecked((nuint)(uint)token),
                            nint.Zero);
                    }
                },
                null,
                delay,
                Timeout.InfiniteTimeSpan);
        }

        previousTimer?.Dispose();
    }

    private void CancelTrayRecoveryRetry()
    {
        Timer? timer;
        lock (_gate)
        {
            timer = _trayRecoveryTimer;
            _trayRecoveryTimer = null;
        }

        timer?.Dispose();
    }

    private void RefreshNotificationIcon()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        var data = CreateNotifyIconData();
        _ = NativeMethods.ShellNotifyIcon(NotifyIconModify, ref data);
    }

    private void DeleteNotificationIcon()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        var data = CreateNotifyIconData();
        _ = NativeMethods.ShellNotifyIcon(NotifyIconDelete, ref data);
    }

    private NativeMethods.NotifyIconData CreateNotifyIconData()
        => new()
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.NotifyIconData>(),
            Window = _windowHandle,
            Id = 1,
            Flags = NotifyIconMessage | NotifyIconIcon | NotifyIconTip | NotifyIconShowTip,
            CallbackMessage = CallbackMessage,
            Icon = _iconHandle,
            Tip = Volatile.Read(ref _screenRecordingActive) != 0
                ? $"SNAPVERE — {SnapvereLocalization.T("ScreenRecordingActive", SnapvereLanguageState.CurrentLanguageCode)}"
                : $"SNAPVERE — {SnapvereLocalization.T("CaptureRegion", SnapvereLanguageState.CurrentLanguageCode)}",
            Info = string.Empty,
            InfoTitle = string.Empty
        };

    private nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam)
    {
        if (_taskbarCreatedMessage != 0 && message == _taskbarCreatedMessage)
        {
            BeginTrayRecovery();
            return nint.Zero;
        }

        if (message == WindowMessageRetryNotificationIcon)
        {
            var token = unchecked((int)(uint)wParam);
            if (token != Volatile.Read(ref _trayRecoveryToken))
            {
                return nint.Zero;
            }

            CancelTrayRecoveryRetry();
            TryRecoverNotificationIcon();
            return nint.Zero;
        }

        if (message == WindowMessageRefreshTooltip)
        {
            RefreshNotificationIcon();
            return nint.Zero;
        }

        if (message == CallbackMessage)
        {
            var notification = unchecked((uint)lParam.ToInt64()) & CallbackEventMask;
            if (notification is WindowMessageLeftButtonUp
                or WindowMessageLeftButtonDoubleClick
                or NotificationSelect
                or NotificationKeySelect)
            {
                RaiseRegionCaptureDebounced();
                return nint.Zero;
            }

            if (notification is WindowMessageRightButtonUp or WindowMessageContextMenu)
            {
                RaiseCommand(TrayCommand.ShowMenu);
                return nint.Zero;
            }
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

    private void OnCurrentLanguageChanged(object? sender, EventArgs e)
    {
        nint windowHandle;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            windowHandle = _windowHandle;
        }

        if (windowHandle != nint.Zero)
        {
            _ = NativeMethods.PostMessage(windowHandle, WindowMessageRefreshTooltip, nuint.Zero, nint.Zero);
        }
    }

    private void RaiseRegionCaptureDebounced()
    {
        var now = Environment.TickCount64;
        var previous = Interlocked.Exchange(ref _lastRegionClickTicks, now);
        if (previous != 0 && now - previous < 350)
        {
            return;
        }

        RaiseCommand(TrayCommand.RegionCapture);
    }

    private void RaiseCommand(TrayCommand command)
    {
        try
        {
            CommandInvoked?.Invoke(this, new TrayCommandEventArgs(command));
        }
        catch
        {
            // UI subscribers cannot terminate the native tray message loop.
        }
    }

    private void Cleanup(nint instance, string className, ushort classAtom)
    {
        CancelTrayRecoveryRetry();
        DeleteNotificationIcon();

        if (_iconHandle != nint.Zero)
        {
            _ = NativeMethods.DestroyIcon(_iconHandle);
            _iconHandle = nint.Zero;
        }

        var windowHandle = _windowHandle;
        if (windowHandle != nint.Zero)
        {
            _ = NativeMethods.DestroyWindow(windowHandle);
            _windowHandle = nint.Zero;
        }

        if (classAtom != 0)
        {
            _ = NativeMethods.UnregisterClass(className, instance);
        }
    }

    private static class TrayIconFactory
    {
        // Exact source asset shared with all four browser extensions.
        // HICON ownership remains with the tray service (DestroyIcon on shutdown).
        internal static nint CreateSnapvereIcon()
        {
            var png = Path.Combine(AppContext.BaseDirectory, "Assets", "SNAPVERE-app-icon-32.png");
            if (!File.Exists(png))
            {
                throw new FileNotFoundException("The SNAPVERE premium tray icon is missing.", png);
            }

            var input = new NativeMethods.GdiplusStartupInput { GdiplusVersion = 1 };
            var status = NativeMethods.GdiplusStartup(out var token, ref input, nint.Zero);
            if (status != 0) { throw new InvalidOperationException($"GDI+ initialization failed: {status}."); }

            nint bitmap = nint.Zero;
            try
            {
                status = NativeMethods.GdipCreateBitmapFromFile(png, out bitmap);
                if (status != 0 || bitmap == nint.Zero)
                {
                    throw new InvalidOperationException($"Premium PNG decoding failed: {status}.");
                }

                status = NativeMethods.GdipCreateHICONFromBitmap(bitmap, out var icon);
                if (status != 0 || icon == nint.Zero)
                {
                    if (icon != nint.Zero) { _ = NativeMethods.DestroyIcon(icon); }
                    throw new InvalidOperationException($"Premium tray icon creation failed: {status}.");
                }

                return icon;
            }
            finally
            {
                if (bitmap != nint.Zero) { _ = NativeMethods.GdipDisposeImage(bitmap); }
                NativeMethods.GdiplusShutdown(token);
            }
        }
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NotifyIconData
        {
            internal uint Size;
            internal nint Window;
            internal uint Id;
            internal uint Flags;
            internal uint CallbackMessage;
            internal nint Icon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string Tip;
            internal uint State;
            internal uint StateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            internal string Info;
            internal uint TimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            internal string InfoTitle;
            internal uint InfoFlags;
            internal Guid GuidItem;
            internal nint BalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GdiplusStartupInput
        {
            internal uint GdiplusVersion;
            internal nint DebugEventCallback;
            [MarshalAs(UnmanagedType.Bool)] internal bool SuppressBackgroundThread;
            [MarshalAs(UnmanagedType.Bool)] internal bool SuppressExternalCodecs;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern nint GetModuleHandle(string? moduleName);

        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint RegisterWindowMessage(string message);

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

        [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);

        [DllImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
        internal static extern int GetMessage(out Message message, nint window, uint filterMin, uint filterMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TranslateMessage(ref Message message);

        [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
        internal static extern nint DispatchMessage(ref Message message);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        internal static extern nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);

        [DllImport("user32.dll")]
        internal static extern void PostQuitMessage(int exitCode);

        [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShellNotifyIcon(uint message, ref NotifyIconData data);

        [DllImport("gdiplus.dll", ExactSpelling = true)]
        internal static extern int GdiplusStartup(out nuint token, ref GdiplusStartupInput input, nint output);

        [DllImport("gdiplus.dll", ExactSpelling = true)]
        internal static extern void GdiplusShutdown(nuint token);

        [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int GdipCreateBitmapFromFile(
            [MarshalAs(UnmanagedType.LPWStr)] string filename, out nint bitmap);

        [DllImport("gdiplus.dll", ExactSpelling = true)]
        internal static extern int GdipCreateHICONFromBitmap(nint bitmap, out nint icon);

        [DllImport("gdiplus.dll", ExactSpelling = true)]
        internal static extern int GdipDisposeImage(nint bitmap);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(nint icon);

    }
}
