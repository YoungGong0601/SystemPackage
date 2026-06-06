using System;
using System.Collections.Generic;
using UnityEngine;

// ═══════════════════════════════════════
//  EventBus
// ═══════════════════════════════════════

public static class EventBus
{
    // ── Data ──────────────

    private static readonly Dictionary<Type, Delegate> listeners = new();

    // ── Init ──────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        listeners?.Clear();
    }


    // ── Public Methods ──────────────

    public static void Subscribe<T>(Action<T> callback) where T : struct
    => listeners[typeof(T)] = Delegate.Combine(listeners.GetValueOrDefault(typeof(T)), callback);

    public static void Unsubscribe<T>(Action<T> callback) where T : struct
    => listeners[typeof(T)] = Delegate.Remove(listeners.GetValueOrDefault(typeof(T)), callback); // null 쌓임. Trade-off 간단

    // Multicast delegate Invoke : Safely
    public static void Publish<T>(T eventData) where T : struct
    {
        if (!listeners.TryGetValue(typeof(T), out var del)) return;
        if (del is not Action<T> action) return;

        foreach (var handler in action.GetInvocationList())
        {
            try { ((Action<T>)handler).Invoke(eventData); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    public static void Clear<T>() where T : struct => listeners.Remove(typeof(T));
    public static void ClearAll() => listeners.Clear();


    // Shortcut
    public static void Sub<T>(Action<T> callback) where T : struct => Subscribe(callback);
    public static void Unsub<T>(Action<T> callback) where T : struct => Unsubscribe(callback);
    public static void Pub<T>(T eventData) where T : struct => Publish(eventData);
}

// ───────────────────────────────
//  Struct
// ───────────────────────────────

// public struct OnPlayerTurned { }
// public struct OnPlayerLanded { }
// public struct OnPlayerDashed { public int continusCount; public DashInfo info; }