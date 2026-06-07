using System.Collections.Generic;
using UnityEngine;


// ═══════════════════════════════════════
//  Objects Load용 Static Manager
// ═══════════════════════════════════════

// Required in Object scripts : OnEnable, OnDisable -> Register, Unregister

public static class ObjectManager
{
    // Object Storage
    static readonly Dictionary<string, HashSet<GameObject>> Objects = new();

    // KEY Constants
    public const string ENEMY = "Enemy";
    public const string BULLET = "Bullet";

    // ── Init ──────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        Objects.Clear();
    }

    //━━━━━━━━━━ Public Methods ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public static void RegisterObject(string type, GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[ObjectManager] Trying to register a null object of type: " + type);
            return;
        }

        if (!Objects.TryGetValue(type, out var set))
        {
            Objects[type] = set = new();
        }

        // 등록을 시도하고,
        if (!set.Add(obj))
        { // 이미 등록된 객체인 경우 return;
            Debug.LogWarning($"[ObjectManager] Object of type {type} is already registered: {obj.name}");
            return;
        }

        // Invoke Event
        EventBus.Publish(new OnObjectsRegistered { key = type, obj = obj, currentSet = set });
    }

    public static void UnregisterObject(string type, GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[ObjectManager] Trying to unregister a null object of type: " + type);
            return;
        }

        if (!Objects.TryGetValue(type, out var set) || !set.Remove(obj))
        {
            Debug.LogWarning($"[ObjectManager] Object of type {type} is not registered: {obj.name}");
            return;
        }

        if (set.Count == 0) Objects.Remove(type);

        // Invoke Event
        EventBus.Publish(new OnObjectsUnregistered { key = type, obj = obj, currentSet = set });
    }

    public static bool TryGetObjects(string type, out IReadOnlyCollection<GameObject> objects)
    {
        if (Objects.TryGetValue(type, out var set))
        {
            objects = set;
            return true;
        }
        objects = null;
        return false;
    }
}