using UnityEngine;
using System.Collections;

public class SortingPointSequence : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;                 // Character Animator
    public Transform sortingPoint;            // Empty placed at the stand spot

    [Header("Start")]
    [Tooltip("Start once when within this distance of sortingPoint")]
    public float startRadius = 0.9f;
    public bool autoStartOnPlay = false;

    [Header("Animator")]
    [Tooltip("Animator layer index that contains the sorting states")]
    public int layer = 0;
    [Range(0f, 1f)] public float crossFade = 0.0f;   // 0 = hard cut
    [Tooltip("Try Animator.Play if CrossFade didn't enter quickly (optional)")]
    public bool fallbackPlayIfNotEntered = false;
    [Tooltip("Seconds to wait for entry before optional fallback")]
    public float enterGrace = 0.15f;

    [Header("States in order (exact state names or full paths)")]
    public string[] stateOrder = { "Sorting", "Sorting_2", "Sorting_3", "Sorting_4", "Sorting_5" };

    [Header("Durations (seconds; must match stateOrder)")]
    public float[] stateDurations = { 70f, 30f, 45f, 30f, 45f };

    [Header("Timing mode")]
    [Tooltip("Use realtime (ignores Time.timeScale)")]
    public bool useUnscaledTime = false;

    [Header("Logging")]
    public bool logToConsole = true;

    // --- internals ---
    bool started = false;
    int index = 0;
    Coroutine loopCo;
    int[] hashedPaths;
    string[] resolvedPaths;

    void Awake()
    {
        if (autoStartOnPlay) started = true;
    }

    void Start()
    {
        ResolveAll();
        if (autoStartOnPlay) StartSequence();
    }

    void Update()
    {
        if (started || autoStartOnPlay) return;
        if (!sortingPoint || !animator || stateOrder == null || stateOrder.Length == 0) return;

        if (Vector3.Distance(transform.position, sortingPoint.position) <= startRadius)
            StartSequence();
    }

    public void StartSequence()
    {
        if (started) return;
        started = true;
        index = 0;
        loopCo = StartCoroutine(LoopTimeDriven());
        Log("Sequence START (time-driven, no stopping).");
    }

    IEnumerator LoopTimeDriven()
    {
        // sanity
        if (stateDurations == null || stateDurations.Length != stateOrder.Length)
            Warn("stateDurations length doesn’t match stateOrder; unspecified entries will use 1s.");

        while (true) // loop forever
        {
            int i = index;

            // duration
            float hold = (stateDurations != null && i < stateDurations.Length && stateDurations[i] > 0f)
                         ? stateDurations[i] : 1f;

            // resolve path + hash
            string path = (resolvedPaths != null && i < resolvedPaths.Length) ? resolvedPaths[i] : null;
            int hash    = (hashedPaths   != null && i < hashedPaths.Length)   ? hashedPaths[i]   : 0;

            if (string.IsNullOrEmpty(path) || hash == 0 || !animator.HasState(layer, hash))
            {
                Warn($"State unresolved/missing at index {i}: '{stateOrder[i]}' on layer {layer}. Skipping.");
                Next();
                yield return null;
                continue;
            }

            // switch state
            animator.CrossFadeInFixedTime(path, crossFade, layer, 0f);
            Log($"Play [{i+1}/{stateOrder.Length}] {path} (layer {layer}) for {hold:0.#}s");

            // (optional) ensure we actually entered; if not, force once
            if (fallbackPlayIfNotEntered)
            {
                float t = 0f; bool entered = false;
                while (t < enterGrace)
                {
                    if (animator.GetCurrentAnimatorStateInfo(layer).fullPathHash == hash) { entered = true; break; }
                    t += dt(); yield return null;
                }
                if (!entered)
                {
                    animator.Play(hash, layer, 0f);
                    Log($"Forced Animator.Play → {path}");
                }
            }

            // wait the configured time — then advance no matter what
            float waited = 0f;
            while (waited < hold)
            {
                waited += dt();
                yield return null;
            }

            Next();
        }
    }

    float dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    void Next() => index = (index + 1) % stateOrder.Length;

    // ---------- resolve helpers ----------
    void ResolveAll()
    {
        hashedPaths = new int[stateOrder.Length];
        resolvedPaths = new string[stateOrder.Length];

        for (int i = 0; i < stateOrder.Length; i++)
        {
            string res = ResolvePath(animator, layer, stateOrder[i]);
            int h = (res != null) ? Animator.StringToHash(res) : 0;

            resolvedPaths[i] = res;
            hashedPaths[i]   = h;

            if (res != null) Log($"Resolved [{i+1}] '{stateOrder[i]}' → '{res}' (layer {layer})");
            else Warn($"Could not resolve state '{stateOrder[i]}' on layer {layer}. Try full path like 'Base Layer/{stateOrder[i]}' or 'UpperBody/{stateOrder[i]}'.");
        }
    }

    string ResolvePath(Animator anim, int layerIndex, string shortName)
    {
        if (!anim) return null;

        int exact = Animator.StringToHash(shortName);
        if (anim.HasState(layerIndex, exact)) return shortName;

        string layerName = anim.GetLayerName(layerIndex);
        string[] candidates =
        {
            $"Base Layer/{shortName}",
            $"{layerName}/{shortName}"
        };

        foreach (var c in candidates)
            if (anim.HasState(layerIndex, Animator.StringToHash(c))) return c;

        return null;
    }

    // ---------- logging ----------
    void Log(string msg)  { if (logToConsole) Debug.Log($"[SortingPointSequence] {msg}", this); }
    void Warn(string msg) { if (logToConsole) Debug.LogWarning($"[SortingPointSequence] {msg}", this); }

    void OnDrawGizmosSelected()
    {
        if (!sortingPoint) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(sortingPoint.position, startRadius);
    }
}
