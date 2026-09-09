using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Snapvere.App.Services;

public enum TrayCommand
{
    Show,
    RegionCapture,
    ScreenCapture,
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
}

/// <summary>
/// Native Windows notification-area integration. It runs on an isolated
/// message thread, restores the icon after Explorer restarts, and exposes
/// only capture commands that are implemented in the current product build.
/// </summary>
public sealed class Win32TrayIconService : ITrayIconService
{
    private const uint CallbackMessage = 0x8000 + 0x53;
    private const uint WindowMessageClose = 0x0010;
    private const uint WindowMessageDestroy = 0x0002;
    private const uint WindowMessageLeftButtonDoubleClick = 0x0203;
    private const uint WindowMessageRightButtonUp = 0x0205;

    private const uint NotifyIconAdd = 0x00000000;
    private const uint NotifyIconDelete = 0x00000002;
    private const uint NotifyIconMessage = 0x00000001;
    private const uint NotifyIconIcon = 0x00000002;
    private const uint NotifyIconTip = 0x00000004;

    private const uint MenuString = 0x00000000;
    private const uint MenuSeparator = 0x00000800;
    private const uint TrackPopupLeftAlign = 0x0000;
    private const uint TrackPopupBottomAlign = 0x0020;
    private const uint TrackPopupRightButton = 0x0002;
    private const uint TrackPopupReturnCommand = 0x0100;
    private const uint TrackPopupNoNotify = 0x0080;

    private const uint CommandShow = 1001;
    private const uint CommandRegion = 1002;
    private const uint CommandScreen = 1003;
    private const uint CommandExit = 1004;

    private static readonly nint MessageOnlyWindowParent = new(-3);

    private readonly object _gate = new();
    private readonly NativeMethods.WindowProcedure _windowProcedure;
    private readonly ManualResetEventSlim _startupSignal = new(false);

    private Thread? _thread;
    private Exception? _startupException;
    private nint _windowHandle;
    private nint _iconHandle;
    private uint _taskbarCreatedMessage;
    private bool _started;
    private bool _disposed;

    public Win32TrayIconService()
    {
        _windowProcedure = WindowProcedure;
    }

    public event EventHandler<TrayCommandEventArgs>? CommandInvoked;

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
            Flags = NotifyIconMessage | NotifyIconIcon | NotifyIconTip,
            CallbackMessage = CallbackMessage,
            Icon = _iconHandle,
            Tip = "SNAPVERE — Capture anything."
        };

    private nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam)
    {
        if (_taskbarCreatedMessage != 0 && message == _taskbarCreatedMessage)
        {
            try
            {
                AddNotificationIcon();
            }
            catch
            {
                // Explorer recovery is best effort. Existing capture workflows
                // and global hotkeys remain available even if tray recovery fails.
            }

            return nint.Zero;
        }

        if (message == CallbackMessage)
        {
            var mouseMessage = unchecked((uint)lParam.ToInt64());
            if (mouseMessage == WindowMessageLeftButtonDoubleClick)
            {
                RaiseCommand(TrayCommand.Show);
                return nint.Zero;
            }

            if (mouseMessage == WindowMessageRightButtonUp)
            {
                ShowContextMenu(window);
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

    private void ShowContextMenu(nint ownerWindow)
    {
        var menu = NativeMethods.CreatePopupMenu();
        if (menu == nint.Zero)
        {
            return;
        }

        try
        {
            _ = NativeMethods.AppendMenu(menu, MenuString, CommandShow, "Open SNAPVERE");
            _ = NativeMethods.AppendMenu(menu, MenuSeparator, 0, null);
            _ = NativeMethods.AppendMenu(menu, MenuString, CommandRegion, "Region Capture    Ctrl+Shift+1");
            _ = NativeMethods.AppendMenu(menu, MenuString, CommandScreen, "Screen Capture    Ctrl+Shift+4");
            _ = NativeMethods.AppendMenu(menu, MenuSeparator, 0, null);
            _ = NativeMethods.AppendMenu(menu, MenuString, CommandExit, "Exit");
            _ = NativeMethods.SetMenuDefaultItem(menu, CommandRegion, false);

            if (!NativeMethods.GetCursorPos(out var cursor))
            {
                return;
            }

            _ = NativeMethods.SetForegroundWindow(ownerWindow);
            var command = NativeMethods.TrackPopupMenu(
                menu,
                TrackPopupLeftAlign |
                TrackPopupBottomAlign |
                TrackPopupRightButton |
                TrackPopupReturnCommand |
                TrackPopupNoNotify,
                cursor.X,
                cursor.Y,
                0,
                ownerWindow,
                nint.Zero);

            switch (command)
            {
                case CommandShow:
                    RaiseCommand(TrayCommand.Show);
                    break;
                case CommandRegion:
                    RaiseCommand(TrayCommand.RegionCapture);
                    break;
                case CommandScreen:
                    RaiseCommand(TrayCommand.ScreenCapture);
                    break;
                case CommandExit:
                    RaiseCommand(TrayCommand.Exit);
                    break;
            }
        }
        finally
        {
            _ = NativeMethods.DestroyMenu(menu);
        }
    }

    private void RaiseCommand(TrayCommand command)
    {
        try
        {
            CommandInvoked?.Invoke(this, new TrayCommandEventArgs(command));
        }
        catch
        {
            // UI subscribers cannot be allowed to terminate the tray message loop.
        }
    }

    private void Cleanup(nint instance, string className, ushort classAtom)
    {
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
        private const int IconSize = 32;
        private const uint DibRgbColors = 0;
        private const uint BiRgb = 0;

        internal static nint CreateSnapvereIcon()
        {
            var bitmapInfo = new NativeMethods.BitmapInfo
            {
                Header = new NativeMethods.BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<NativeMethods.BitmapInfoHeader>(),
                    Width = IconSize,
                    Height = -IconSize,
                    Planes = 1,
                    BitCount = 32,
                    Compression = BiRgb,
                    SizeImage = IconSize * IconSize * 4
                }
            };

            var colorBitmap = NativeMethods.CreateDIBSection(
                nint.Zero,
                ref bitmapInfo,
                DibRgbColors,
                out var bits,
                nint.Zero,
                0);

            if (colorBitmap == nint.Zero || bits == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the SNAPVERE tray icon bitmap.");
            }

            var maskBitmap = nint.Zero;
            try
            {
                var pixels = BuildIconPixels();
                Marshal.Copy(pixels, 0, bits, pixels.Length);

                maskBitmap = NativeMethods.CreateBitmap(IconSize, IconSize, 1, 1, nint.Zero);
                if (maskBitmap == nint.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the SNAPVERE tray icon mask.");
                }

                var iconInfo = new NativeMethods.IconInfo
                {
                    IsIcon = true,
                    MaskBitmap = maskBitmap,
                    ColorBitmap = colorBitmap
                };

                var icon = NativeMethods.CreateIconIndirect(ref iconInfo);
                if (icon == nint.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the SNAPVERE tray icon.");
                }

                return icon;
            }
            finally
            {
                if (maskBitmap != nint.Zero)
                {
                    _ = NativeMethods.DeleteObject(maskBitmap);
                }

                _ = NativeMethods.DeleteObject(colorBitmap);
            }
        }

        private static byte[] BuildIconPixels()
        {
            var pixels = new byte[IconSize * IconSize * 4];

            for (var y = 0; y < IconSize; y++)
            {
                for (var x = 0; x < IconSize; x++)
                {
                    if (IsInsideRoundedSquare(x, y))
                    {
                        SetPixel(pixels, x, y, 10, 132, 255, 255);
                    }
                }
            }

            DrawLine(pixels, 7, 11, 7, 7, 255, 255, 255, 255, 2);
            DrawLine(pixels, 7, 7, 11, 7, 255, 255, 255, 255, 2);
            DrawLine(pixels, 21, 7, 25, 7, 255, 255, 255, 255, 2);
            DrawLine(pixels, 25, 7, 25, 11, 255, 255, 255, 255, 2);
            DrawLine(pixels, 7, 21, 7, 25, 255, 255, 255, 255, 2);
            DrawLine(pixels, 7, 25, 11, 25, 255, 255, 255, 255, 2);
            DrawLine(pixels, 21, 25, 25, 25, 255, 255, 255, 255, 2);
            DrawLine(pixels, 25, 21, 25, 25, 255, 255, 255, 255, 2);

            DrawLine(pixels, 20, 11, 14, 11, 255, 255, 255, 255, 2);
            DrawLine(pixels, 14, 11, 12, 13, 255, 255, 255, 255, 2);
            DrawLine(pixels, 12, 13, 19, 18, 255, 255, 255, 255, 2);
            DrawLine(pixels, 19, 18, 20, 20, 255, 255, 255, 255, 2);
            DrawLine(pixels, 20, 20, 18, 22, 255, 255, 255, 255, 2);
            DrawLine(pixels, 18, 22, 12, 22, 255, 255, 255, 255, 2);

            return pixels;
        }

        private static bool IsInsideRoundedSquare(int x, int y)
        {
            const int inset = 3;
            const int radius = 6;

            if (x < inset || x >= IconSize - inset || y < inset || y >= IconSize - inset)
            {
                return false;
            }

            var left = inset + radius;
            var right = IconSize - inset - radius - 1;
            var top = inset + radius;
            var bottom = IconSize - inset - radius - 1;

            if ((x >= left && x <= right) || (y >= top && y <= bottom))
            {
                return true;
            }

            var cornerX = x < left ? left : right;
            var cornerY = y < top ? top : bottom;
            var deltaX = x - cornerX;
            var deltaY = y - cornerY;
            return (deltaX * deltaX) + (deltaY * deltaY) <= radius * radius;
        }

        private static void DrawLine(
            byte[] pixels,
            int x0,
            int y0,
            int x1,
            int y1,
            byte red,
            byte green,
            byte blue,
            byte alpha,
            int thickness)
        {
            var dx = Math.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Math.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;

            while (true)
            {
                for (var oy = -(thickness / 2); oy <= thickness / 2; oy++)
                {
                    for (var ox = -(thickness / 2); ox <= thickness / 2; ox++)
                    {
                        SetPixel(pixels, x0 + ox, y0 + oy, red, green, blue, alpha);
                    }
                }

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var twiceError = 2 * error;
                if (twiceError >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (twiceError <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void SetPixel(
            byte[] pixels,
            int x,
            int y,
            byte red,
            byte green,
            byte blue,
            byte alpha)
        {
            if ((uint)x >= IconSize || (uint)y >= IconSize)
            {
                return;
            }

            var offset = ((y * IconSize) + x) * 4;
            pixels[offset] = blue;
            pixels[offset + 1] = green;
            pixels[offset + 2] = red;
            pixels[offset + 3] = alpha;
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
        internal struct BitmapInfoHeader
        {
            internal uint Size;
            internal int Width;
            internal int Height;
            internal ushort Planes;
            internal ushort BitCount;
            internal uint Compression;
            internal uint SizeImage;
            internal int XPelsPerMeter;
            internal int YPelsPerMeter;
            internal uint ColorsUsed;
            internal uint ColorsImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapInfo
        {
            internal BitmapInfoHeader Header;
            internal uint Colors;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IconInfo
        {
            [MarshalAs(UnmanagedType.Bool)]
            internal bool IsIcon;
            internal uint HotspotX;
            internal uint HotspotY;
            internal nint MaskBitmap;
            internal nint ColorBitmap;
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

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint CreatePopupMenu();

        [DllImport("user32.dll", EntryPoint = "AppendMenuW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AppendMenu(nint menu, uint flags, uint itemId, string? text);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyMenu(nint menu);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out Point point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(nint window);

        [DllImport("user32.dll", EntryPoint = "TrackPopupMenu", SetLastError = true)]
        internal static extern uint TrackPopupMenu(
            nint menu,
            uint flags,
            int x,
            int y,
            int reserved,
            nint window,
            nint rectangle);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetMenuDefaultItem(nint menu, uint item, [MarshalAs(UnmanagedType.Bool)] bool byPosition);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint CreateDIBSection(
            nint deviceContext,
            ref BitmapInfo bitmapInfo,
            uint usage,
            out nint bits,
            nint section,
            uint offset);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint CreateBitmap(
            int width,
            int height,
            uint planes,
            uint bitsPerPixel,
            nint bits);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint CreateIconIndirect(ref IconInfo iconInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(nint icon);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(nint graphicsObject);
    }
}
