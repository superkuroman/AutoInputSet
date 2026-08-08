using System.Runtime.InteropServices;
using System.Text;

namespace GameAutomation.Core.Windows;

public static partial class WindowFinder
{
    public static IReadOnlyList<GameWindowInfo> GetVisibleWindows()
    {
        var windows = new List<GameWindowInfo>();
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || GetWindowTextLength(handle) == 0) return true;
            if (!GetClientRect(handle, out var rect) || rect.Width <= 0 || rect.Height <= 0) return true;
            var title = new StringBuilder(GetWindowTextLength(handle) + 1);
            _ = GetWindowText(handle, title, title.Capacity);
            windows.Add(new GameWindowInfo(handle, title.ToString(), rect.Width, rect.Height));
            return true;
        }, 0);

        return windows.OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private delegate bool EnumWindowsProc(nint handle, nint parameter);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc callback, nint parameter);
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint handle);
    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
    private static partial int GetWindowTextLength(nint handle);
    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint handle, StringBuilder text, int maximumCount);
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(nint handle, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }
}
