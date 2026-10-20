using UnityEngine;
using System.Collections;
using TMPro;

public class BurgerStackManager : MonoBehaviour
{
    [Header("Stack Positions (Ordered Bottom to Top)")]
    public Transform[] stackPositions;

    [Header("Final Compact Burger Positions")]
    [SerializeField] private Transform[] finalBurgerPositions;

    [SerializeField] private BurgerIngredientSpawner spawner;
    private GameObject[] stackedItems;

    void Awake()
    {
        EnsureStackArray();
    }

    public void StackItem(GameObject newItem)
    {
        EnsureStackArray();
        string newType = CleanType(newItem.name);

        // FULL BUILD
        if (spawner != null &&
            spawner.BurgerPlayMode == BurgerPlayMode.FullBuild)
        {
            // Find the current top of the stack.
            int topIndex = -1;

            for (int i = stackedItems.Length - 1; i >= 0; i--)
            {
                if (stackedItems[i] != null)
                {
                    topIndex = i;
                    break;
                }
            }

            // If the new ingredient matches the TOP ingredient,
            // replace only that top ingredient.
            if (topIndex >= 0)
            {
                GameObject topItem = stackedItems[topIndex];

                if (CleanType(topItem.name) == newType)
                {
                    EvictToSpawn(topItem);

                    stackedItems[topIndex] = newItem;

                    TextMeshProUGUI label =
                        newItem.GetComponentInChildren<TextMeshProUGUI>();

                    if (label != null)
                        label.enabled = false;

                    MoveAndMark(newItem, topIndex);
                    return;
                }
            }

            // If this ingredient type already exists LOWER in the stack,
            // don't allow another copy to be pushed.
            for (int i = 0; i < stackedItems.Length; i++)
            {
                if (stackedItems[i] != null &&
                    CleanType(stackedItems[i].name) == newType)
                {

                    DragHandler3D drag =
                        newItem.GetComponent<DragHandler3D>();

                    if (drag != null)
                        drag.ResetToSpawnPoint();

                    return;
                }
            }

            int nextIndex = topIndex + 1;

            if (nextIndex < stackedItems.Length)
            {
                stackedItems[nextIndex] = newItem;

                TextMeshProUGUI label =
                    newItem.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                    label.enabled = false;

                MoveAndMark(newItem, nextIndex);
                return;
            }

            DragHandler3D fullDrag =
                newItem.GetComponent<DragHandler3D>();

            if (fullDrag != null)
                fullDrag.ResetToSpawnPoint();

            return;
        }

        for (int i = 0; i < stackedItems.Length; i++)
        {
            GameObject existing = stackedItems[i];

            if (existing != null &&
                CleanType(existing.name) == newType)
            {
                EvictToSpawn(existing);

                stackedItems[i] = newItem;

                TextMeshProUGUI label =
                    newItem.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                    label.enabled = false;

                MoveAndMark(newItem, i);
                return;
            }
        }

        // Guided: place in next empty slot.
        for (int i = 0; i < stackedItems.Length; i++)
        {
            if (stackedItems[i] == null)
            {
                stackedItems[i] = newItem;

                TextMeshProUGUI label =
                    newItem.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                    label.enabled = false;

                MoveAndMark(newItem, i);
                return;
            }
        }

    }

    private static string CleanType(string raw) => raw.Replace("(Clone)", "").Trim();

    private void MoveAndMark(GameObject item, int stackIndex)
    {
        var drag = item.GetComponent<DragHandler3D>();

        if (drag != null)
        {
            drag.MarkInStack(true);

            if (spawner != null &&
                spawner.BurgerPlayMode == BurgerPlayMode.FullBuild)
            {
                drag.enabled = true;
            }
            else
            {
                drag.enabled = false;
            }
        }

        Transform point = stackPositions[stackIndex];

        StartCoroutine(
            MoveToTarget(
                item.transform,
                point.position,
                point.rotation
            )
        );

        spawner?.NotifyIngredientPlaced();
        spawner?.RemoveFromActiveItems(item);
    }


    private IEnumerator MoveToTarget(Transform item, Vector3 pos, Quaternion rot)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        Vector3 start = item.position;
        Quaternion startRot = item.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            item.position = Vector3.Lerp(start, pos, t);
            item.rotation = Quaternion.Slerp(startRot, rot, t);
            yield return null;
        }

        item.position = pos;
        item.rotation = rot;
    }

    private void EvictToSpawn(GameObject item)
    {
        var drag = item.GetComponent<DragHandler3D>();

        if (drag != null)
        {
            drag.enabled = true;
            drag.ResetToSpawnPoint();
        }

        var label = item.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.enabled = true;
    }

    public void RemoveFromStack(GameObject item)
    {
        for (int i = 0; i < stackedItems.Length; i++)
        {
            if (stackedItems[i] == item)
            {
                stackedItems[i] = null;
                spawner?.ReAddToActiveItems(item);

                break;
            }
        }
    }

    public GameObject[] GetStackedItems()
    {
        return stackedItems;
    }

    public bool IsTopMostItem(GameObject item)
    {
        for (int i = stackedItems.Length - 1; i >= 0; i--)
        {
            if (stackedItems[i] != null)
                return stackedItems[i] == item;
        }

        return false;
    }


    private void EnsureStackArray()
    {
        int neededSize =
            stackPositions != null
            ? stackPositions.Length
            : 0;

        if (stackedItems == null ||
            stackedItems.Length != neededSize)
        {
            stackedItems = new GameObject[neededSize];
        }
    }

    public Transform[] GetFinalBurgerPositions()
    {
        return finalBurgerPositions;
    }
}
