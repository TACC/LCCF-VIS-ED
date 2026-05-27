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
    Debug.Log($"[BurgerStackManager] Submit pressed. isCorrect={isCorrect}");

    if (GameSession.Instance != null)
    {
        int before = GameSession.Instance.Score;
        GameSession.Instance.SetMode(GameMode.Burger);   // safety
        GameSession.Instance.CompleteTask(isCorrect);    // +100/−10 (+bonus if fast)
        Debug.Log($"[BurgerStackManager] Score {before} -> {GameSession.Instance.Score}");
    }
    else
    {
        Debug.LogWarning("[BurgerStackManager] No GameSession instance.");
    }
    // slide/explode handled elsewhere
}


    public void StackItem(GameObject newItem)
    {
        string newType = CleanType(newItem.name);

        // Replace same-type item if found
        for (int i = 0; i < stackedItems.Length; i++)
        {
            GameObject existing = stackedItems[i];
            if (existing != null && CleanType(existing.name) == newType)
            {
                // ⬅️ send old piece back to spawn AND re-add to active list
                EvictToSpawn(existing);

                // occupy this slot with the new item
                stackedItems[i] = newItem;

                // hide number label on the newly stacked item
                var labelNew = newItem.GetComponentInChildren<TextMeshProUGUI>();
                if (labelNew != null) labelNew.enabled = false;

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

                var labelNew = newItem.GetComponentInChildren<TextMeshProUGUI>();
                if (labelNew != null) labelNew.enabled = false;

                MoveAndMark(newItem, stackPositions[i]);
                return;
            }
        }

        Debug.LogWarning("No empty stack slots available.");
    }

    private static string CleanType(string raw) => raw.Replace("(Clone)", "").Trim();

    private void MoveAndMark(GameObject item, Transform point)
    {
        var drag = item.GetComponent<DragHandler3D>();
        if (drag != null)
        {
            drag.enabled = false;
            drag.MarkInStack(true);
        }

        StartCoroutine(MoveToTarget(item.transform, point.position, point.rotation));

        // tell the spawner this choice was used; it should NOT be cleared on Next
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

    // 🔧 unified eviction path that ensures the item is cleared AND tracked for cleanup
    private void EvictToSpawn(GameObject item)
    {
        var drag = item.GetComponent<DragHandler3D>();
        if (drag != null)
        {
            drag.enabled = true;
            drag.MarkInStack(false);      // back to spawn state (also sets tag)
            drag.ResetToSpawnPoint();     // handles reparent + snap/lerp back
        }

        // show its number again in spawn
        var label = item.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.enabled = true;

        // so ClearUnusedItems() can destroy it on Next
        spawner?.ReAddToActiveItems(item);
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
