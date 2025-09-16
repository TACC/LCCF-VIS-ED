using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class ConveyorManager3D : MonoBehaviour
{
    public static ConveyorManager3D Instance { get; private set; }

    public enum SortOrder { AscendingRightToLeft, DescendingRightToLeft }

    // ----- Slots / Zone / Exit -----
    [Header("Slots (ordered RIGHT → LEFT; index 0 = rightmost/lowest)")]
    public List<Transform> slots = new List<Transform>();

    [Header("Zone & Exit")]
    [Tooltip("Trigger collider representing the conveyor drop zone.")]
    public Collider conveyorZone;
    [Tooltip("Where items slide off to the right after checking.")]
    public Transform offscreenRight;

    [Header("Timings")]
    public float snapDuration = 0.25f;
    public float offscreenDuration = 0.5f;
    public float offscreenStagger = 0.05f;

    [Header("Rules")]
    public SortOrder order = SortOrder.AscendingRightToLeft;

    // ----- Feedback (per-slot) -----
    [Header("Feedback Materials (per-slot)")]
    [Tooltip("One renderer per slot (same order as 'slots').")]
    public Renderer[] feedbackCubes;
    [Tooltip("Material for a GREEN 'correct' slot.")]
    public Material glowGreenMat;
    [Tooltip("Material for a RED 'wrong' slot.")]
    public Material glowRedMat;
    [Tooltip("Material to restore to after feedback. If empty, original materials are restored.")]
    public Material defaultMat;
    public float feedbackTime = 0.75f;

    // ----- Round flow (no scene reload) -----
    [Header("Round Flow")]
    [Tooltip("Spawner used to create the next round of items.")]
    public SortingIngredientSpawner spawner;
    [Tooltip("Automatically call SpawnRound() after each round finishes.")]
    public bool autoSpawnNextRound = true;
    public float nextRoundDelay = 0.35f;
    [Tooltip("Optional UnityEvent fired when a round finishes grading & cleanup.")]
    public UnityEvent onRoundCompleted;

    // ----- Debug -----
    [Header("Debug")]
    public bool debugLogs = true;

    // ----- Runtime -----
    private SortableItem3D[] occupied;          // length = slots.Count
    private Material[][] _originalMats;         // cached original mats per feedback cube

    // ============================
    // Lifecycle
    // ============================
    void Awake()
    {
        Instance = this;

        if (slots == null) slots = new List<Transform>();
        occupied = new SortableItem3D[slots.Count];

        CacheOriginalFeedbackMats();

        if (conveyorZone == null && debugLogs) Debug.LogWarning("[ConveyorManager3D] conveyorZone not assigned.", this);
        if (slots.Count == 0 && debugLogs) Debug.LogWarning("[ConveyorManager3D] No slots assigned.", this);
        if (feedbackCubes != null && feedbackCubes.Length > 0 && feedbackCubes.Length != slots.Count)
            Debug.LogWarning("[ConveyorManager3D] feedbackCubes count should match slots count.", this);
    }

    void OnDisable() { if (Instance == this) Instance = null; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void CacheOriginalFeedbackMats()
    {
        if (feedbackCubes == null || feedbackCubes.Length == 0) return;

        _originalMats = new Material[feedbackCubes.Length][];
        for (int i = 0; i < feedbackCubes.Length; i++)
        {
            var r = feedbackCubes[i];
            _originalMats[i] = r ? (Material[])r.sharedMaterials.Clone() : null;
        }
    }

    // ============================
    // Zone & placement
    // ============================
    public bool IsInConveyorZone(Vector3 point)
    {
        if (!conveyorZone)
        {
            if (debugLogs) Debug.LogWarning("[ConveyorManager3D] conveyorZone not assigned.", this);
            return false;
        }

        Vector3 closest = conveyorZone.ClosestPoint(point);
        bool inside = (closest - point).sqrMagnitude < 0.0001f;
        if (debugLogs) Debug.Log($"[ConveyorManager3D] InsideZone={inside}, point={point}, closest={closest}", this);
        return inside;
    }

    int FirstFreeIndex()
    {
        for (int i = 0; i < occupied.Length; i++)
            if (occupied[i] == null) return i;
        return -1;
    }

    public bool TryPlace(SortableItem3D item)
    {
        if (item == null)
        {
            if (debugLogs) Debug.LogWarning("[ConveyorManager3D] Item is null.");
            return false;
        }
        if (slots == null || slots.Count == 0)
        {
            if (debugLogs) Debug.LogWarning("[ConveyorManager3D] No slots set.");
            return false;
        }

        // already placed and registered
        if (item.slotIndex >= 0 && item.slotIndex < occupied.Length && occupied[item.slotIndex] == item)
            return true;

        int idx = FirstFreeIndex();
        if (idx < 0)
        {
            if (debugLogs) Debug.Log("[ConveyorManager3D] No free slot.");
            return false;
        }

        // vacate previous slot if any
        if (item.slotIndex != -1 && item.slotIndex < occupied.Length && occupied[item.slotIndex] == item)
            occupied[item.slotIndex] = null;

        occupied[idx] = item;
        item.slotIndex = idx;

        // snap into slot
        item.moveDuration = snapDuration;
        item.MoveTo(slots[idx]);

        // lock physics while in slot
        var rb = item.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }

        if (debugLogs) Debug.Log($"[ConveyorManager3D] Placed value={item.value} into slot={idx}");

        if (AllFilled()) StartCoroutine(CheckThenClearCo());

        return true;
    }

    public bool AllFilled()
    {
        for (int i = 0; i < occupied.Length; i++)
            if (occupied[i] == null) return false;
        return true;
    }

    // ============================
    // Checking logic
    // ============================
    bool IsCorrectOrder()
    {
        if (occupied == null || occupied.Length == 0) return true;

        if (order == SortOrder.AscendingRightToLeft)
        {
            int prev = int.MinValue;
            for (int i = 0; i < occupied.Length; i++)
            {
                int v = occupied[i].value;
                if (v <= prev) return false; // strictly increasing across right->left
                prev = v;
            }
            return true;
        }
        else // DescendingRightToLeft
        {
            int prev = int.MaxValue;
            for (int i = 0; i < occupied.Length; i++)
            {
                int v = occupied[i].value;
                if (v >= prev) return false; // strictly decreasing across right->left
                prev = v;
            }
            return true;
        }
    }

    IEnumerator CheckThenClearCo()
    {
        yield return new WaitForSeconds(0.15f); // settle

        bool overallOk = IsCorrectOrder();
        if (debugLogs) Debug.Log(overallOk ? "✅ Correct order!" : "❌ Wrong order!");

        // Per-slot feedback (green = correct, red = wrong)
        yield return StartCoroutine(ShowPerSlotFeedback());

        // Slide items off and delete
        yield return StartCoroutine(SendAllOffscreenRight());

        ClearState();

        // --- DEBUG: Round ended ---
        if (debugLogs) Debug.Log("[ConveyorManager3D] Round ended: feedback shown, items cleared, state reset.");

        // Signal end-of-round
        onRoundCompleted?.Invoke();

        // Spawn the next round in-scene
        if (autoSpawnNextRound && spawner != null)
        {
            if (debugLogs) Debug.Log($"[ConveyorManager3D] Calling spawner.SpawnRound() in {nextRoundDelay:0.00}s ...");
            StartCoroutine(SpawnNextRoundAfterDelay());
        }
        else if (debugLogs)
        {
            Debug.Log("[ConveyorManager3D] autoSpawnNextRound is OFF or spawner is not assigned.");
        }
    }

    IEnumerator ShowPerSlotFeedback()
    {
        if (feedbackCubes == null || feedbackCubes.Length == 0 || glowGreenMat == null || glowRedMat == null)
            yield break;

        if (_originalMats == null || _originalMats.Length != feedbackCubes.Length)
            CacheOriginalFeedbackMats();

        // Build expected values array
        var current = new List<int>(occupied.Length);
        for (int i = 0; i < occupied.Length; i++)
            current.Add(occupied[i] ? occupied[i].value : int.MinValue);

        var sorted = new List<int>(current);
        if (order == SortOrder.AscendingRightToLeft) sorted.Sort();
        else sorted.Sort((a, b) => b.CompareTo(a));

        int n = Mathf.Min(feedbackCubes.Length, current.Count);

        for (int i = 0; i < n; i++)
        {
            bool hasItem = occupied[i] != null;
            bool correct = hasItem && (current[i] == sorted[i]);
            SetAllSubmeshMaterials(feedbackCubes[i], correct ? glowGreenMat : glowRedMat);
        }

        yield return new WaitForSeconds(feedbackTime);

        // Restore after glow
        for (int i = 0; i < n; i++)
            RestoreMaterials(i);
    }

    // ============================
    // Cleanup & next round helpers
    // ============================
    IEnumerator SendAllOffscreenRight()
    {
        // snapshot list to avoid mutation issues
        var list = new List<SortableItem3D>();
        for (int i = 0; i < occupied.Length; i++)
            if (occupied[i] != null) list.Add(occupied[i]);

        // slide off to the right (stay kinematic)
        for (int i = 0; i < list.Count; i++)
        {
            var it = list[i];
            if (!it) continue;

            it.moveDuration = offscreenDuration;
            Vector3 dest = offscreenRight
                ? offscreenRight.position
                : (it.transform.position + Vector3.right * 10f);

            it.MoveTo(dest);
            yield return new WaitForSeconds(offscreenStagger);
        }

        // wait for last slide to finish
        yield return new WaitForSeconds(offscreenDuration + 0.05f);

        // delete items
        foreach (var it in list)
            if (it) Destroy(it.gameObject);
    }

    void ClearState()
    {
        for (int i = 0; i < occupied.Length; i++)
            occupied[i] = null;
    }

    IEnumerator SpawnNextRoundAfterDelay()
    {
        yield return new WaitForSeconds(nextRoundDelay);
        spawner.SpawnRound();
        if (debugLogs) Debug.Log("[ConveyorManager3D] SpawnRound() called.");
    }

    // ============================
    // Material helpers
    // ============================
    void SetAllSubmeshMaterials(Renderer r, Material mat)
    {
        if (!r || mat == null) return;
        var count = r.sharedMaterials.Length;
        var arr = new Material[count];
        for (int i = 0; i < count; i++) arr[i] = mat;
        r.sharedMaterials = arr; // runtime swap (affects this renderer only)
    }

    void RestoreMaterials(int index)
    {
        if (feedbackCubes == null) return;
        if (index < 0 || index >= feedbackCubes.Length) return;

        var r = feedbackCubes[index];

        if (defaultMat != null)
        {
            SetAllSubmeshMaterials(r, defaultMat);
            return;
        }

        if (_originalMats == null || index >= _originalMats.Length) return;
        var originals = _originalMats[index];
        if (r && originals != null) r.sharedMaterials = originals;
    }
}
