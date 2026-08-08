namespace GameAutomation.Core.Coordinates;

public readonly record struct NormalizedPoint
{
    public NormalizedPoint(double x, double y)
    {
        if (x is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(x));
        if (y is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(y));
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }

    public PixelPoint ToPixel(int width, int height) => new(
        Math.Clamp((int)Math.Round(X * width), 0, Math.Max(0, width - 1)),
        Math.Clamp((int)Math.Round(Y * height), 0, Math.Max(0, height - 1)));
}

public readonly record struct PixelPoint(int X, int Y);
