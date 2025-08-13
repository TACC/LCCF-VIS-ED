using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DishlineController : MonoBehaviour
{
    [Header("Prefab")]
    public PlateController platePrefab;

    [Header("Left: Dirty rack/queue")]
    // where dirty dishes go in the rack
    public Transform dirtyAnchor;
    // how far apart they are in the rack
    public Vector3 dirtySpacingLocal = new Vector3(0, 0.03f, 0);
    public int maxQueue = 8;
    // how often they spawn in the rack
    public Vector2 spawnEvery = new Vector2(1.5f, 4f);
    public float minRackDwellSeconds = 0.35f;

    struct QueueItem { public PlateController plate; public float timeIn; }
    readonly Queue<QueueItem> dirtyQueue = new Queue<QueueItem>();

    [Header("Middle: Work area (max 3)")]
    public Transform[] workSlots = new Transform[3];
    public float toWorkDuration = 0.5f;
    readonly PlateController[] active = new PlateController[3];

    [Header("Right: Clean stack")]
    public Transform cleanAnchor;
    // how far apart from eachother
    public Vector3 cleanSpacingLocal = new Vector3(0, 0.03f, 0);
    public float toCleanDuration = 0.55f;
    // for points
    int cleanCount = 0;

    [Header("Motion look")]
    public AnimationCurve ease = null;
    public float arcHeight = 0.12f;

    Coroutine generatorCo;

    void OnValidate()
    {
        if (workSlots == null || workSlots.Length != 3) workSlots = new Transform[3];
        if (ease == null || ease.length < 2) ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
        if (spawnEvery.x < 0.05f) spawnEvery.x = 0.05f;
        if (spawnEvery.y < spawnEvery.x) spawnEvery.y = spawnEvery.x;
    }

    void Start()
    {
        if (!platePrefab) { Debug.LogError("[Dishline] Plate Prefab is not assigned."); return; }
        if (!dirtyAnchor) { Debug.LogError("[Dishline] Dirty Anchor not assigned."); return; }
        if (!cleanAnchor) { Debug.LogError("[Dishline] Clean Anchor not assigned."); return; }
        for (int i = 0; i < 3; i++) if (!workSlots[i]) { Debug.LogError($"[Dishline] WorkSlot_{i} not assigned."); return; }

        generatorCo = StartCoroutine(DirtyGenerator());
    }

    void Update()
    {
        TryFillWorkSlots();
        LayoutDirtyStack();
    }

    IEnumerator DirtyGenerator()
    {
        while (true)
        {
            if (dirtyQueue.Count < maxQueue)
                EnqueueDirty();

            yield return new WaitForSeconds(Random.Range(spawnEvery.x, spawnEvery.y));
        }
    }

    void EnqueueDirty()
    {
        var plate = Instantiate(platePrefab, dirtyAnchor.position, dirtyAnchor.rotation, transform);
        plate.RespawnDirt();
        plate.OnCleaned += HandlePlateCleaned;

        dirtyQueue.Enqueue(new QueueItem { plate = plate, timeIn = Time.time });
        LayoutDirtyStack();
    }

    void LayoutDirtyStack()
    {
        int idx = 0;
        foreach (var item in dirtyQueue)
        {
            if (!item.plate) continue;
            Vector3 offset = dirtyAnchor.TransformVector(dirtySpacingLocal) * idx;
            item.plate.transform.position = dirtyAnchor.position + offset;
            item.plate.transform.rotation = dirtyAnchor.rotation;
            idx++;
        }
    }

    void TryFillWorkSlots()
    {
        int freeSlot = -1;
        for (int i = 0; i < active.Length; i++)
            if (active[i] == null) { freeSlot = i; break; }
        if (freeSlot == -1) return;
        if (dirtyQueue.Count == 0) return;

        // peek oldest plate without removing it
        var next = dirtyQueue.Peek();
        if (next.plate == null) { dirtyQueue.Dequeue(); return; }

        // enforce dwell time so it appears in the rack briefly first
        if (Time.time - next.timeIn < minRackDwellSeconds) return;

        dirtyQueue.Dequeue();
        StartCoroutine(PlateMover.MoveWithArcRot(
            next.plate.transform,
            next.plate.transform.position, next.plate.transform.rotation,
            workSlots[freeSlot].position, workSlots[freeSlot].rotation,
            toWorkDuration, ease, arcHeight, true));

        active[freeSlot] = next.plate;
    }

    void HandlePlateCleaned(PlateController plate)
    {
        int slot = -1;
        for (int i = 0; i < active.Length; i++)
            if (active[i] == plate) { slot = i; break; }
        if (slot == -1) return;

        // compute next clean stack position
        Vector3 offset = cleanAnchor.TransformVector(cleanSpacingLocal) * cleanCount;
        Vector3 cleanPos = cleanAnchor.position + offset;

        StartCoroutine(MoveToCleanAndFreeSlot(plate, slot, cleanPos));
    }

    IEnumerator MoveToCleanAndFreeSlot(PlateController plate, int slot, Vector3 cleanPos)
    {
        // move & rotate to flat clean stack
        var tmp = new GameObject("CleanTarget").transform;
        tmp.position = cleanPos;
        tmp.rotation = cleanAnchor.rotation;

        yield return PlateMover.MoveWithArcRot(
            plate.transform,
            plate.transform.position, plate.transform.rotation,
            tmp.position, tmp.rotation,
            toCleanDuration, ease, arcHeight, true);

        Destroy(tmp.gameObject);

        var col = plate.GetComponent<Collider>(); if (col) col.enabled = false;
        var spawner = plate.spawner; if (spawner) spawner.enabled = false;
        plate.transform.SetParent(cleanAnchor);
        cleanCount++;

        // free the slot
        active[slot] = null;
    }

    // temporary
    void OnDrawGizmos()
    {
        if (dirtyAnchor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(dirtyAnchor.position, Vector3.one * 0.06f);
            // draw 5 stack markers
            for (int i = 0; i < 5; i++)
            {
                Vector3 o = dirtyAnchor.TransformVector(dirtySpacingLocal) * i;
                Gizmos.DrawWireCube(dirtyAnchor.position + o, Vector3.one * 0.04f);
            }
        }
        if (cleanAnchor)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(cleanAnchor.position, Vector3.one * 0.06f);
        }
        if (workSlots != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var t in workSlots) if (t) Gizmos.DrawWireCube(t.position, Vector3.one * 0.06f);
        }
    }
}
