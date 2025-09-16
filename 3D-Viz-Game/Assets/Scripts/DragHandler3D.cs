using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SortableItem3D))]
public class DragHandler3D : MonoBehaviour
{
    public enum DragMode { BurgerBuild, Sorting }
    public DragMode mode = DragMode.BurgerBuild;

    private Camera mainCam;
    private Vector3 offset;
    private bool dragging = false;
    private float fixedAxis;

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BurgerStackManager stackManager; // used only in BurgerBuild

    private Quaternion originalRotation;

    // BurgerBuild only
    private bool isOverDropZone = false;
    private Transform currentDropTarget;

    public bool IsInStack { get; private set; } = false;
    public int ingredientValue;

    void Start()
    {
        mainCam = Camera.main;
        originalRotation = transform.rotation;

        if (mode == DragMode.BurgerBuild)
        {
            fixedAxis = transform.position.z;
            if (stackManager == null)
                stackManager = FindAnyObjectByType<BurgerStackManager>();
        }
        else // Sorting
        {
            fixedAxis = (spawnPoint != null) ? spawnPoint.position.z : 1.68f;
        }
    }

    void OnMouseDown()
    {
        dragging = true;
        offset = transform.position - GetMouseWorldPosition();
    }

    void OnMouseUp()
    {
        dragging = false;

        if (mode == DragMode.Sorting)
        {
            var mgr = ConveyorManager3D.Instance;
            var item = GetComponent<SortableItem3D>();
            bool inZone = (mgr && mgr.IsInConveyorZone(transform.position));
            Debug.Log($"[DragHandler3D] Sorting release | mgr={(mgr? "OK":"NULL")} item={(item? "OK":"MISSING")} inZone={inZone} pos={transform.position}");

            if (inZone && item != null)
            {
                bool placed = mgr.TryPlace(item);
                if (!placed) ResetToSpawnPoint();
            }
            else
            {
                ResetToSpawnPoint();
            }
            return;
        }

        // --- BurgerBuild path (unchanged) ---
        if (isOverDropZone && stackManager != null)
        {
            stackManager.StackItem(gameObject);
        }
        else
        {
            ResetToSpawnPoint();
        }
    }

    void Update()
    {
        if (!dragging) return;

        Vector3 newPos = GetMouseWorldPosition() + offset;

        if (mode == DragMode.BurgerBuild)
        {
            newPos.x = Mathf.Clamp(newPos.x, -3.3f, 3.3f);
            newPos.y = Mathf.Clamp(newPos.y, 0.5f, 2.5f);
            newPos.z = fixedAxis;
        }
        else // Sorting
        {
            newPos.x = Mathf.Clamp(newPos.x, -3f, 3f);
            newPos.y = Mathf.Clamp(newPos.y, -1.0f, 2.8f);
            newPos.z = fixedAxis;
        }

        transform.position = newPos;
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.forward, new Vector3(0, 0, fixedAxis));
        return dragPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : transform.position;
    }

    public void ResetToSpawnPoint()
    {
        if (mode == DragMode.BurgerBuild)
        {
            stackManager?.RemoveFromStack(gameObject);
            MarkInStack(false);
        }

        if (spawnPoint != null)
            StartCoroutine(AnimateReturn(spawnPoint.position, originalRotation));

        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.enabled = true;
    }

    private IEnumerator AnimateReturn(Vector3 targetPos, Quaternion targetRot)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 start = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(start, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
        if (mode == DragMode.Sorting)
        {
            fixedAxis = point.position.z;
            originalRotation = transform.rotation;
        }
    }

    public void MarkInStack(bool value)
    {
        IsInStack = value;
        gameObject.tag = value ? "InStack" : "InSpawn";
    }

    // ---- Trigger hooks now apply to BurgerBuild only ----
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
