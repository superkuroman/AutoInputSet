namespace GameAutomation.Core.Windows;

public sealed record CapturedFrame(int Width, int Height, int Stride, byte[] Pixels);
