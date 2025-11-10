// PlayerNavAnimator.cs
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;   // Timeline
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerNavAnimator : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    [Tooltip("Assign your mesh root CHILD here (e.g., ModelRoot). Do NOT use the same transform as the agent root.")]
    public Transform rotateRoot;

    [Header("Move")]
    public float speedParamScale = 1f;     // scales Speed param if needed
    public float arrivalSlack = 0.05f;     // extra tolerance over stoppingDistance
    [Tooltip("If true, while MOVING, the model faces 180° opposite the agent's forward (so +Z points 'backwards').")]
    public bool flipModelWhenMoving = true;
    public float moveEpsilon = 0.01f;      // velocity threshold to consider 'moving'

    [Header("Facing at task")]
    public float rotateSpeedDeg = 540f;    // turn speed while aligning to task
    [Tooltip("Base yaw offset used when facing tasks (not applied to the move-flip). Leave 0 unless your mesh is authored sideways.")]
    public float baseYawOffset = 0f;

    [Header("Knife (Timeline Loop)")]
    [Tooltip("PlayableDirector that owns the knife's Timeline (Animation track bound to the knife).")]
    public PlayableDirector knifeDirector;
    [Tooltip("Animator on the knife object. Leave Controller = None. We'll enable/disable it at runtime.")]
    public Animator knifeAnimator;

    [Header("Debug")]
    public bool debugLogs = false;          // <-- toggle logs here
    public bool debugGizmos = false;        // optional scene gizmos while selected
    public float debugLogInterval = 0.5f;   // seconds for periodic logs
    float _lastDebugTime;

    NavMeshAgent agent;
    TaskPoint currentTask;
    bool performing;
    bool warnedRotateRootIsRoot = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!rotateRoot) rotateRoot = transform;

        if (knifeDirector) knifeDirector.playOnAwake = false;
        if (knifeAnimator)
        {
            knifeAnimator.enabled = false;
            knifeAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        Log($"Awake: animator={(animator?animator.name:"null")} rotateRoot={(rotateRoot?rotateRoot.name:"null")}");
    }

    void Update()
    {
        // Drive Idle <-> Moving
        float speed = agent.velocity.magnitude * speedParamScale;
        if (animator) animator.SetFloat("Speed", speed);

        // Periodic status log (optional)
        if (debugLogs && Time.time - _lastDebugTime > debugLogInterval)
        {
            _lastDebugTime = Time.time;
            Log($"Update: speed={speed:F2} performing={performing} hasPath={agent.hasPath} " +
                $"remDist={agent.remainingDistance:F2} isStopped={agent.isStopped} pathPending={agent.pathPending} status={agent.pathStatus}");
        }

        // Arrival & perform
        if (!performing && currentTask && Arrived(agent, currentTask))
        {
            Log($"Arrived at '{currentTask.name}'. Starting DoTask.");
            StartCoroutine(DoTask(currentTask));
        }
    }

    // Keep the mesh child aligned to agent facing; flip 180° only WHILE MOVING
    void LateUpdate()
    {
        if (!rotateRoot) return;

        // Avoid fighting the agent by rotating only the CHILD, not the root
        if (rotateRoot == transform)
        {
            if (!warnedRotateRootIsRoot)
            {
                LogWarning("rotateRoot is the same as the agent root. Assign a mesh child (e.g., ModelRoot) to avoid rotation conflicts.");
                warnedRotateRootIsRoot = true;
            }
            return;
        }

        if (performing) return; // during tasks we handle rotation inside DoTask()

        // Determine if we're moving
        Vector3 vel = agent != null ? agent.velocity : Vector3.zero;
        vel.y = 0f;
        bool isMoving = vel.sqrMagnitude > (moveEpsilon * moveEpsilon);

        // Base orientation = agent/root rotation
        Quaternion baseRot = transform.rotation;

        // If moving and flip is enabled, add 180° yaw so +Z points backwards relative to motion
        float extraYaw = (flipModelWhenMoving && isMoving) ? 180f : 0f;
        rotateRoot.rotation = baseRot * Quaternion.Euler(0f, extraYaw, 0f);
    }

    bool Arrived(NavMeshAgent a, TaskPoint t)
    {
        bool arrived = !a.pathPending &&
                       a.remainingDistance <= Mathf.Max(a.stoppingDistance + arrivalSlack, 0.05f);
        if (debugLogs)
        {
            Log($"Arrived? {arrived}  task='{t.name}'  rem={a.remainingDistance:F3} stop={a.stoppingDistance:F3} slack={arrivalSlack:F3} " +
                $"hasPath={a.hasPath} status={a.pathStatus}");
        }
        return arrived;
    }

    IEnumerator DoTask(TaskPoint t)
    {
        performing = true;

        // Stop and face the task
        agent.isStopped = true;
        agent.updateRotation = false;
        Log($"DoTask: BEGIN for '{t.name}'  animTrigger='{t.animTrigger}'");

        // ---- Facing block with per-station options ----
        if (t.lookAtTarget && rotateRoot)
        {
            Log($"Facing: using lookAtTarget='{t.lookAtTarget.name}', matchForward={t.matchLookTargetForward}, addYaw={t.additionalYaw}, invert180={t.invertFacing180}");
            while (true)
            {
                Vector3 dir = t.matchLookTargetForward
                    ? t.lookAtTarget.forward
                    : (t.lookAtTarget.position - rotateRoot.position);

                dir.y = 0f;

                // NEW: allow 180° flip per-station (e.g., dish station pointing backwards)
                if (t.invertFacing180)
                    dir = -dir;

                if (dir.sqrMagnitude < 0.0001f)
                {
                    LogWarning("Facing: computed direction is near zero; using current forward as fallback.");
                    dir = rotateRoot.forward;
                }

                float yaw = baseYawOffset + t.additionalYaw; // per-station twist (e.g., +90°)
                Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, yaw, 0f);

                rotateRoot.rotation = Quaternion.RotateTowards(
                    rotateRoot.rotation, targetRot, rotateSpeedDeg * Time.deltaTime);

                float angle = Quaternion.Angle(rotateRoot.rotation, targetRot);
                if (angle <= t.faceAngleThreshold)
                {
                    Log($"Facing: aligned within {angle:F1}° (threshold {t.faceAngleThreshold}°).");
                    break;
                }

                yield return null;
            }
        }
        else
        {
            if (!rotateRoot) LogWarning("Facing skipped: rotateRoot is null.");
            if (!t.lookAtTarget) LogWarning($"Facing skipped: TaskPoint '{t.name}' has no lookAtTarget.");
        }

        // Trigger the station animation and (optionally) start the knife loop
        if (!string.IsNullOrEmpty(t.animTrigger) && animator)
        {
            Log($"Animator: SetTrigger('{t.animTrigger}')");
            animator.SetTrigger(t.animTrigger);

            // NEW: only kitchen (enableKnife == true) should start the knife Timeline
            if (t.enableKnife)
                StartKnifeLoop();
        }
        else
        {
            LogWarning($"Animator: trigger missing or animator null for task '{t.name}'.");
        }

        // Wait for the state to finish via Exit Time (state name == trigger name)
        yield return WaitForStateExit(t.animTrigger);

        // NEW: stop knife loop only if it was started for this task
        if (t.enableKnife)
            StopKnifeLoop();

        // Cleanup
        agent.updateRotation = true;
        agent.isStopped = false;
        performing = false;
        currentTask = null;

        Log("DoTask: END (cleanup done).");
    }

    IEnumerator WaitForStateExit(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) yield break;

        const int baseLayer = 0;
        // Wait until we ENTER the state
        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(baseLayer);
            if (st.IsName(triggerName))
            {
                Log($"Animator: ENTER state '{triggerName}'.");
                break;
            }
            yield return null;
        }
        // Then wait until we EXIT it
        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(baseLayer);
            if (!st.IsName(triggerName))
            {
                Log($"Animator: EXIT state '{triggerName}'.");
                break;
            }
            yield return null;
        }
    }

    // --- Knife Timeline helpers (loop while performing) ---

    void StartKnifeLoop()
    {
        if (!knifeDirector) return;

        if (knifeAnimator) knifeAnimator.enabled = true;
        knifeDirector.extrapolationMode = DirectorWrapMode.Loop;
        knifeDirector.time = 0;
        knifeDirector.Evaluate();
        knifeDirector.Play();

        Log("Knife: LOOP start");
    }

    void StopKnifeLoop()
    {
        if (!knifeDirector) return;

        knifeDirector.Stop();
        knifeDirector.time = 0;
        knifeDirector.Evaluate();

        if (knifeAnimator) knifeAnimator.enabled = false;

        Log("Knife: LOOP stop");
    }

    // --- Public API ---

    public void GoToTask(TaskPoint task)
    {
        currentTask = task;
        agent.stoppingDistance = task ? task.stopDistance : 0f;
        agent.updateRotation = true;
        agent.isStopped = false;
        if (task)
        {
            Vector3 dest = task.ApproachPosition;
            agent.SetDestination(dest);
            Log($"GoToTask: '{task.name}' dest={dest} stopDist={agent.stoppingDistance}");
        }
        else
        {
            LogWarning("GoToTask: task is null.");
        }
    }

    public void GoToPoint(Vector3 worldPos)   // optional click-to-move
    {
        currentTask = null;
        agent.stoppingDistance = 0f;
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.SetDestination(worldPos);
        Log($"GoToPoint: {worldPos}");
    }

    // --- Debug helpers ---
    void Log(string msg)
    {
        if (debugLogs) Debug.Log($"[PlayerNavAnimator] {msg}", this);
    }
    void LogWarning(string msg)
    {
        if (debugLogs) Debug.LogWarning($"[PlayerNavAnimator] {msg}", this);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        if (!rotateRoot) return;

        // Forward ray for rotateRoot
        Gizmos.color = Color.green;
        Gizmos.DrawRay(rotateRoot.position, rotateRoot.forward * 0.6f);

        // Current task target line
        if (currentTask)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTask.ApproachPosition);
            if (currentTask.lookAtTarget)
            {
                Gizmos.color = Color.yellow;
                Vector3 baseDir = currentTask.matchLookTargetForward
                    ? currentTask.lookAtTarget.forward
                    : (currentTask.lookAtTarget.position - rotateRoot.position);
                baseDir.y = 0f;

                if (currentTask.invertFacing180)
                    baseDir = -baseDir;

                Vector3 fwd = Quaternion.Euler(0f, baseYawOffset + currentTask.additionalYaw, 0f) *
                              (baseDir.sqrMagnitude > 0.001f ? baseDir.normalized : rotateRoot.forward);
                Gizmos.DrawRay(rotateRoot.position, fwd * 0.6f);
            }
        }
    }
#endif
}
