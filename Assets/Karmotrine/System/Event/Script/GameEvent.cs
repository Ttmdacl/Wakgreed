using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEvent", menuName = "GameSystem/GameEvent")]
public class GameEvent : ScriptableObject
{
    [NonSerialized] private readonly List<GameEventListener> listeners = new();
    private event Action Collback;
    private event Action<Transform> CollbackTransform;

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--) { listeners[i].OnEventRaised(); }

        if (Collback is not null)
        {
            Collback.Invoke();
        }
    }

    public void Raise(Transform transform)
    {
        for (int i = listeners.Count - 1; i >= 0; i--) { listeners[i].OnEventRaised(); }

        if (CollbackTransform is not null)
        {
            CollbackTransform.Invoke(transform);
        }
    }

    public void RegisterListener(GameEventListener listener) { listeners.Add(listener); }
    public void UnregisterListener(GameEventListener listener) { listeners.Remove(listener); }
    public void AddCollback(Action a) { Collback += a; }
    public void AddCollback(Action<Transform> a) { CollbackTransform += a; }
    public void RemoveCollback(Action a) { Collback -= a; }
    public void RemoveCollback(Action<Transform> a) { CollbackTransform -= a; }
}