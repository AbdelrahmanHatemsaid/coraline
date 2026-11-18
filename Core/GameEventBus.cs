using OpenTK.Mathematics;

namespace Coraline.Core;

public readonly record struct NoiseEmittedEvent(Vector2 Origin, float Radius, string SourceTag);
public readonly record struct WorldShiftedEvent(WorldDimension NewDimension);
public readonly record struct GhostEyeCollectedEvent(int EyeId, int TotalCollected);

public sealed class GameEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Delegate>();
            _subscribers[eventType] = handlers;
        }

        handlers.Add(handler);
    }

    public void Publish<TEvent>(TEvent gameEvent) where TEvent : struct
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.ToArray())
        {
            ((Action<TEvent>)handler).Invoke(gameEvent);
        }
    }

    public bool Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        if (handler is null)
        {
            return false;
        }

        var eventType = typeof(TEvent);
        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            return false;
        }

        var removed = handlers.Remove(handler);
        if (handlers.Count == 0)
        {
            _subscribers.Remove(eventType);
        }

        return removed;
    }
}
