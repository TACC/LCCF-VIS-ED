using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class SortingIngredientSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    public GameObject[] prefabs;
    public int itemCount = 8;

    [Header("Spawn & Arrive")]
    public Transform spawnFromPoint;            // single origin
    public Transform[] arrivePoints;            // table spots (>= itemCount)
    public Transform itemsParent;

    [Header("Integrations")]
    public ConveyorManager3D conveyor;          // optional cap to slots
    [Header("Station Camera")]
    [SerializeField] private Camera stationCamera;

    [Header("Entry Tween")]
    public float arriveDuration = 0.45f;
    public AnimationCurve arriveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float spawnStagger = 0f;

    [Header("Rotation")]
    public bool overrideRotation = true;
    public Vector3 rotationOffsetEuler = new Vector3(90f, 0f, 0f);

    [Header("Number Range (unique per round)")]
    public int minValue = 1;
    public int maxValue = 60;                   // <- random from 1..60
    public bool setDragModeToSorting = true;
    public bool clearExistingBeforeSpawn = true;
    public bool spawnOnStart = true;

    [Header("Debug")]
    public bool debugLogs = false;

    GameObject chosenPrefabThisRound;

    void Start()
    {
        if (spawnOnStart) StartCoroutine(SpawnRoundCo());
    }

    public void SpawnRound()
    {
        StartCoroutine(SpawnRoundCo());
    }

    [ContextMenu("Spawn Round (Editor)")]
    void SpawnRound_EditorMenu() => StartCoroutine(SpawnRoundCo());

    IEnumerator SpawnRoundCo()
    {
        if (clearExistingBeforeSpawn) ClearExisting();

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[SortingIngredientSpawner] No prefabs assigned.");
            yield break;
        }
        if (arrivePoints == null || arrivePoints.Length == 0)
        {
            Debug.LogWarning("[SortingIngredientSpawner] No arrive points assigned.");
            yield break;
        }

        int count = Mathf.Min(itemCount, arrivePoints.Length);
        if (conveyor && conveyor.slots != null && conveyor.slots.Count > 0)
            count = Mathf.Min(count, conveyor.slots.Count);

        // Build a pool min..max and take 'count' UNIQUE numbers
        int rangeSize = Mathf.Max(0, maxValue - minValue + 1);
        if (rangeSize <= 0)
        {
            Debug.LogError("[SortingIngredientSpawner] Invalid range. Ensure maxValue >= minValue.");
            yield break;
        }
        if (count > rangeSize)
        {
            Debug.LogWarning($"[SortingIngredientSpawner] itemCount capped from {count} to range size {rangeSize}.");
            count = rangeSize;
        }

        // Create and shuffle candidate list, then take first 'count'
        var pool = Enumerable.Range(minValue, rangeSize).ToList();
        Shuffle(pool);
        var values = pool.GetRange(0, count);   // unique per round, random order

        // Pick ONE prefab for the whole round
        chosenPrefabThisRound = prefabs[Random.Range(0, prefabs.Length)];
        if (debugLogs) Debug.Log($"[SortingIngredientSpawner] Chosen prefab: {chosenPrefabThisRound.name}");

        Quaternion baseRot = spawnFromPoint ? spawnFromPoint.rotation : Quaternion.identity;
        if (overrideRotation) baseRot *= Quaternion.Euler(rotationOffsetEuler);

        for (int i = 0; i < count; i++)
        {
            Transform arrive = arrivePoints[i];
            if (arrive == null)
            {
                Debug.LogWarning($"[SortingIngredientSpawner] arrivePoints[{i}] is null, skipping.");
                continue;
            }

            Vector3 spawnPos = spawnFromPoint ? spawnFromPoint.position : arrive.position;

            var go = Instantiate(chosenPrefabThisRound, spawnPos, baseRot, itemsParent);

            // Burger Build compatibility
            var dh = go.GetComponent<DragHandler3D>();
            if (dh)
            {
                dh.ingredientValue = values[i];
                dh.SetSpawnPoint(arrive);
                dh.SetDragCamera(stationCamera); // added to ensure the DragHandler3D knows which camera to use for raycasting
                if (setDragModeToSorting) dh.mode = DragHandler3D.DragMode.Sorting;
            }

            // Sorting + label
            var si = go.GetComponent<SortableItem3D>();
            if (si)
            {
                si.home = arrive;
                si.SetValue(values[i]);
            }
            else
            {
                var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label) label.text = values[i].ToString();
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb) { rb.isKinematic = true; rb.useGravity = false; }

            // animate from single origin onto the table
            yield return StartCoroutine(EntryTween(go.transform, arrive.position));

            if (debugLogs) Debug.Log($"[SortingIngredientSpawner] Spawned '{go.name}' -> {values[i]} to {arrive.name}");

            if (spawnStagger > 0f)
                yield return new WaitForSeconds(spawnStagger);
        }
    }

    IEnumerator EntryTween(Transform t, Vector3 dest)
    {
        Vector3 start = t.position;
        float dur = Mathf.Max(0.01f, arriveDuration);
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / dur);
            float e = arriveCurve.Evaluate(u);
            t.position = Vector3.Lerp(start, dest, e);
            yield return null;
        }
        t.position = dest;
    }

    [ContextMenu("Clear Existing")]
    public void ClearExisting()
    {
        if (itemsParent)
        {
            var list = new List<Transform>();
            foreach (Transform child in itemsParent) list.Add(child);
            foreach (var t in list) DestroyImmediateSafe(t.gameObject);
            return;
        }

#if UNITY_2023_1_OR_NEWER
        var items = FindObjectsByType<SortableItem3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var items = FindObjectsOfType<SortableItem3D>(true);
#endif
        foreach (var it in items) DestroyImmediateSafe(it.gameObject);
    }

    void Shuffle(List<int> a)
    {
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }

    void DestroyImmediateSafe(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(go);
        else Destroy(go);
#else
        Destroy(go);
#endif
    }
}
