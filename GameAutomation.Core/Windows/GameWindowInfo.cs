namespace GameAutomation.Core.Windows;

public sealed record GameWindowInfo(nint Handle, string Title, int ClientWidth, int ClientHeight)
{
    public string DisplayName => $"{Title}  ({ClientWidth} × {ClientHeight})";
}
