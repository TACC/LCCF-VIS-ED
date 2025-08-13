using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

public class BrushManager : MonoBehaviour
{
    [Header("Scene")]
    public Camera cam;
    public LayerMask plateLayer;
    public float surfaceOffset = 0.002f;
    public float rayMaxDistance = 5f;

    [Header("Brush")]
    // small visual to see where we are pointing, will change to small sponge later maybe?
    public WashBrush brushPrefab;

    // active brushes by pointer id, EnhancedTouch fingerId, or -1 for mouse
    readonly Dictionary<int, WashBrush> _brushes = new Dictionary<int, WashBrush>();
    static List<WashBrush> _cache = new List<WashBrush>();
    public static IReadOnlyList<WashBrush> ActiveBrushes
    {
        get
        {
            _cache.Clear();
            if (_instance != null) _cache.AddRange(_instance._brushes.Values);
            return _cache;
        }
    }

    const int MouseId = -1;
    static BrushManager _instance;

    void Awake()
    {
        _instance = this;
        if (cam == null) cam = Camera.main;
        ETouch.EnhancedTouchSupport.Enable();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        ETouch.EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (Mouse.current != null)
        {
            Vector2 mpos = Mouse.current.position.ReadValue();
            bool mpressed = Mouse.current.leftButton.isPressed;
            var b = GetOrCreate(MouseId);
            b.UpdateFromPointer(cam, plateLayer, surfaceOffset, rayMaxDistance, mpos, mpressed);
        }

        // track all active touches
        var active = ETouch.Touch.activeTouches;
        var seenIds = HashSetPool<int>.Get();
        foreach (var t in active)
        {
            int id = t.finger.index;
            seenIds.Add(id);

            bool pressed = t.phase == UnityEngine.InputSystem.TouchPhase.Began
                        || t.phase == UnityEngine.InputSystem.TouchPhase.Moved
                        || t.phase == UnityEngine.InputSystem.TouchPhase.Stationary;

            var b = GetOrCreate(id);
            b.UpdateFromPointer(cam, plateLayer, surfaceOffset, rayMaxDistance, t.screenPosition, pressed);
        }

        var toRemove = ListPool<int>.Get();
        foreach (var kvp in _brushes)
        {
            if (kvp.Key == MouseId) continue;
            if (!seenIds.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
        {
            Destroy(_brushes[id].gameObject);
            _brushes.Remove(id);
        }
        ListPool<int>.Release(toRemove);
        HashSetPool<int>.Release(seenIds);
    }

    WashBrush GetOrCreate(int id)
    {
        if (_brushes.TryGetValue(id, out var b) && b != null)
            return b;

        var go = Instantiate(brushPrefab.gameObject, transform);
        b = go.GetComponent<WashBrush>();
        _brushes[id] = b;
        return b;
    }
}

static class ListPool<T>
{
    static readonly Stack<List<T>> pool = new Stack<List<T>>();
    public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>();
    public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
}
static class HashSetPool<T>
{
    static readonly Stack<HashSet<T>> pool = new Stack<HashSet<T>>();
    public static HashSet<T> Get() => pool.Count > 0 ? pool.Pop() : new HashSet<T>();
    public static void Release(HashSet<T> set) { set.Clear(); pool.Push(set); }
}
