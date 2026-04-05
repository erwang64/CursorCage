namespace CursorCage.Events;

public sealed class ToggleLockRequested
{
}

public sealed class LockStateChanged(bool isLocked)
{
    public bool IsLocked { get; } = isLocked;
}
