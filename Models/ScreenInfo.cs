using CursorCage.Native;

namespace CursorCage.Models;

public sealed class ScreenInfo
{
    public required string Id { get; init; }
    public required RECT Bounds { get; init; }
}
