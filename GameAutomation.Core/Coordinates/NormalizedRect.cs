namespace GameAutomation.Core.Coordinates;

public readonly record struct NormalizedRect
{
    public NormalizedRect(double left, double top, double right, double bottom)
    {
        if (left is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(left));
        if (top is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(top));
        if (right is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(right));
        if (bottom is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(bottom));
        if (right <= left) throw new ArgumentException("Right must be greater than left.");
        if (bottom <= top) throw new ArgumentException("Bottom must be greater than top.");
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public double Left { get; }
    public double Top { get; }
    public double Right { get; }
    public double Bottom { get; }
}
