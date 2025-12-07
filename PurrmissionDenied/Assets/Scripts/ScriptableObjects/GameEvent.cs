using System;
using UnityEngine;

public class GameEvent<T> : ScriptableObject, IGameEvent
{
    private event Action<T> listeners;

    /// <summary>
    /// Emit the chosen event
    /// </summary>
    public void Emit(T value)
    {
        listeners?.Invoke(value);
    }

    /// <summary>
    /// Subscribe to the event
    /// </summary>
    public void Subscribe(Action<T> listener)
    {
        listeners += listener;
    }

    /// <summary>
    /// Unsubscribe from the event
    /// </summary>
    public void Unsubscribe(Action<T> listener)
    {
        listeners -= listener;
    }

    public void ClearAll()
    {
        listeners = null;
    }

    void OnEnable()
    {
        listeners = null;
    }
}

public interface IGameEvent
{
    void ClearAll();
}
