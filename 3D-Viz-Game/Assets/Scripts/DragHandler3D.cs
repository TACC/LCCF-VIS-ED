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
    [SerializeField] private Camera dragCamera;                 // resolved via tag or injected
    [SerializeField] private string dragCameraTag = "dragCamera";
    [SerializeField] private Transform dragSurface;             // for SurfacePlane mode
    [SerializeField] private Vector3 surfaceNormalLocal = Vector3.up;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BurgerStackManager stackManager;

    [Header("Clamps (optional, world X/Y)")]
    public bool useClamps = true;
    public Vector2 sortingClampX = new Vector2(-3f, 3f);
    public Vector2 sortingClampY = new Vector2(-1f, 2.8f);
    public Vector2 burgerClampX  = new Vector2(-3.3f, 3.3f);
    public Vector2 burgerClampY  = new Vector2(0.5f, 2.5f);

    [Header("Physics")]
    public bool keepKinematicAlways = true;     // keeps RB kinematic while dragging/returning
    public bool freezeRotation = true;          // adds FreezeRotation to RB constraints if present

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

    // Optional value mirror
    [SerializeField] private int ingredientValueBacking;
    public int ingredientValue
    {
        get => sortable ? sortable.value : ingredientValueBacking;
        set { ingredientValueBacking = value; if (sortable) sortable.value = value; }
    }

    public bool IsInStack { get; private set; } = false;

    // ---------------- lifecycle ----------------
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sortable = GetComponent<SortableItem3D>();
        originalRotation = transform.rotation;
        initialLocalScale = transform.localScale;

        // Resolve camera: explicit > tagged > MainCamera
        if (!dragCamera) dragCamera = FindTaggedCamera();
        if (!dragCamera) dragCamera = Camera.main;

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

    // ---------------- camera helpers ----------------
    public void SetDragCamera(Camera cam) => dragCamera = cam;

    private Camera FindTaggedCamera()
    {
        if (string.IsNullOrEmpty(dragCameraTag)) return null;
        var tagged = GameObject.FindGameObjectsWithTag(dragCameraTag);
        // prefer active+enabled cameras
        foreach (var go in tagged)
        {
            var cam = go.GetComponent<Camera>();
            if (cam != null && cam.enabled && go.activeInHierarchy) return cam;
        }
        // fallback: any camera on tagged objects
        foreach (var go in tagged)
        {
            var cam = go.GetComponent<Camera>();
            if (cam != null) return cam;
        }
        return null;
    }

    private Camera Cam()
    {
        if (dragCamera && dragCamera.enabled && dragCamera.gameObject.activeInHierarchy) return dragCamera;
        var tagged = FindTaggedCamera();
        if (tagged) { dragCamera = tagged; return dragCamera; }
        return Camera.main;
    }

    // ---------------- input helpers ----------------
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

    // ---------------- mouse (explicit raycasts) ----------------
    void OnMouseDown()
    {
        var cam = Cam();
        if (!cam)
        {
            Debug.LogError("DragHandler3D: No camera found (tagged, assigned, or MainCamera).", this);
            return;
        }

        StopAllCoroutines();

        Ray ray = cam.ScreenPointToRay(PointerPos());
        if (!Physics.Raycast(ray, out var hit, 1000f, ~0, QueryTriggerInteraction.Collide)) return;
        if (hit.collider.gameObject != gameObject) return;

        // Build drag plane at hit depth/orientation
        switch (depthMode)
        {
            case DepthMode.SurfacePlane:
            {
                Vector3 n = dragSurface
                    ? dragSurface.TransformDirection(surfaceNormalLocal).normalized
                    : -cam.transform.forward; // screen-aligned plane through hit
                dragPlane = new Plane(n, hit.point);
                break;
            }
            case DepthMode.CameraPlane:
                dragPlane = new Plane(-cam.transform.forward, hit.point);
                break;
            case DepthMode.WorldZ:
                worldZ = transform.position.z;
                dragPlane = new Plane(Vector3.forward, new Vector3(0, 0, worldZ));
                break;
        }

        grabOffset = transform.position - hit.point;
        dragging = true;

        // quiet physics
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

    // ---------------- utilities ----------------
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

    private IEnumerator ReturnToSpawnRoutine()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 endPos = spawnPoint.position;
        Quaternion endRot = (mode == DragMode.Sorting) ? startRot : spawnPoint.rotation;

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

        var cols = GetComponentsInChildren<Collider>(includeInactive: true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        float dur = 0.25f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = t / dur;
            transform.position = Vector3.Lerp(startPos, endPos, a);
            if (mode != DragMode.Sorting)
                transform.rotation = Quaternion.Slerp(startRot, endRot, a);
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        transform.SetParent(spawnPoint, worldPositionStays: true);
        transform.localScale = initialLocalScale;
        transform.localPosition = Vector3.zero;
        if (mode == DragMode.BurgerBuild) transform.localRotation = Quaternion.identity;

        yield return null;
        for (int i = 0; i < cols.Length; i++) if (cols[i]) cols[i].enabled = true;

        if (logDebug) Debug.Log($"[RETURN] {name} -> {spawnPoint.name} (mode={mode})", this);
    }

    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
        originalRotation  = transform.rotation;
        initialLocalScale = transform.localScale;
    }

    public void SetDragSurface(Transform surface, Vector3 localNormalUp)
    {
        dragSurface = surface;
        surfaceNormalLocal = localNormalUp;
    }

    public void MarkInStack(bool value)
    {
        IsInStack = value;
        gameObject.tag = value ? "InStack" : "InSpawn";
    }

    // ---------------- burger triggers ----------------
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
