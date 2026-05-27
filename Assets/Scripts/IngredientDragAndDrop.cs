using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IngredientDragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Transform originalParent;
    private bool placed = false;

    [HideInInspector] public Vector2 originalSpawnPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed) return;

        originalSpawnPos = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling(); // Bring to front
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (placed) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (placed) return;

        canvasGroup.blocksRaycasts = true;

        if (RectTransformUtility.RectangleContainsScreenPoint(
            BuildManager.Instance.dropZone,
            eventData.position,
            canvas.worldCamera))
        {
            BuildManager.Instance.IngredientChosen(gameObject);
            placed = true;
        }
        else
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalSpawnPos;
        }
    }

    public void ResetPlacement()
    {
        placed = false;
        canvasGroup.blocksRaycasts = true;
    }
}
