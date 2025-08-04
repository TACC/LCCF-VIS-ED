using UnityEngine;
using UnityEngine.EventSystems;

public class IngredientDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 originalPosition;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rt;

    private bool placed = false;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed) return;

        originalPosition = rt.position;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (placed) return;

        Plane dragPlane = new Plane(Vector3.forward, transform.position); // adjust normal for camera if needed

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float distance))
        {
            transform.position = ray.GetPoint(distance);
        }
    }



    public void OnEndDrag(PointerEventData eventData)
    {
        if (placed) return;

        // Check for drop zone
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            if (hit.collider.CompareTag("DropZone"))
            {
                placed = true;
                BuildManager bm = Object.FindFirstObjectByType<BuildManager>();
                bm.IngredientChosen(gameObject);
                return;
            }
        }

        // Return to original position if not placed
        rt.position = originalPosition;
        canvasGroup.blocksRaycasts = true;
    }
}
