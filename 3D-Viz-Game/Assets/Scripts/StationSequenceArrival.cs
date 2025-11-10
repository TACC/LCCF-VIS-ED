using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class StationSequenceArrival : MonoBehaviour
{
    public enum StationType { Sorting, Dishes }

    [Header("Who/What")]
    public Transform player;                   // your character root (the thing that should rotate)
    public NavMeshAgent agent;                 // optional; if set, we’ll pause agent rotation
    public SimpleClipSequencer sequencer;      // the SAME sequencer on the character
    public StationType stationType = StationType.Sorting;

    [Header("Facing")]
    [Tooltip("Extra yaw to apply when arriving at THIS station (Sorting=+90, Dishes=0, etc).")]
    public float yawOffsetDegrees = 0f;
    [Tooltip("If true, base the facing on this station's forward; if false, keep player's current yaw and just add the offset.")]
    public bool alignToStationForward = true;
    [Tooltip("Smooth turn duration (seconds). Set 0 for an instant snap.")]
    [Range(0f, 0.5f)] public float turnDuration = 0.15f;

    [Header("Auto-detect (optional, no colliders)")]
    public bool enableAutoDetect = false;      // leave OFF if you call OnArrived/OnLeft from your own system
    [Tooltip("Distance to consider 'arrived' when auto-detect is enabled.")]
    public float arriveRadius = 0.5f;
    [Tooltip("Distance to consider 'left' when auto-detect is enabled (should be > arriveRadius).")]
    public float leaveRadius = 1.0f;

    // --- internals ---
    Quaternion _storedRot;
    bool _isRunning;
    bool _hadAgent;
    bool _storedUpdateRotation;
    Coroutine _turnCo;

    void Reset()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!enableAutoDetect || player == null) return;

        float d = Vector3.Distance(player.position, transform.position);

        if (!_isRunning && d <= arriveRadius)
        {
            OnArrivedAtThisStation();
        }
        else if (_isRunning && d > leaveRadius)
        {
            OnLeftThisStation();
        }
    }

    // === Call these from your existing arrival logic if you're not using auto-detect ===
    public void OnArrivedAtThisStation()
    {
        if (player == null || sequencer == null) return;

        // store state
        _storedRot = player.rotation;

        // pause agent-driven rotation so we can face the right way
        if (agent != null)
        {
            _hadAgent = true;
            _storedUpdateRotation = agent.updateRotation;
            agent.updateRotation = false;
        }

        // compute target yaw
        float baseYaw = alignToStationForward ? transform.eulerAngles.y : player.eulerAngles.y;
        float targetYaw = baseYaw + yawOffsetDegrees;

        // face target yaw (smooth or snap)
        if (_turnCo != null) StopCoroutine(_turnCo);
        if (turnDuration <= 0f)
        {
            player.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        }
        else
        {
            _turnCo = StartCoroutine(SmoothFace(player, Quaternion.Euler(0f, targetYaw, 0f), turnDuration));
        }

        // play the correct sequence
        switch (stationType)
        {
            case StationType.Sorting: sequencer.PlaySorting(true); break;
            case StationType.Dishes:  sequencer.PlayDishes(true);  break;
        }

        _isRunning = true;
    }

    public void OnLeftThisStation()
    {
        if (sequencer != null) sequencer.StopSequence();

        // restore agent rotation handling
        if (_hadAgent && agent != null)
            agent.updateRotation = _storedUpdateRotation;

        // (optional) restore original facing; comment out if you prefer to keep the new facing
        // player.rotation = _storedRot;

        _isRunning = false;
    }

    // --- helpers ---
    IEnumerator SmoothFace(Transform t, Quaternion to, float dur)
    {
        Quaternion from = t.rotation;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            float u = elapsed / Mathf.Max(0.0001f, dur);
            t.rotation = Quaternion.Slerp(from, to, u);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.rotation = to;
        _turnCo = null;
    }
}
