using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

public class BrushManager : MonoBehaviour
{
    public bool inputEnabled = true;

    [Header("Scene")]
    public Camera cam;
    public LayerMask plateLayer;
    public float surfaceOffset = 0.001f;
    public float rayMaxDistance = 5f;


    [Header("Brush")]
    public WashBrush brushPrefab;
    private int brushNum = 1; //number of brushes for an input
    private float brushOffset = 2.5f; //space between new brushes, should be going to left: fingerb - - - newb

    // active brushes by pointer id, EnhancedTouch fingerId, or -1 for mouse
    readonly Dictionary<int, List<WashBrush>> _brushes = new Dictionary<int, List<WashBrush>>();
    static List<WashBrush> _cache = new List<WashBrush>();
    public static IReadOnlyList<WashBrush> ActiveBrushes
    {
        get
        {
            _cache.Clear();
            if (_instance != null)
            {
                foreach (var list in _instance._brushes.Values)
                    _cache.AddRange(list);
            }
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

    //used for adding brushes for parallelize.
    public void addBrushes()
    {
        if (brushNum < 3)
        {
            brushNum++;
            foreach (int id in _brushes.Keys)
            {
                addBrush(id);
            }
        }
    }

    private void addBrush(int id)
    {
        var list = GetOrCreate(id);
        var go = Instantiate(brushPrefab.gameObject, transform);
        var brush = go.GetComponent<WashBrush>();
        brush.newBrushOffset = ComputeOffset(list.Count);
        list.Add(brush);
    }

    Vector2 ComputeOffset(int index)
    {
        return new Vector2(-index * brushOffset, 0f);
    }

    void Update()
    {
        if (!inputEnabled)
        {
            // prevent scrubbing and also clear existing brushes so DirtSpot sees none
            if (_brushes.Count > 0)
            {
                foreach (var kv in _brushes)
                { foreach (var brush in kv.Value) if (brush) Destroy(brush.gameObject); }
                _brushes.Clear();
            }
            return;
        }
        if (Mouse.current != null)
        {
            Vector2 mpos = Mouse.current.position.ReadValue();
            bool mpressed = Mouse.current.leftButton.isPressed;
            List<WashBrush> bList = GetOrCreate(MouseId);
            Ray ray = cam.ScreenPointToRay(mpos);

            if (Physics.Raycast(ray, out var hit, rayMaxDistance, plateLayer))
            {
                var plate = hit.collider.GetComponentInParent<PlateController>();
                if (plate != null)
                    foreach (var brush in bList)
                    {
                        brush.UpdateFromPointer(cam, plate, surfaceOffset, mpos, mpressed);
                        brush.transform.position += plate.transform.right * brush.newBrushOffset.x
                                                   + plate.transform.up * brush.newBrushOffset.y;
                    }

            }
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

            List<WashBrush> bList = GetOrCreate(id);
            Ray ray = cam.ScreenPointToRay(t.screenPosition);

            if (Physics.Raycast(ray, out var hit, rayMaxDistance, plateLayer))
            {
                var plate = hit.collider.GetComponentInParent<PlateController>();
                if (plate != null)
                {
                    foreach (var brush in bList)
                    {
                        brush.UpdateFromPointer(cam, plate, surfaceOffset, t.screenPosition, pressed);
                        brush.transform.position += plate.transform.right * brush.newBrushOffset.x
                                                   + plate.transform.up * brush.newBrushOffset.y;
                    }
                }
            }
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
            List<WashBrush> brushes = _brushes[id];
            foreach (var brush in brushes)
            {
                Destroy(brush.gameObject);
            }
            _brushes.Remove(id);
        }
        ListPool<int>.Release(toRemove);
        HashSetPool<int>.Release(seenIds);
    }

    List<WashBrush> GetOrCreate(int id)
    {
        if (_brushes.TryGetValue(id, out var list) && list != null && list.Count > 0)
            return list;

        list = new List<WashBrush>();
        _brushes[id] = list;
        for (int i = 0; i < brushNum; i++)
        {
            var go = Instantiate(brushPrefab.gameObject, transform);
            var brush = go.GetComponent<WashBrush>();
            brush.newBrushOffset = ComputeOffset(i);
            list.Add(brush);
        }
        return list;
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
