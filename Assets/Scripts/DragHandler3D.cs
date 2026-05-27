using UnityEngine;
using System.Collections;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SortableItem3D))]
public class DragHandler3D : MonoBehaviour
{
    public enum DragMode  { BurgerBuild, Sorting }
    public enum DepthMode { SurfacePlane, CameraPlane, WorldZ }

    [Header("Mode")]
    public DragMode mode = DragMode.Sorting;
    public DepthMode depthMode = DepthMode.SurfacePlane;

    [Header("References")]
    [SerializeField] private Camera dragCamera;
    [SerializeField] private Transform dragSurface;
    [SerializeField] private Vector3 surfaceNormalLocal = Vector3.up;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BurgerStackManager stackManager;

    [Header("Clamps (optional, world X/Y)")]
    public bool useClamps = false;
    public Vector2 sortingClampX = new Vector2(-3f, 3f);
    public Vector2 sortingClampY = new Vector2(-1f, 2.8f);
    public Vector2 burgerClampX  = new Vector2(-3.3f, 3.3f);
    public Vector2 burgerClampY  = new Vector2(0.5f, 2.5f);

    [Header("Physics")]
    public bool keepKinematicAlways = true;
    public bool freezeRotation = true;

    [Header("Diagnostics")]
    public bool logDebug = false;

    // -------- internals --------
    private Rigidbody rb;
    private SortableItem3D sortable;
    private Quaternion originalRotation;
    private Vector3 initialLocalScale;

    private bool dragging;
    private Vector3 grabOffset;
    private Plane dragPlane;
    private float worldZ;

    // Burger-only
    private bool isOverDropZone;
    private Transform currentDropTarget;

    [SerializeField] private int ingredientValueBacking;
    public int ingredientValue
    {
        get => sortable ? sortable.value : ingredientValueBacking;
        set { ingredientValueBacking = value; if (sortable) sortable.value = value; }
    }

    public bool IsInStack { get; private set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sortable = GetComponent<SortableItem3D>();
        originalRotation = transform.rotation;
        initialLocalScale = transform.localScale;

        if (rb)
        {
            if (keepKinematicAlways) rb.isKinematic = true;
            if (freezeRotation) rb.constraints |= RigidbodyConstraints.FreezeRotation;
        }
    }

    void Start()
    {
        if (mode == DragMode.BurgerBuild && !stackManager)
        {
#if UNITY_2023_1_OR_NEWER
            stackManager = FindFirstObjectByType<BurgerStackManager>();
#else
            stackManager = FindObjectOfType<BurgerStackManager>();
#endif
        }
    }

    private static Vector2 PointerPos()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private Camera Cam() => dragCamera ? dragCamera : Camera.main;

    void OnMouseDown()
    {
        var cam = Cam();
        if (!cam)
        {
            Debug.LogError("DragHandler3D: No camera assigned and no MainCamera tagged.", this);
            return;
        }

        StopAllCoroutines();

        Ray ray = cam.ScreenPointToRay(PointerPos());
        if (!Physics.Raycast(ray, out var hit, 1000f, ~0, QueryTriggerInteraction.Collide)) return;
        if (hit.collider.gameObject != gameObject) return;

        if (depthMode == DepthMode.SurfacePlane)
        {
            Vector3 n = dragSurface
                ? dragSurface.TransformDirection(surfaceNormalLocal).normalized
                : -cam.transform.forward;
            dragPlane = new Plane(n, hit.point);
        }
        else if (depthMode == DepthMode.CameraPlane)
        {
            dragPlane = new Plane(-cam.transform.forward, hit.point);
        }
        else
        {
            worldZ = transform.position.z;
            dragPlane = new Plane(Vector3.forward, new Vector3(0, 0, worldZ));
        }

        grabOffset = transform.position - hit.point;
        dragging = true;

        // stop physics BEFORE making kinematic (prevents warnings)
        if (rb && !keepKinematicAlways)
        {
#if UNITY_6000_0_OR_NEWER
            if (!rb.isKinematic) rb.linearVelocity  = Vector3.zero;
#else
            if (!rb.isKinematic) rb.velocity        = Vector3.zero;
#endif
            if (!rb.isKinematic) rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    void OnMouseUp()
    {
        if (rb && !keepKinematicAlways) rb.isKinematic = true;

        if (mode == DragMode.Sorting)
        {
            var mgr  = ConveyorManager3D.Instance;
            var item = sortable;
            bool inZone = (mgr && mgr.IsInConveyorZone(transform.position));
            if (inZone && item != null)
            {
                bool placed = mgr.TryPlace(item);
                if (!placed) ResetToSpawnPoint();
            }
            else ResetToSpawnPoint();

            dragging = false;
            return;
        }

        // BurgerBuild
        if (isOverDropZone && stackManager) stackManager.StackItem(gameObject);
        else                                ResetToSpawnPoint();

        dragging = false;
    }

    void Update()
    {
        if (!dragging) return;
        var cam = Cam(); if (!cam) return;

        Ray ray = cam.ScreenPointToRay(PointerPos());
        if (dragPlane.Raycast(ray, out float t))
        {
            Vector3 p = ray.GetPoint(t) + grabOffset;

            if (useClamps)
            {
                if (mode == DragMode.BurgerBuild)
                {
                    p.x = Mathf.Clamp(p.x, burgerClampX.x, burgerClampX.y);
                    p.y = Mathf.Clamp(p.y, burgerClampY.x, burgerClampY.y);
                }
                else
                {
                    p.x = Mathf.Clamp(p.x, sortingClampX.x, sortingClampX.y);
                    p.y = Mathf.Clamp(p.y, sortingClampY.x, sortingClampY.y);
                }
            }

            if (rb) rb.MovePosition(p);
            else    transform.position = p;
        }
    }

    // -------- Utilities --------
    public void ResetToSpawnPoint()
    {
        if (mode == DragMode.BurgerBuild)
        {
            stackManager?.RemoveFromStack(gameObject);
            MarkInStack(false);
        }

        if (!spawnPoint)
        {
            if (logDebug) Debug.LogWarning($"[{name}] ResetToSpawnPoint: no spawnPoint.", this);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ReturnToSpawnRoutine());

        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.enabled = true;
    }

    // Smooth glide back to spawn (animate in world space first, THEN parent)
    private IEnumerator ReturnToSpawnRoutine()
    {
        // 0) Cache world start/end BEFORE any parenting
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 endPos = spawnPoint.position;
        Quaternion endRot = (mode == DragMode.Sorting) ? startRot : spawnPoint.rotation;

        // 1) Silence physics
        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            if (!rb.isKinematic) rb.linearVelocity  = Vector3.zero;
#else
            if (!rb.isKinematic) rb.velocity        = Vector3.zero;
#endif
            if (!rb.isKinematic) rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 2) Disable colliders during glide
        var cols = GetComponentsInChildren<Collider>(includeInactive: true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        // 3) Animate in world space (no parenting yet)
        float dur = 0.25f; // matches your stack-in feel
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = t / dur;

            Vector3 p = Vector3.Lerp(startPos, endPos, a);
            // Use transform for reliable world-space interpolation
            transform.position = p;

            if (mode != DragMode.Sorting)
                transform.rotation = Quaternion.Slerp(startRot, endRot, a);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        // 4) Now parent under spawn while keeping the world pose
        transform.SetParent(spawnPoint, worldPositionStays: true);
        transform.localScale = initialLocalScale;

        // (Optional tidy) ensure locals are clean; this won't visually change if we are exactly at 'endPos'
        transform.localPosition = Vector3.zero;
        if (mode == DragMode.BurgerBuild) transform.localRotation = Quaternion.identity;

        // 5) Re-enable colliders next frame
        yield return null;
        for (int i = 0; i < cols.Length; i++) if (cols[i]) cols[i].enabled = true;

        if (logDebug) Debug.Log($"[RETURN] {name} -> {spawnPoint.name} (mode={mode})", this);
    }

    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
        originalRotation   = transform.rotation;
        initialLocalScale  = transform.localScale;
    }

    public void MarkInStack(bool value)
    {
        IsInStack = value;
        gameObject.tag = value ? "InStack" : "InSpawn";
    }

    // Burger triggers
    void OnTriggerEnter(Collider other)
    {
        if (mode != DragMode.BurgerBuild) return;
        if (other.CompareTag("DropZone") || other.GetComponent<DropTarget>())
        {
            isOverDropZone = true;
            currentDropTarget = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (mode != DragMode.BurgerBuild) return;
        if (other.CompareTag("DropZone") || other.GetComponent<DropTarget>())
        {
            isOverDropZone = false;
            currentDropTarget = null;
        }
    }
}
