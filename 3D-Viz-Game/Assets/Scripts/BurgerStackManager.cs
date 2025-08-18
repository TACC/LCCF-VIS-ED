using UnityEngine;
using System.Collections;
using TMPro;

public class BurgerStackManager : MonoBehaviour
{
    [Header("Stack Positions (Ordered Bottom to Top)")]
    public Transform[] stackPositions;

    [SerializeField] private BurgerIngredientSpawner spawner;

    private GameObject[] stackedItems;

    void Awake()
    {
        stackedItems = new GameObject[stackPositions.Length];
    }

    void Start()
    {
        EnsureSessionAndMode();
        // When your spawner starts a new burger, call BeginBurgerRound() below.
    }

    private void EnsureSessionAndMode()
    {
        if (GameSession.Instance == null) return;
        if (!GameSession.Instance.SessionRunning)
            GameSession.Instance.StartSession(150);
        GameSession.Instance.SetMode(GameMode.Burger);
    }

    // 🔹 Call this when a NEW burger round begins (after spawning the bottom bun/options)
    public void BeginBurgerRound()
    {
        EnsureSessionAndMode();
        if (GameSession.Instance != null) GameSession.Instance.StartTask(); // hidden 15s timer
    }

    // 🔹 Call this from your Submit button/controller with your computed result
    public void OnSubmitBurger(bool isCorrect)
    {
        if (GameSession.Instance != null)
            GameSession.Instance.CompleteTask(isCorrect);   // +100/−10, +5 if ≤15s

        // Your existing "correct → slide off" or "wrong → explode/fall" flows continue as-is.
    }

    public void StackItem(GameObject newItem)
    {
        string newType = newItem.name.Replace("(Clone)", "").Trim();

        // Replace same-type item if found
        for (int i = 0; i < stackedItems.Length; i++)
        {
            GameObject existing = stackedItems[i];
            if (existing != null && existing.name.Replace("(Clone)", "").Trim() == newType)
            {
                var drag = existing.GetComponent<DragHandler3D>();
                if (drag != null)
                {
                    drag.ResetToSpawnPoint();
                    drag.enabled = true;
                }

                stackedItems[i] = newItem;

                // 🔻 Hide number label
                TextMeshProUGUI label = newItem.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.enabled = false;

                MoveAndMark(newItem, stackPositions[i]);
                return;
            }
        }

        // Stack in next available empty slot
        for (int i = 0; i < stackedItems.Length; i++)
        {
            if (stackedItems[i] == null)
            {
                stackedItems[i] = newItem;

                // 🔻 Hide number label
                TextMeshProUGUI label = newItem.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.enabled = false;

                MoveAndMark(newItem, stackPositions[i]);
                return;
            }
        }

        Debug.LogWarning("No empty stack slots available.");
    }

    private void MoveAndMark(GameObject item, Transform point)
    {
        var drag = item.GetComponent<DragHandler3D>();
        if (drag != null)
        {
            drag.enabled = false;
            drag.MarkInStack(true);
        }

        StartCoroutine(MoveToTarget(item.transform, point.position, point.rotation));
        spawner?.NotifyIngredientPlaced(item);
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

    public void RemoveFromStack(GameObject item)
    {
        for (int i = 0; i < stackedItems.Length; i++)
        {
            if (stackedItems[i] == item)
            {
                stackedItems[i] = null;
                break;
            }
        }
    }

    public GameObject[] GetStackedItems()
    {
        return stackedItems;
    }
}
