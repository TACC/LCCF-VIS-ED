using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BunSlideManager : MonoBehaviour
{
    private List<int> usedNumbers = new List<int>();

    public GameObject bunPrefab;
    public RectTransform[] targetPositions;  // UI target positions
    public Transform canvasParent;           // Drag your Canvas here in the Inspector

    public void SpawnBuns(int count)
    {
        StartCoroutine(SlideInBuns(count));
    }

    IEnumerator SlideInBuns(int count)
    {
        usedNumbers.Clear(); // ✅ Prevent build-up across rounds

        for (int i = 0; i < count; i++)
        {
            GameObject bun = Instantiate(bunPrefab, canvasParent);
            RectTransform rt = bun.GetComponent<RectTransform>();

            // Generate and assign unique number
            int number = GetUniqueRandomNumber();
            BunData bunData = bun.GetComponent<BunData>();
            BunDragHandler dragHandler = bun.GetComponent<BunDragHandler>();
            bunData.assignedNumber = number;
            bunData.originalSlot = targetPositions[i];

            // Assign top and side TMP text references
            TextMeshProUGUI[] texts = bun.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tmp in texts)
            {
                if (tmp.gameObject.name.Contains("(1)")) // side text
                {
                    bunData.sideTextObject = tmp.gameObject;
                    dragHandler.sideText = tmp;
                    tmp.gameObject.SetActive(false);
                }
                else // top text
                {
                    tmp.text = number.ToString();
                    bunData.topTextObject = tmp.gameObject;
                }
            }

            // Slide in from the left
            float offscreenX = -Screen.width - 200f;
            Vector2 offscreenPos = new Vector2(offscreenX, targetPositions[i].anchoredPosition.y);
            Vector2 target = targetPositions[i].anchoredPosition;
            rt.anchoredPosition = offscreenPos;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                rt.anchoredPosition = Vector2.Lerp(offscreenPos, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            rt.anchoredPosition = target;
            yield return new WaitForSeconds(0.05f); // delay before next bun
        }
    }

    // ✅ Safe random number generator with retry cap
    int GetUniqueRandomNumber()
    {
        int num = -1;
        int attempts = 0;

        do
        {
            num = Random.Range(1, 61); // 1 to 60
            attempts++;

            if (attempts > 200)
            {
                Debug.LogWarning("Too many attempts to find unique bun number. Using fallback.");
                break;
            }
        }
        while (usedNumbers.Contains(num));

        usedNumbers.Add(num);
        return num;
    }
}
