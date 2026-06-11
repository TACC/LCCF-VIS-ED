using System.Collections;
using UnityEngine;

public class SingleNPCManager3D : MonoBehaviour
{
    [Header("NPC root (parent of Capsule + Sphere). If null, uses this.transform")]
    public Transform npcRoot;

    [Header("Optional: tint these renderers each round")]
    public Renderer[] colorTargets;

    [Header("Waypoints")]
    public Transform entryLeft;    // offscreen start
    public Transform talkPoint;    // on-screen stop
    public Transform exitLeft;     // offscreen end (can be same as entry)

    public enum MoveAxis { X, Y, Z }

    [Header("Movement")]
    public MoveAxis moveAxis = MoveAxis.Z;   // <<< set to Z
    public float moveSpeed = 3.5f;
    public bool faceMoveDirection = true;
    public float turnSpeed = 12f;
    public float reenterDelay = 0.4f;

    public GameObject npc;

    [Header("Start / Robustness")]
    [Tooltip("Force-teleport the NPC to Entry at startup (Awake/OnEnable/Start).")]
    public bool forceTeleportAtStart = true;
    [Tooltip("Match Entry rotation on teleport.")]
    public bool matchRotationOnTeleport = true;
    [Tooltip("Zero Rigidbody velocity/angVel on teleport.")]
    public bool resetRigidbodyOnTeleport = true;

    [Tooltip("Detach waypoints from their parents at runtime to stop them from moving.")]
    public bool detachWaypointsAtRuntime = true;
    [Tooltip("After detaching, restore their world positions exactly.")]
    public bool restoreWaypointPositions = true;

    [Tooltip("If true, snap TALK and EXIT to NPC's plane after teleporting to Entry.")]
    public bool projectTalkAndExitToNPCPlane = false; // default OFF to avoid moving points

    [Header("UI + Order")]
    public GameObject dialogueBox;
    public NPCOrderManager_Single orderMgr;

    Rigidbody rb;

    // cache original world poses and parents
    Vector3 entryPos, talkPos, exitPos;
    Quaternion entryRot, talkRot, exitRot;
    Transform entryParent, talkParent, exitParent;

    void Awake()
    {
        if (!npcRoot) npcRoot = transform;
        rb = npcRoot.GetComponent<Rigidbody>();

        CacheWaypointPoses();

        if (detachWaypointsAtRuntime)
        {
            if (entryLeft) entryLeft.SetParent(null, true);
            if (talkPoint) talkPoint.SetParent(null, true);
            if (exitLeft)  exitLeft.SetParent(null, true);
            if (restoreWaypointPositions) RestoreWaypointPoses();
        }

        if (forceTeleportAtStart) TeleportToEntry();
    }

    void OnEnable()
    {
        if (forceTeleportAtStart) TeleportToEntry();
    }

    IEnumerator Start()
    {
        yield return null;
        if (forceTeleportAtStart) TeleportToEntry();

        if (projectTalkAndExitToNPCPlane)
        {
            ProjectToAxisPlane(talkPoint);
            ProjectToAxisPlane(exitLeft);
        }

        if (dialogueBox) dialogueBox.SetActive(false);
        yield return EnterAndStartOrder();
    }

    void CacheWaypointPoses()
    {
        if (entryLeft) { entryParent = entryLeft.parent; entryPos = entryLeft.position; entryRot = entryLeft.rotation; }
        if (talkPoint) { talkParent  = talkPoint.parent; talkPos  = talkPoint.position; talkRot  = talkPoint.rotation; }
        if (exitLeft)  { exitParent  = exitLeft.parent;  exitPos  = exitLeft.position;  exitRot  = exitLeft.rotation; }
    }

    void RestoreWaypointPoses()
    {
        if (entryLeft) { entryLeft.position = entryPos; entryLeft.rotation = entryRot; }
        if (talkPoint) { talkPoint.position  = talkPos; talkPoint.rotation  = talkRot; }
        if (exitLeft)  { exitLeft.position   = exitPos; exitLeft.rotation   = exitRot; }
    }

    // -------- public API --------
    public void BeginNextRound() => StartCoroutine(ExitThenReenter());

    // -------- flow --------
    IEnumerator EnterAndStartOrder()
    {
        npc.SetActive(true);
        yield return MoveTo(talkPoint ? talkPoint.position : npcRoot.position);
        if (dialogueBox) dialogueBox.SetActive(true);
        if (orderMgr)    orderMgr.GenerateOrder();
    }

    IEnumerator ExitThenReenter()
    {
        if (dialogueBox) dialogueBox.SetActive(false);
        yield return MoveTo(exitLeft ? exitLeft.position : npcRoot.position);

        TintNPC(new Color(Random.value, Random.value, Random.value));

        TeleportToEntry();
        yield return new WaitForSeconds(reenterDelay);

        yield return EnterAndStartOrder();
    }

    // -------- movement --------
    Vector3 ConstrainToAxis(Vector3 from, Vector3 target)
    {
        switch (moveAxis)
        {
            case MoveAxis.X: target.y = from.y; target.z = from.z; break;
            case MoveAxis.Y: target.x = from.x; target.z = from.z; break;
            case MoveAxis.Z: target.x = from.x; target.y = from.y; break;
        }
        return target;
    }

    IEnumerator MoveTo(Vector3 target)
    {
        if (!npcRoot) yield break;

        Vector3 targetPos = ConstrainToAxis(npcRoot.position, target);

        while ((npcRoot.position - targetPos).sqrMagnitude > 0.0004f)
        {
            Vector3 next = Vector3.MoveTowards(npcRoot.position, targetPos, moveSpeed * Time.deltaTime);

            if (faceMoveDirection)
            {
                Vector3 dir = next - npcRoot.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-6f)
                    npcRoot.rotation = Quaternion.Slerp(npcRoot.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            }

            SetPosition(next);
            yield return null;
        }
    }

    void SetPosition(Vector3 p)
    {
        if (rb && !rb.isKinematic) rb.MovePosition(p);
        else npcRoot.position = p;
    }

    void TeleportToEntry()
    {
        if (!entryLeft || !npcRoot) return;

        Vector3 p = ConstrainToAxis(npcRoot.position, entryLeft.position);
        if (rb && !rb.isKinematic)
        {
            rb.position = p;
            if (matchRotationOnTeleport) rb.rotation = entryLeft.rotation;

            if (resetRigidbodyOnTeleport)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            npcRoot.position = p;
            if (matchRotationOnTeleport) npcRoot.rotation = entryLeft.rotation;
        }
    }

    void ProjectToAxisPlane(Transform t)
    {
        if (!t || !npcRoot) return;
        Vector3 p = t.position;
        switch (moveAxis)
        {
            case MoveAxis.X: p.y = npcRoot.position.y; p.z = npcRoot.position.z; break;
            case MoveAxis.Y: p.x = npcRoot.position.x; p.z = npcRoot.position.z; break;
            case MoveAxis.Z: p.x = npcRoot.position.x; p.y = npcRoot.position.y; break;
        }
        t.position = p;
    }

    void TintNPC(Color c)
    {
        if (colorTargets == null) return;
        var block = new MaterialPropertyBlock();
        foreach (var r in colorTargets)
        {
            if (!r) continue;
            r.GetPropertyBlock(block);
            if (r.sharedMaterial && r.sharedMaterial.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", c);
            else
                block.SetColor("_Color", c);
            r.SetPropertyBlock(block);
        }
    }

    // gizmos
    void OnDrawGizmos()
    {
        if (!npcRoot) npcRoot = transform;
        if (entryLeft) { Gizmos.color = Color.red;   Gizmos.DrawSphere(entryLeft.position, 0.1f); }
        if (talkPoint) { Gizmos.color = Color.green; Gizmos.DrawSphere(talkPoint.position, 0.1f); }
        if (exitLeft)  { Gizmos.color = Color.cyan;  Gizmos.DrawSphere(exitLeft.position, 0.1f); }
        if (entryLeft && talkPoint) Gizmos.DrawLine(entryLeft.position, talkPoint.position);
        if (talkPoint && exitLeft)  Gizmos.DrawLine(talkPoint.position, exitLeft.position);
    }
}
