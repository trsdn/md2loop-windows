using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace md2loop;

public sealed partial class MainWindow : Window
{
    // Design size in device-independent pixels (DIPs) at 96 DPI.
    private const int BaseWidthDips = 360;
    private const int BaseHeightDips = 300;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    private readonly IntPtr _hwnd;
    private DispatcherQueueTimer? _settleTimer;
    private int _settleTicks;
    private bool _initialSizeApplied;
    private bool _centered;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        _hwnd = WindowNative.GetWindowHandle(this);

        RootFrame.Navigate(typeof(MainPage));

        RootFrame.Loaded += (_, _) =>
        {
            // The rasterization scale is only trustworthy once the XAML tree is live;
            // GetDpiForWindow still reports 96 during construction and activation.
            if (RootFrame.XamlRoot is { } xamlRoot)
            {
                xamlRoot.Changed += (_, _) => EnforceMinimumSize();
            }

            EnforceMinimumSize();
            StartSettleTimer();
        };
    }

    /// <summary>
    /// Windows finishes the initial per-monitor DPI transition shortly after the
    /// content loads and rescales the window as part of it, which silently undoes
    /// the size we just applied. Re-assert the size for a moment until it sticks.
    /// </summary>
    private void StartSettleTimer()
    {
        var queue = DispatcherQueue.GetForCurrentThread();
        if (queue is null)
        {
            return;
        }

        _settleTimer = queue.CreateTimer();
        _settleTimer.Interval = TimeSpan.FromMilliseconds(100);
        _settleTimer.Tick += (timer, _) =>
        {
            EnforceMinimumSize();
            if (++_settleTicks >= 10)
            {
                timer.Stop();
                _settleTimer = null;
            }
        };
        _settleTimer.Start();
    }

    private void EnforceMinimumSize()
    {
        var scale = GetScaleFactor();
        var width = (int)Math.Round(BaseWidthDips * scale);
        var height = (int)Math.Round(BaseHeightDips * scale);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = width;
            presenter.PreferredMinimumHeight = height;
        }

        if (!GetWindowRect(_hwnd, out var rect))
        {
            return;
        }

        var currentWidth = rect.Right - rect.Left;
        var currentHeight = rect.Bottom - rect.Top;

        // The first pass applies the compact design size exactly. Later passes only
        // grow, so a user who enlarged the window keeps their size while the window
        // is still restored after the DPI transition rescales it.
        if (_initialSizeApplied)
        {
            if (currentWidth >= width && currentHeight >= height)
            {
                return;
            }

            width = Math.Max(currentWidth, width);
            height = Math.Max(currentHeight, height);
        }
        else if (currentWidth == width && currentHeight == height)
        {
            _initialSizeApplied = true;
            return;
        }

        _initialSizeApplied = true;

        // SetWindowPos is used instead of AppWindow.Resize because it is unambiguously
        // in physical pixels and is not reinterpreted by the pending DPI transition.
        SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, width, height,
            SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);

        if (!_centered)
        {
            _centered = true;
            CenterOnDisplay();
        }
    }

    private void CenterOnDisplay()
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        if (display is null || !GetWindowRect(_hwnd, out var rect))
        {
            return;
        }

        var work = display.WorkArea;
        var x = work.X + ((work.Width - (rect.Right - rect.Left)) / 2);
        var y = work.Y + ((work.Height - (rect.Bottom - rect.Top)) / 2);
        SetWindowPos(_hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private double GetScaleFactor()
    {
        if (RootFrame.XamlRoot is { RasterizationScale: > 0 } xamlRoot)
        {
            return xamlRoot.RasterizationScale;
        }

        var dpi = GetDpiForWindow(_hwnd);
        return dpi == 0 ? 1.0 : dpi / 96.0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
