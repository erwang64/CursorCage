namespace CursorCage.Services;

public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : class;
    void Unsubscribe<T>(Action<T> handler) where T : class;
    void Publish<T>(T payload) where T : class;
}
