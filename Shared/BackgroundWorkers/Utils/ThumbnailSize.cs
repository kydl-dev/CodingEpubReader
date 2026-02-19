namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Represents a thumbnail size configuration
/// </summary>
public class ThumbnailSize(int width, int height)
{
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;

    public override string ToString()
    {
        return $"{Width}x{Height}";
    }
}