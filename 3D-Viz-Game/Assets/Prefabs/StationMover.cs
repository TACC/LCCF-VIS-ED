using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class StationMover : MonoBehaviour
{
    [System.Serializable]
    public class Station
    {
        public string name;
        public Transform standPoint;        // optional: exact spot (will be clamped)
        public Transform faceTarget;        // the counter to look at

        [Header("If standPoint is null, auto-place in front of the counter")]
        public float standDistance = 0.35f; // desired distance from counter
        public float sampleRadius  = 0.6f;  // search radius for nearest NavMesh
    }

    public Station[] stations;

    [Header("Refs")]
    public NavMeshAgent agent;
    public SimpleBobber bobber;            // your bob/blend script

    [Header("Tuning")]
    public float faceSpeedDeg = 540f;
    public float extraArriveBuffer = 0.05f; // added to agent.stoppingDistance

    Coroutine running;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        bobber = GetComponent<SimpleBobber>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (agent) agent.updateRotation = false; // we rotate manually
        // Ensure we start on the mesh
        if (agent && NavMesh.SamplePosition(transform.position, out var hit, 1f, agent.areaMask))
            agent.Warp(hit.position);
    }

    // === BUTTON API (unchanged) ===
    public void Go0() => GoToStation(0);
    public void Go1() => GoToStation(1);
    public void Go2() => GoToStation(2);
    public void Go3() => GoToStation(3);
    public void GoToStation(int index)
    {
        if (index < 0 || index >= stations.Length) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(MoveRoutine(stations[index]));
    }

    IEnumerator MoveRoutine(Station s)
    {
        // 1) Find a valid destination on the NavMesh
        Vector3 dest = GetValidStandPos(s, out bool ok);
        if (!ok) { Debug.LogWarning($"No valid NavMesh near {s.name}"); yield break; }

        // 2) Start moving
        bobber?.SetMoveBlendTarget(1f);
        agent.isStopped = false;
        agent.SetDestination(dest);

        // 3) Travel (face desired velocity while moving)
        float arrive = Mathf.Max(0f, agent.stoppingDistance + extraArriveBuffer);
        while (agent.pathPending) yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"Invalid path to {s.name}");
            agent.isStopped = true; bobber?.SetMoveBlendTarget(0f);
            yield break;
        }

        while (!agent.pathPending && agent.remainingDistance > arrive)
        {
            Vector3 vel = agent.desiredVelocity; vel.y = 0f;
            if (vel.sqrMagnitude > 0.0004f)
            {
                var want = Quaternion.LookRotation(vel, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, faceSpeedDeg * Time.deltaTime);
            }
            yield return null;
        }

        // 4) Stop + blend to idle
        agent.isStopped = true;
        agent.ResetPath();
        bobber?.SetMoveBlendTarget(0f);

        // 5) Face the counter cleanly
        yield return FaceYaw(s.faceTarget.position);
    }

    Vector3 GetValidStandPos(Station s, out bool ok)
    {
        Vector3 desired = s.standPoint
            ? s.standPoint.position
            : s.faceTarget.position - s.faceTarget.forward * s.standDistance;

        desired.y = transform.position.y; // flatten

        if (NavMesh.SamplePosition(desired, out var hit, s.sampleRadius, agent.areaMask))
        { ok = true; return hit.position; }

        ok = false; return desired;
    }

    IEnumerator FaceYaw(Vector3 lookAtWorld)
    {
        Vector3 dir = lookAtWorld - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) yield break;
        Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
        while (Quaternion.Angle(transform.rotation, want) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, faceSpeedDeg * Time.deltaTime);
            yield return null;
        }
        transform.rotation = want;
    }
}
