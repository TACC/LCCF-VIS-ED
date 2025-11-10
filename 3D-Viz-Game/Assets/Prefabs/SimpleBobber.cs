using UnityEngine;
using UnityEngine.AI;

public class SimpleBobber : MonoBehaviour
{
    // ====== Types ======
    public enum BlendMode { AutoSmooth, Scripted }

    [System.Serializable]
    public class BobPart
    {
        [Header("Target")]
        public Transform target;
        public bool useLocal = true;

        [Header("Bob")]
        [Tooltip("Multiply global amplitude for this part (1 = same as global).")]
        public float amplitudeScale = 1f;
        [Tooltip("Per-part phase delay in DEGREES (hands start a little later).")]
        [Range(0f, 360f)] public float phaseDegrees = 0f;

        [Header("Optional Trailing (XZ only, during move)")]
        public bool  enableLag = false;
        [Tooltip("Extra per-part multiplier for lag strength.")]
        public float lagStrength = 1f;
        [Tooltip("Max trailing distance (meters).")]
        public float maxLag = 0.18f;
        [Tooltip("Catch-up time (seconds). Lower = snappier.")]
        public float catchupTime = 0.18f;

        // Cached / internal
        [HideInInspector] public Vector3 basePosLocal, basePosWorld;
        [HideInInspector] public Vector3 lagOffsetLocal, lagVelLocal;

        // Task control (Order station etc.)
        [HideInInspector] public bool suspended = false; // when true, bobber won't touch this part
    }

    // ====== Bob settings ======
    [Header("Global Bob (shared by all parts)")]
    [Tooltip("Vertical bob amplitude (meters).")]
    public float idleAmplitude = 0.05f;
    public float moveAmplitude = 0.08f;

    [Tooltip("Bob speed in Hz (cycles/sec). Keep these close for subtle change.")]
    public float idleHz = 1.0f;
    public float moveHz = 1.3f;

    [Header("Blend Driver")]
    [Tooltip("AutoSmooth: bobber eases toward target. Scripted: you drive moveBlend via BlendTo().")]
    public BlendMode blendMode = BlendMode.AutoSmooth;

    [Range(0f, 1f)] public float moveBlend = 0f;    // visible blend (0 idle .. 1 move)
    [Tooltip("Seconds to ease the amplitude blend (AutoSmooth mode).")]
    public float blendTime = 0.25f;
    [Tooltip("Seconds to ease frequency changes (prevents flutter).")]
    public float freqBlendTime = 0.20f;

    // ====== Lag settings (velocity-based trailing) ======
    [Header("Lag Source (optional)")]
    [Tooltip("If null, uses this transform. Set to the object that actually moves (e.g., Agent root).")]
    public Transform lagRoot;
    [Tooltip("If set, uses NavMeshAgent.velocity as the motion signal.")]
    public NavMeshAgent agent;

    [Header("Lag Strength & Timing")]
    [Tooltip("Meters of lag per 1 m/s of speed (global).")]
    public float velocityLagScale = 0.12f;
    [Tooltip("Seconds for smoothing the input velocity (lower = snappier).")]
    public float velocitySmoothing = 0.08f;

    [Header("Lag Visibility (independent fade)")]
    [Tooltip("Below this speed, lag is mostly hidden.")]
    public float minLagSpeed = 0.02f;
    [Tooltip("At/above this speed, lag is at full strength.")]
    public float fullLagSpeed = 0.60f;
    [Tooltip("Seconds to fade lag in when starting to move.")]
    public float lagBlendIn = 0.35f;
    [Tooltip("Seconds to fade lag out when stopping.")]
    public float lagBlendOut = 0.25f;

    [Header("Parts")]
    public BobPart[] parts;

    // ====== Internals ======
    float _moveBlendTarget = 0f, _blendVel = 0f;
    float _currentHz, _hzVel, _phase;                  // continuous oscillator

    Transform _lagRoot;                                // resolved lag root
    Vector3 _prevLagPos;
    Vector3 _smoothedLocalVel;

    float _lagBlend = 0f, _lagBlendVel = 0f;           // independent 0..1 lag gain
    Coroutine _blendCo;                                // scripted blend coroutine

    void Awake()
    {
        // Cache bases
        foreach (var p in parts)
        {
            if (!p.target) continue;
            p.basePosLocal = p.target.localPosition;
            p.basePosWorld = p.target.position;
            p.lagOffsetLocal = Vector3.zero;
            p.lagVelLocal    = Vector3.zero;
        }

        // Resolve lag root
        _lagRoot = lagRoot ? lagRoot : transform;
        _prevLagPos = _lagRoot.position;

        _currentHz = Mathf.Max(0.0001f, idleHz);
        _phase = 0f;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        // --- 1) Amplitude blend ---
        if (blendMode == BlendMode.AutoSmooth)
        {
            moveBlend = Mathf.SmoothDamp(
                moveBlend, _moveBlendTarget, ref _blendVel, Mathf.Max(0.0001f, blendTime));
        }
        // Scripted mode: moveBlend is driven externally via BlendTo() / SetMoveBlendImmediate()

        // --- 2) Frequency blend + continuous phase (no flutter) ---
        float targetHz = Mathf.Lerp(idleHz, moveHz, moveBlend);
        _currentHz = Mathf.SmoothDamp(_currentHz, targetHz, ref _hzVel, Mathf.Max(0.0001f, freqBlendTime));
        _phase += 2f * Mathf.PI * Mathf.Max(0.0001f, _currentHz) * dt;
        if (_phase > 1e6f) _phase -= 2f * Mathf.PI; // keep numbers tame

        float amp = Mathf.Lerp(idleAmplitude, moveAmplitude, moveBlend);

        // --- 3) Motion signal for lag (prefer agent velocity) ---
        Vector3 worldVel = (agent != null && agent.enabled) ? agent.velocity
                                                            : (_lagRoot.position - _prevLagPos) / dt;
        _prevLagPos = _lagRoot.position;

        // Smooth and convert to local space
        Vector3 localVel = transform.InverseTransformVector(worldVel);
        float a = 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, velocitySmoothing));
        _smoothedLocalVel = Vector3.Lerp(_smoothedLocalVel, localVel, a);

        float speed = worldVel.magnitude;

        // --- 4) Independent lag visibility blend (so it doesn’t get lost in fast state changes) ---
        float speedGain = Mathf.InverseLerp(minLagSpeed, fullLagSpeed, speed);
        float lagTarget = moveBlend * speedGain; // respect locomotion state but grow with speed
        float lagTime   = (lagTarget > _lagBlend) ? lagBlendIn : lagBlendOut;
        _lagBlend = Mathf.SmoothDamp(_lagBlend, lagTarget, ref _lagBlendVel, Mathf.Max(0.0001f, lagTime));

        // --- 5) Apply per-part bob + optional lag ---
        foreach (var p in parts)
        {
            if (!p.target) continue;
            if (p.suspended) continue; // a task owns this part (e.g., Order animation)

            // Bob (Y only)
            float phaseRad = p.phaseDegrees * Mathf.Deg2Rad;
            float bobY = Mathf.Sin(_phase + phaseRad) * (amp * p.amplitudeScale);

            // Lag (XZ only)
            Vector3 lagXZ = Vector3.zero;
            if (p.enableLag)
            {
                // velocity-based trailing; scaled by independent lag blend and per-part strength
                Vector3 desired = -_smoothedLocalVel * (velocityLagScale * Mathf.Max(0f, p.lagStrength)) * _lagBlend;
                desired.y = 0f;

                p.lagOffsetLocal = Vector3.SmoothDamp(
                    p.lagOffsetLocal, desired, ref p.lagVelLocal, p.catchupTime);

                // clamp to circle in XZ
                Vector2 xz = new Vector2(p.lagOffsetLocal.x, p.lagOffsetLocal.z);
                if (xz.magnitude > p.maxLag) xz = xz.normalized * p.maxLag;
                lagXZ = new Vector3(xz.x, 0f, xz.y);
            }
            else
            {
                // if lag disabled, keep offset cleaned up
                p.lagOffsetLocal = Vector3.SmoothDamp(
                    p.lagOffsetLocal, Vector3.zero, ref p.lagVelLocal, p.catchupTime);
            }

            // Final apply
            Vector3 finalLocal = p.basePosLocal + lagXZ + new Vector3(0f, bobY, 0f);
            if (p.useLocal)
                p.target.localPosition = finalLocal;
            else
                p.target.position = p.basePosWorld + transform.TransformVector(lagXZ) + new Vector3(0f, bobY, 0f);
        }
    }

    // ====== Public API ======

    /// <summary>Set desired move state (only used in AutoSmooth mode).</summary>
    public void SetMoving(bool moving) => _moveBlendTarget = moving ? 1f : 0f;

    /// <summary>Set desired blend directly (0..1). Used by AutoSmooth mode as target.</summary>
    public void SetMoveBlendTarget(float t01) => _moveBlendTarget = Mathf.Clamp01(t01);

    /// <summary>Snap to a blend immediately (affects both visible and internal target).</summary>
    public void SetMoveBlendImmediate(float v01)
    {
        moveBlend = _moveBlendTarget = Mathf.Clamp01(v01);
        _blendVel = _hzVel = 0f;
        _currentHz = Mathf.Lerp(idleHz, moveHz, moveBlend);
    }

    /// <summary>Scripted, deterministic tween (works best with BlendMode.Scripted).</summary>
    public void BlendTo(float target01, float seconds)
    {
        if (_blendCo != null) StopCoroutine(_blendCo);
        _blendCo = StartCoroutine(BlendToCo(Mathf.Clamp01(target01), Mathf.Max(0.0001f, seconds)));
    }

    System.Collections.IEnumerator BlendToCo(float target, float seconds)
    {
        float start = moveBlend;
        float t = 0f;

        // keep internal target synced so AutoSmooth logic never fights this
        _moveBlendTarget = start;
        _blendVel = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / seconds;
            float v = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            moveBlend = _moveBlendTarget = v;
            yield return null;
        }
        moveBlend = _moveBlendTarget = target;
        _blendVel = 0f;
        _blendCo = null;
    }

    /// <summary>Temporarily stop bobbing/lag on a specific part (e.g., during a station task).</summary>
    public void SetPartSuspended(Transform t, bool suspend)
    {
        bool found = false;
        foreach (var p in parts)
        {
            if (p.target == t)
            {
                p.suspended = suspend;
                found = true;
                break;
            }
        }
        if (!found) Debug.LogWarning($"[SimpleBobber] Tried to {(suspend ? "suspend" : "resume")} '{t?.name}', but it's not in Parts.");
    }

    /// <summary>Convenience to suspend/resume all parts.</summary>
    public void SetAllSuspended(bool suspend)
    {
        foreach (var p in parts) p.suspended = suspend;
    }
}
