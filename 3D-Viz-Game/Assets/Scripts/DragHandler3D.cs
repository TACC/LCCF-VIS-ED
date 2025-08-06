using UnityEngine;
using System.Collections;
using TMPro;

public class DragHandler3D : MonoBehaviour
{
    private Camera mainCam;
    private Vector3 offset;
    private bool dragging = false;
    private float fixedZ;

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BurgerStackManager stackManager;

    private bool isOverDropZone = false;

    public bool IsInStack { get; private set; } = false;

    public void MarkInStack(bool value)
    {
        IsInStack = value;
        gameObject.tag = value ? "InStack" : "InSpawn";
    }

    void Start()
    {
        mainCam = Camera.main;
        fixedZ = transform.position.z;

        if (spawnPoint == null)
        {
            spawnPoint = new GameObject(name + "_Spawn").transform;
            spawnPoint.position = transform.position;
        }

        if (stackManager == null)
        {
            stackManager = FindAnyObjectByType<BurgerStackManager>();
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

        if (!isOverDropZone)
        {
            ResetToSpawnPoint();
        }
        else
        {
            stackManager?.StackItem(gameObject);
        }
    }

    void Update()
    {
        if (dragging)
        {
            Vector3 newPos = GetMouseWorldPosition() + offset;
            newPos.x = Mathf.Clamp(newPos.x, -3f, 3f);
            newPos.y = Mathf.Clamp(newPos.y, 0.5f, 2.5f);
            newPos.z = fixedZ;
            transform.position = newPos;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(mainCam.transform.position.z - fixedZ);
        return mainCam.ScreenToWorldPoint(mouse);
    }

    public void ResetToSpawnPoint()
{
    stackManager?.RemoveFromStack(gameObject);
    StartCoroutine(AnimateReturn(spawnPoint.position, spawnPoint.rotation));
    MarkInStack(false);

    // ✅ Show number again
    TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();
    if (label != null)
    {
        label.enabled = true;
    }
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
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DropZone"))
        {
            isOverDropZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DropZone"))
        {
            isOverDropZone = false;
        }
    }
}
