using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameAutomation.Core.Windows;

namespace GameAutomation.Capture;

public partial class MainWindow : Window
{
    private CapturedFrame? _currentFrame;

    public MainWindow()
    {
        InitializeComponent();
        RefreshWindows();
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
        if (WindowSelector.SelectedItem is not GameWindowInfo window)
        {
            StatusText.Text = "請先選擇視窗。";
            return;
        }

        try
        {
            _currentFrame = WindowCapture.CaptureClient(window.Handle);
            ScreenshotImage.Source = BitmapSource.Create(
                _currentFrame.Width, _currentFrame.Height, 96, 96,
                PixelFormats.Bgra32, null, _currentFrame.Pixels, _currentFrame.Stride);
            StatusText.Text = $"已擷取 {window.Title}：{_currentFrame.Width} × {_currentFrame.Height}";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"擷取失敗：{exception.Message}";
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
