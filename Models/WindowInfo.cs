namespace CursorCage.Models;

public sealed class WindowInfo
{
    public required string Id { get; init; }
    public required nint Handle { get; init; }
    public required string Title { get; init; }
}
