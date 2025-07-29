using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;


public class DropZoneManager : MonoBehaviour
{
    public static DropZoneManager Instance;

    public RectTransform dropZone;         // Green drop zone area
    public RectTransform[] binSlots;       // Assign 10 bin slot RectTransforms in order
    private bool[] slotOccupied;           // Tracks which bin slots are taken

    public List<int> droppedNumbers = new List<int>();
    public int expectedCount = 7; // Will change with difficulty
    public System.Action<bool, float> onStackComplete; // Callback to GameManager
    private float dropStartTime;
    public Image dropZoneImage;
    public Sprite normalSprite;
    public Sprite errorSprite;


    private void Awake()
    {
        Instance = this;
        slotOccupied = new bool[binSlots.Length];
    }

    public RectTransform GetAvailableSlot()
    {
        for (int i = 0; i < binSlots.Length; i++)
        {
            if (!slotOccupied[i])
            {
                slotOccupied[i] = true;
                return binSlots[i];
            }
        }
        return null; // All full
    }

    public void StartDropPhase(int count)
    {
        droppedNumbers.Clear();
        expectedCount = count;
        dropStartTime = Time.time;

        for (int i = 0; i < slotOccupied.Length; i++)
            slotOccupied[i] = false;
    }

    public void RegisterDroppedNumber(int number)
    {
        droppedNumbers.Add(number);

        if (droppedNumbers.Count == expectedCount)
        {
            bool isCorrect = IsSortedAscending(droppedNumbers);
            float elapsed = Time.time - dropStartTime;

            onStackComplete?.Invoke(isCorrect, elapsed);
        }
    }

    private bool IsSortedAscending(List<int> list)
    {
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i] < list[i - 1])
                return false;
        }
        return true;
    }

    public void ClearBin()
    {
        GameObject[] buns = GameObject.FindGameObjectsWithTag("DroppedBun");
        foreach (GameObject bun in buns)
        {
            StartCoroutine(MoveAndDestroy(bun));
        }
    }

    private IEnumerator MoveAndDestroy(GameObject bun)
    {
        RectTransform rt = bun.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + Vector2.down * 500f; // You can adjust how far it falls

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(bun);
    }

    public void FlashErrorZone()
    {
        StartCoroutine(FlashDropZone());
    }


    private IEnumerator FlashDropZone()
    {
        if (dropZoneImage != null && errorSprite != null)
            dropZoneImage.sprite = errorSprite;

        yield return new WaitForSeconds(3f);

        if (dropZoneImage != null && normalSprite != null)
            dropZoneImage.sprite = normalSprite;

    }


}
