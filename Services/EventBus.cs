using System.Collections.Concurrent;

namespace CursorCage.Services;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : class
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => []);
        lock (list)
            list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            return;
        lock (list)
            list.Remove(handler);
    }

    public void Publish<T>(T payload) where T : class
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            return;
        List<Delegate> snapshot;
        lock (list)
            snapshot = [..list];
        foreach (var d in snapshot)
        {
            if (d is Action<T> action)
                action(payload);
        }
    }
}
