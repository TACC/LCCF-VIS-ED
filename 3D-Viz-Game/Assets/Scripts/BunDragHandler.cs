using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BunDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rt;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private BunData bunData;

    public Image bunImage;
    public Sprite normalSprite;
    public Sprite inBinSprite;

    public TextMeshProUGUI sideText;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        bunData = GetComponent<BunData>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Fix drag offset using screen-to-local conversion
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPoint
        );
        rt.anchoredPosition = localPoint;

        if (IsOverDropZone(eventData))
            bunImage.sprite = inBinSprite;
        else
            bunImage.sprite = normalSprite;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (IsOverDropZone(eventData))
        {
            RectTransform slot = DropZoneManager.Instance.GetAvailableSlot();
            if (slot != null)
            {
                StartCoroutine(SnapToSlot(slot));
                bunImage.sprite = inBinSprite;

                // Show side label
                if (bunData.sideTextObject != null && sideText != null)
                {
                    sideText.text = bunData.assignedNumber.ToString();
                    bunData.sideTextObject.SetActive(true);
                }

                // Hide top label
                if (bunData.topTextObject != null)
                {
                    bunData.topTextObject.SetActive(false);
                }

                DropZoneManager.Instance.RegisterDroppedNumber(bunData.assignedNumber);

                gameObject.tag = "DroppedBun"; // You'll create this tag in Unity if needed

                canvasGroup.blocksRaycasts = false;
                this.enabled = false;
                return;
            }

            

        }

        // If not dropped in bin: snap back
        rt.anchoredPosition = bunData.originalSlot.anchoredPosition;
        bunImage.sprite = normalSprite;
    }

    private IEnumerator SnapToSlot(RectTransform slot)
    {
        Vector2 start = rt.anchoredPosition;
        Vector2 end = slot.anchoredPosition;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rt.anchoredPosition = Vector2.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = end;
    }

    private bool IsOverDropZone(PointerEventData eventData)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            DropZoneManager.Instance.dropZone,
            eventData.position,
            canvas.worldCamera
        );
    }
}
