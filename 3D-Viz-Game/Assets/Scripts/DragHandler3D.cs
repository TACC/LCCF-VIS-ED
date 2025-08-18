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
    [SerializeField] private Camera dragCamera;                 // Assign your active gameplay camera
    [SerializeField] private Transform dragSurface;             // Board/table transform (for SurfacePlane)
    [SerializeField] private Vector3 surfaceNormalLocal = Vector3.up; // Board's local normal axis
    [SerializeField] private Transform spawnPoint;              // Optional return target
    [SerializeField] private BurgerStackManager stackManager;   // Burger mode only

    [Header("Clamps (optional, world X/Y)")]
    public bool useClamps = false;
    public Vector2 sortingClampX = new Vector2(-3f, 3f);
    public Vector2 sortingClampY = new Vector2(-1f, 2.8f);
    public Vector2 burgerClampX  = new Vector2(-3.3f, 3.3f);
    public Vector2 burgerClampY  = new Vector2(0.5f, 2.5f);

    [Header("Physics")]
    public bool keepKinematicAlways = true; // stay kinematic so physics never push
    public bool freezeRotation = true;      // freeze rotation on the rigidbody

    [Header("Diagnostics")]
    public bool logDebug = false;

    // -------- internals --------
    private Rigidbody rb;
    private SortableItem3D sortable;
    private Quaternion originalRotation;

    private bool dragging;
    private Vector3 grabOffset;
    private Plane dragPlane;      // plane used for projection
    private float worldZ;         // used only for DepthMode.WorldZ

    // Burger-only
    private bool isOverDropZone;
    private Transform currentDropTarget;

    // Back-compat for other scripts
    [SerializeField] private int ingredientValueBacking;
    public int ingredientValue
    {
        get => sortable ? sortable.value : ingredientValueBacking;
        set
        {
            ingredientValueBacking = value;
            if (sortable) sortable.value = value;
        }
    }

    public bool IsInStack { get; private set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sortable = GetComponent<SortableItem3D>();
        originalRotation = transform.rotation;

        if (rb)
        {
            if (keepKinematicAlways) rb.isKinematic = true;
            if (freezeRotation) rb.constraints |= RigidbodyConstraints.FreezeRotation;
            // Do NOT freeze Position Z — that constrains motion to a line on tilted boards.
        }
    }

    void Start()
    {
        if (mode == DragMode.BurgerBuild && !stackManager)
            stackManager = FindFirstObjectByType<BurgerStackManager>();
    }

    // -------- Input helpers (New Input System or legacy) --------
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

    // Always resolves the latest camera (no caching)
    private Camera Cam()
    {
        if (dragCamera) return dragCamera;
        return Camera.main;
    }

    // -------- Drag lifecycle --------
    void OnMouseDown()
    {
        var cam = Cam();
        if (!cam)
        {
            Debug.LogError("DragHandler3D: No camera assigned and no MainCamera tagged. Assign Drag Camera.", this);
            return;
        }

        // Stop any tweens/movements fighting the drag
        StopAllCoroutines();

        Ray ray = cam.ScreenPointToRay(PointerPos());
        if (!Physics.Raycast(ray, out var hit, 1000f, ~0, QueryTriggerInteraction.Collide)) return;
        if (hit.collider.gameObject != gameObject) return;

        // Build plane
        if (depthMode == DepthMode.SurfacePlane)
        {
            Vector3 n;
            if (!dragSurface)
            {
                n = -cam.transform.forward; // fallback
                if (logDebug) Debug.Log("[Drag] No dragSurface set; using CameraPlane fallback.", this);
            }
            else
            {
                n = dragSurface.TransformDirection(surfaceNormalLocal).normalized;
            }
            dragPlane = new Plane(n, hit.point);
        }
        else if (depthMode == DepthMode.CameraPlane)
        {
            dragPlane = new Plane(-cam.transform.forward, hit.point);
        }
        else // WorldZ
        {
            worldZ = transform.position.z;
            dragPlane = new Plane(Vector3.forward, new Vector3(0, 0, worldZ));
        }

        grabOffset = transform.position - hit.point;
        dragging = true;

        if (rb && !keepKinematicAlways)
        {
            rb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity  = Vector3.zero;
#else
            rb.velocity        = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        if (logDebug)
        {
            Vector3 planeN =
                depthMode == DepthMode.SurfacePlane && dragSurface
                ? dragSurface.TransformDirection(surfaceNormalLocal).normalized
                : (depthMode == DepthMode.CameraPlane ? -cam.transform.forward : Vector3.forward);

            Debug.Log($"[Drag START] cam={cam.name} mode={mode} depth={depthMode} planeN={planeN} hit={hit.point}", this);
        }
    }

    void OnMouseUp()
    {
        if (rb && !keepKinematicAlways) rb.isKinematic = true; // remain kinematic by default

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

            if (logDebug) Debug.Log($"[Drag MOVE] to {p}", this);
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

        if (spawnPoint)
            StartCoroutine(AnimateReturn(spawnPoint.position, originalRotation));

        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.enabled = true;
    }

    IEnumerator AnimateReturn(Vector3 targetPos, Quaternion targetRot)
    {
        float duration = 0.3f, elapsed = 0f;
        Vector3 start = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 pos = Vector3.Lerp(start, targetPos, t);
            if (rb) rb.MovePosition(pos);
            else    transform.position = pos;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        if (rb) rb.MovePosition(targetPos);
        else    transform.position = targetPos;
        transform.rotation = targetRot;
    }

    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
        originalRotation = transform.rotation;
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
