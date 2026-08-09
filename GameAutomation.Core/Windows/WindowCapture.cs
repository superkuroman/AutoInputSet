using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GameAutomation.Core.Windows;

public static partial class WindowCapture
{
    private const int Srccopy = 0x00CC0020;
    private const int CaptureBlt = 0x40000000;

    public static CapturedFrame CaptureClient(nint windowHandle)
    {
        if (!GetClientRect(windowHandle, out var rect) || rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException("The selected window has no capturable client area.");

        var clientOrigin = new NativePoint();
        if (!ClientToScreen(windowHandle, ref clientOrigin))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        // Capture the pixels composed on the desktop instead of the target window's GDI surface.
        // Hardware-accelerated applications such as Chrome may not render useful pixels into their window DC.
        var sourceDc = GetDC(0);
        if (sourceDc == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        var memoryDc = CreateCompatibleDC(sourceDc);
        var bitmap = CreateCompatibleBitmap(sourceDc, rect.Width, rect.Height);
        var previous = nint.Zero;

        try
        {
            if (memoryDc == 0 || bitmap == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            previous = SelectObject(memoryDc, bitmap);
            if (!BitBlt(memoryDc, 0, 0, rect.Width, rect.Height, sourceDc,
                    clientOrigin.X, clientOrigin.Y, Srccopy | CaptureBlt))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var stride = rect.Width * 4;
            var pixels = new byte[stride * rect.Height];
            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(), Width = rect.Width,
                    Height = -rect.Height, Planes = 1, BitCount = 32
                }
            };
            if (GetDIBits(memoryDc, bitmap, 0, (uint)rect.Height, pixels, ref info, 0) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return new CapturedFrame(rect.Width, rect.Height, stride, pixels);
        }
        finally
        {
            if (previous != 0) _ = SelectObject(memoryDc, previous);
            if (bitmap != 0) _ = DeleteObject(bitmap);
            if (memoryDc != 0) _ = DeleteDC(memoryDc);
            _ = ReleaseDC(0, sourceDc);
        }
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(nint handle, out NativeRect rect);
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClientToScreen(nint handle, ref NativePoint point);
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint GetDC(nint handle);
    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(nint handle, nint deviceContext);
    [LibraryImport("gdi32.dll", SetLastError = true)]
    private static partial nint CreateCompatibleDC(nint deviceContext);
    [LibraryImport("gdi32.dll", SetLastError = true)]
    private static partial nint CreateCompatibleBitmap(nint deviceContext, int width, int height);
    [LibraryImport("gdi32.dll")]
    private static partial nint SelectObject(nint deviceContext, nint graphicsObject);
    [LibraryImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, int operation);
    [LibraryImport("gdi32.dll", SetLastError = true)]
    private static partial int GetDIBits(nint deviceContext, nint bitmap, uint startScan, uint scanLines, [Out] byte[] bits, ref BitmapInfo info, uint usage);
    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint graphicsObject);
    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(nint deviceContext);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPelsPerMeter, YPelsPerMeter;
        public uint ColorsUsed, ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }
}
