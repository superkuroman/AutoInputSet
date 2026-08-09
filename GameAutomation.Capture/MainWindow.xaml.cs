using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GameAutomation.Core.Windows;
using Microsoft.Win32;

namespace GameAutomation.Capture;

public partial class MainWindow : Window
{
    private const int VkLeftButton = 0x01;
    private const int VkRightButton = 0x02;

    private CapturedFrame? _currentFrame;
    private BitmapSource? _currentBitmap;
    private readonly DispatcherTimer _previewTimer;
    private readonly DispatcherTimer _windowPickerTimer;
    private bool _windowPickerWaitingForRelease;

    public MainWindow()
    {
        InitializeComponent();
        RefreshWindows();

        _previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _previewTimer.Tick += PreviewTimer_Tick;
        _previewTimer.Start();

        _windowPickerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _windowPickerTimer.Tick += WindowPickerTimer_Tick;
    }

    protected override void OnClosed(EventArgs e)
    {
        _previewTimer.Stop();
        _windowPickerTimer.Stop();
        base.OnClosed(e);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshWindows();

    private void RefreshWindows()
    {
        var selectedHandle = (WindowSelector.SelectedItem as GameWindowInfo)?.Handle;
        var windows = WindowFinder.GetVisibleWindows();
        WindowSelector.ItemsSource = windows;
        WindowSelector.SelectedItem = windows.FirstOrDefault(window => window.Handle == selectedHandle) ?? windows.FirstOrDefault();
        StatusText.Text = $"找到 {windows.Count} 個可見視窗。";
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureSelectedWindow(showError: true);
    }

    private void PickWindowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_windowPickerTimer.IsEnabled)
        {
            StopWindowPicker("已取消選取視窗。");
            return;
        }

        _windowPickerWaitingForRelease = true;
        PickWindowButton.Content = "取消選取";
        _previewTimer.Stop();
        _windowPickerTimer.Start();
        StatusText.Text = "將滑鼠移到目標視窗並按左鍵；按右鍵取消。";
    }

    private void WindowPickerTimer_Tick(object? sender, EventArgs e)
    {
        var leftButtonDown = IsKeyDown(VkLeftButton);
        var rightButtonDown = IsKeyDown(VkRightButton);

        if (rightButtonDown)
        {
            StopWindowPicker("已取消選取視窗。");
            return;
        }

        if (_windowPickerWaitingForRelease)
        {
            if (!leftButtonDown) _windowPickerWaitingForRelease = false;
            return;
        }

        var hoveredWindow = WindowFinder.GetWindowUnderCursor();
        if (hoveredWindow is not null)
            StatusText.Text = $"目前指向：{hoveredWindow.DisplayName}　按左鍵確認。";

        if (!leftButtonDown || hoveredWindow is null) return;

        SelectWindow(hoveredWindow);
        StopWindowPicker($"已選取：{hoveredWindow.DisplayName}");
        CaptureSelectedWindow(showError: true);
    }

    private void SelectWindow(GameWindowInfo selectedWindow)
    {
        var windows = WindowFinder.GetVisibleWindows().ToList();
        var existingWindow = windows.FirstOrDefault(window => window.Handle == selectedWindow.Handle);
        if (existingWindow is null)
        {
            windows.Add(selectedWindow);
            windows = windows.OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
            existingWindow = windows.First(window => window.Handle == selectedWindow.Handle);
        }

        WindowSelector.ItemsSource = windows;
        WindowSelector.SelectedItem = existingWindow;
    }

    private void StopWindowPicker(string status)
    {
        _windowPickerTimer.Stop();
        PickWindowButton.Content = "滑鼠選取視窗";
        StatusText.Text = status;
        _previewTimer.Start();
    }

    private static bool IsKeyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (AutoRefreshCheckBox.IsChecked == true)
            CaptureSelectedWindow(showError: false);
    }

    private void CaptureSelectedWindow(bool showError)
    {
        if (WindowSelector.SelectedItem is not GameWindowInfo window)
        {
            if (showError) StatusText.Text = "請先選擇視窗。";
            return;
        }

        try
        {
            _currentFrame = WindowCapture.CaptureClient(window.Handle);
            var bitmap = BitmapSource.Create(
                _currentFrame.Width, _currentFrame.Height, 96, 96,
                PixelFormats.Bgra32, null, _currentFrame.Pixels, _currentFrame.Stride);
            bitmap.Freeze();

            _currentBitmap = bitmap;
            ScreenshotImage.Source = null;
            ScreenshotImage.Source = bitmap;
            ScreenshotImage.InvalidateVisual();
            SavePngButton.IsEnabled = true;
            StatusText.Text = $"已更新 {window.Title}：{_currentFrame.Width} × {_currentFrame.Height}　{DateTime.Now:HH:mm:ss}";
        }
        catch (Exception exception)
        {
            if (showError) StatusText.Text = $"擷取失敗：{exception.Message}";
        }
    }

    private void SavePngButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentBitmap is null)
        {
            StatusText.Text = "請先擷取畫面。";
            return;
        }

        try
        {
            var templatesDirectory = Path.Combine(AppContext.BaseDirectory, "Templates");
            Directory.CreateDirectory(templatesDirectory);

            var dialog = new SaveFileDialog
            {
                Title = "儲存擷取畫面",
                Filter = "PNG 圖片 (*.png)|*.png",
                DefaultExt = ".png",
                AddExtension = true,
                InitialDirectory = templatesDirectory,
                FileName = $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (dialog.ShowDialog(this) != true) return;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_currentBitmap));
            using var stream = File.Create(dialog.FileName);
            encoder.Save(stream);
            StatusText.Text = $"PNG 已儲存：{dialog.FileName}";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"儲存失敗：{exception.Message}";
        }
    }

    private void ScreenshotImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentFrame is null || ScreenshotImage.ActualWidth <= 0 || ScreenshotImage.ActualHeight <= 0) return;

        var point = e.GetPosition(ScreenshotImage);
        var imageRatio = _currentFrame.Width / (double)_currentFrame.Height;
        var controlRatio = ScreenshotImage.ActualWidth / ScreenshotImage.ActualHeight;
        double displayedWidth, displayedHeight, offsetX, offsetY;

        if (controlRatio > imageRatio)
        {
            displayedHeight = ScreenshotImage.ActualHeight;
            displayedWidth = displayedHeight * imageRatio;
            offsetX = (ScreenshotImage.ActualWidth - displayedWidth) / 2;
            offsetY = 0;
        }
        else
        {
            displayedWidth = ScreenshotImage.ActualWidth;
            displayedHeight = displayedWidth / imageRatio;
            offsetX = 0;
            offsetY = (ScreenshotImage.ActualHeight - displayedHeight) / 2;
        }

        var normalizedX = (point.X - offsetX) / displayedWidth;
        var normalizedY = (point.Y - offsetY) / displayedHeight;
        if (normalizedX is < 0 or > 1 || normalizedY is < 0 or > 1)
        {
            CoordinateText.Text = "Pixel: —    Normalized: —";
            return;
        }

        var pixelX = Math.Clamp((int)(normalizedX * _currentFrame.Width), 0, _currentFrame.Width - 1);
        var pixelY = Math.Clamp((int)(normalizedY * _currentFrame.Height), 0, _currentFrame.Height - 1);
        CoordinateText.Text = $"Pixel: {pixelX}, {pixelY}    Normalized: {normalizedX:F6}, {normalizedY:F6}";
    }
}
