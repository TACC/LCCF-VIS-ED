using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrderStationTask : MonoBehaviour
{
    [Header("Refs")]
    public SimpleBobber bob;              // bobber on root
    public Transform rightHand;           // picks paper
    public Transform leftHand;            // writes
    public Transform paper;               // optional if spawn proxy

    [Header("Poses / Points")]
    public Transform paperOnCounterPose;
    public Transform paperInHandPose;     // world pose for held paper
    public Transform holdPoint;           // world point in front of chest
    public Transform writeHoverPoint;     // world point above paper

    [Header("Timings")]
    public float reachTime = 0.35f;
    public float liftTime  = 0.35f;

    [Header("Writing motion")]
    public float writeSideAmp   = 0.06f;
    public float writeSideHz    = 1.2f;
    public float writeJitterAmp = 0.01f;
    public float writeJitterHz  = 8f;

    [Header("Control")]
    public bool suspendHandsInBobber = true;   // pause bob on hands during task
    public bool disableBobberWhileRunning = false; // hard kill to rule out overrides
    public bool runOnStart = false;            // auto Begin() on Start for testing

    [Header("Debug / Visualization")]
    public bool logToConsole = true;
    public bool debugSpawnMarkers = true;
    public bool debugSpawnPaperProxy = true;   // makes a temp paper if none
    public bool debugLineToPaper = true;
    public float debugMarkerSize = 0.05f;

    Material debugMaterial;                    // auto-created
    readonly List<GameObject> markers = new();
    LineRenderer line;
    GameObject paperProxy;

    // caches
    Transform paperOriginalParent;
    Vector3 rhBasePosLocal, lhBasePosLocal;
    Quaternion rhBaseRotLocal, lhBaseRotLocal;
    Coroutine runCo;

    // handy test entries
    [ContextMenu("TEST Begin")] public void TestBegin() => Begin();
    [ContextMenu("TEST End")]   public void TestEnd()   => End();

    void Start()
    {
        if (rightHand) { rhBasePosLocal = rightHand.localPosition; rhBaseRotLocal = rightHand.localRotation; }
        if (leftHand)  { lhBasePosLocal = leftHand.localPosition;  lhBaseRotLocal = leftHand.localRotation;  }
        paperOriginalParent = paper ? paper.parent : null;

        if (runOnStart) StartCoroutine(BeginNextFrame());
    }

    IEnumerator BeginNextFrame() { yield return null; Begin(); }

    // ====== Public API ======
    public void Begin()
    {
        if (!ValidateRefs()) return;

        // Confirm hands are registered in the bobber Parts
        if (bob) CheckHandsInBobberParts();

        if (disableBobberWhileRunning && bob) bob.enabled = false;

        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(Run());
    }

    public void End()
    {
        if (runCo != null) StopCoroutine(runCo);
        runCo = null;
        StartCoroutine(ReturnAndReset());
    }

    // ====== Main routine ======
    IEnumerator Run()
    {
        Log("BEGIN order task");

        // Suspend bob on hands so it can't override
        if (suspendHandsInBobber && bob)
        {
            bob.SetPartSuspended(rightHand, true);
            bob.SetPartSuspended(leftHand,  true);
            Log("Suspended bob on hands");
        }

        SetupDebug();

        // Ensure we have a paper
        if (!paper && debugSpawnPaperProxy)
        {
            paperProxy = CreatePaperProxy();
            paper = paperProxy.transform;
            paperOriginalParent = paper.parent;
            Log("Spawned paper proxy");
        }

        // Place paper on counter
        if (paper && paperOnCounterPose)
        {
            paper.SetParent(paperOriginalParent);
            paper.position = paperOnCounterPose.position;
            paper.rotation = paperOnCounterPose.rotation;
            Log("Paper placed on counter pose");
        }

        // Reach
        if (rightHand && paperOnCounterPose)
        {
            Log("Reach to paper");
            yield return MoveWorld(rightHand, rightHand.position, paperOnCounterPose.position, reachTime);
            Log($"Right hand reached: dist now {Vector3.Distance(rightHand.position, paperOnCounterPose.position):F3}m");
        }

        // Pick up (attach to hand at grip pose)
        if (paper && paperInHandPose)
        {
            Log("Attach paper to hand (grip pose)");
            paper.SetParent(rightHand);
            paper.position = paperInHandPose.position;
            paper.rotation = paperInHandPose.rotation;
        }

        // Lift
        if (rightHand && holdPoint)
        {
            Log("Lift to hold point");
            yield return MoveWorld(rightHand, rightHand.position, holdPoint.position, liftTime);
            Log($"Right hand at hold point: dist {Vector3.Distance(rightHand.position, holdPoint.position):F3}m");
        }

        // Loop: write
        float t = 0f;
        Log("Entering writing loop");
        while (true)
        {
            t += Time.deltaTime;

            if (leftHand)
            {
                Vector3 basePos = writeHoverPoint ? writeHoverPoint.position
                                  : (paper ? paper.position + rightHand.up * 0.02f : leftHand.position);

                Vector3 side   = rightHand.right   * (Mathf.Sin(t * writeSideHz   * 2f * Mathf.PI) * writeSideAmp);
                Vector3 jitter = rightHand.up      * (Mathf.Sin(t * writeJitterHz * 2f * Mathf.PI) * writeJitterAmp)
                               + rightHand.forward * (Mathf.Cos(t * writeJitterHz * 2f * Mathf.PI) * writeJitterAmp * 0.6f);

                Vector3 target = basePos + side + jitter;

                leftHand.position = Vector3.Lerp(leftHand.position, target, 12f * Time.deltaTime);
                leftHand.rotation = Quaternion.Slerp(leftHand.rotation,
                    Quaternion.LookRotation((paper ? paper.position - leftHand.position : rightHand.forward), Vector3.up),
                    10f * Time.deltaTime);

                if (line && paper) { line.SetPosition(0, leftHand.position); line.SetPosition(1, paper.position); }
            }

            yield return null;
        }
    }

    IEnumerator ReturnAndReset()
    {
        Log("END → reset");

        // Drop paper back
        if (paper && paperOnCounterPose)
        {
            Vector3 dropPos = paperOnCounterPose.position + Vector3.up * 0.02f;
            if (rightHand) yield return MoveWorld(rightHand, rightHand.position, dropPos, 0.2f);

            paper.SetParent(paperOriginalParent);
            paper.position = paperOnCounterPose.position;
            paper.rotation = paperOnCounterPose.rotation;
            Log("Paper returned to counter");
        }

        // Hands back
        if (rightHand) { yield return MoveLocal(rightHand, rightHand.localPosition, rhBasePosLocal, 0.2f); rightHand.localRotation = rhBaseRotLocal; }
        if (leftHand)  { yield return MoveLocal(leftHand,  leftHand.localPosition,  lhBasePosLocal, 0.2f); leftHand.localRotation  = lhBaseRotLocal;  }

        // Resume bob
        if (suspendHandsInBobber && bob)
        {
            bob.SetPartSuspended(rightHand, false);
            bob.SetPartSuspended(leftHand,  false);
            Log("Resumed bob on hands");
        }

        if (disableBobberWhileRunning && bob) bob.enabled = true;

        CleanupDebug();
    }

    // ====== moves ======
    IEnumerator MoveWorld(Transform tr, Vector3 a, Vector3 b, float time)
    {
        if (!tr) yield break;
        time = Mathf.Max(0.0001f, time);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            tr.position = Vector3.LerpUnclamped(a, b, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        tr.position = b;
    }

    IEnumerator MoveLocal(Transform tr, Vector3 a, Vector3 b, float time)
    {
        if (!tr) yield break;
        time = Mathf.Max(0.0001f, time);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            tr.localPosition = Vector3.LerpUnclamped(a, b, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        tr.localPosition = b;
    }

    // ====== debug helpers ======
    void SetupDebug()
    {
        CleanupDebug();

        if (debugSpawnMarkers)
        {
            SpawnMarker(paperOnCounterPose, Color.green,  "PaperCounter");
            SpawnMarker(paperInHandPose,    Color.cyan,   "PaperGrip");
            SpawnMarker(holdPoint,          Color.yellow, "HoldPoint");
            if (writeHoverPoint) SpawnMarker(writeHoverPoint, Color.magenta, "WriteHover");
        }

        if (debugLineToPaper)
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.01f;
            line.material = GetDebugMaterial();
            line.material.color = new Color(1f, 0.5f, 0f, 0.9f);
        }
    }

    void CleanupDebug()
    {
        foreach (var go in markers) if (go) Destroy(go);
        markers.Clear();

        if (line) Destroy(line); line = null;

        if (paperProxy) { Destroy(paperProxy); paperProxy = null; }
    }

    void SpawnMarker(Transform t, Color c, string label)
    {
        if (!t) return;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var col = go.GetComponent<Collider>(); if (col) Destroy(col);
        go.transform.position = t.position;
        go.transform.localScale = Vector3.one * Mathf.Max(0.001f, debugMarkerSize);
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = GetDebugMaterial();
        mr.sharedMaterial.color = c;
        go.name = $"DBG_{label}";
        markers.Add(go);
    }

    GameObject CreatePaperProxy()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var col = go.GetComponent<Collider>(); if (col) Destroy(col);
        go.name = "DBG_PaperProxy";
        go.transform.localScale = new Vector3(0.20f, 0.002f, 0.28f);
        go.transform.position = paperOnCounterPose ? paperOnCounterPose.position : transform.position;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = GetDebugMaterial();
        mr.sharedMaterial.color = new Color(1f, 1f, 1f, 0.95f);
        return go;
    }

    Material GetDebugMaterial()
    {
        if (debugMaterial) return debugMaterial;
        debugMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (!debugMaterial.shader) debugMaterial.shader = Shader.Find("Unlit/Color");
        if (!debugMaterial.shader) debugMaterial.shader = Shader.Find("Standard");
        return debugMaterial;
    }

    bool ValidateRefs()
    {
        if (!rightHand || !leftHand || (!paper && !debugSpawnPaperProxy) ||
            !paperOnCounterPose || !paperInHandPose || !holdPoint)
        {
            LogError("Missing refs. Need rightHand, leftHand, (paper or proxy), paperOnCounterPose, paperInHandPose, holdPoint.");
            return false;
        }
        return true;
    }

    void CheckHandsInBobberParts()
    {
        bool rh = false, lh = false;
        if (bob != null && bob.parts != null)
        {
            foreach (var p in bob.parts)
            {
                if (!p.target) continue;
                if (p.target == rightHand) rh = true;
                if (p.target == leftHand)  lh = true;
            }
        }
        if (!rh) LogWarning("Right hand is NOT in SimpleBobber.Parts (suspend/lag won’t affect it).");
        if (!lh) LogWarning("Left hand is NOT in SimpleBobber.Parts (suspend/lag won’t affect it).");
    }

    void Log(string msg)       { if (logToConsole) Debug.Log($"[OrderTask] {msg}", this); }
    void LogWarning(string msg){ if (logToConsole) Debug.LogWarning($"[OrderTask] {msg}", this); }
    void LogError(string msg)  { if (logToConsole) Debug.LogError($"[OrderTask] {msg}", this); }
}
