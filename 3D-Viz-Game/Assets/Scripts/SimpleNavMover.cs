using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleNavMover : MonoBehaviour
{
    [Header("Stations (drag your station Transforms here)")]
    public List<Transform> stations = new List<Transform>();

    [Header("Movement")]
    public float stopDistance = 0.35f;
    public bool faceAlongPath = true;
    public float turnSpeedDegPerSec = 540f;
    public bool autoMoveToFirstOnPlay = false;   // <— toggle to test without UI

    [Header("Animator (optional)")]
    public Animator animator;
    public string speedParam = "Speed";
    public bool disableRootMotion = true;

    [Header("Events")]
    public UnityEvent<Transform> onArrived;

    NavMeshAgent agent;
    Transform currentTarget;
    bool moving;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();
        if (animator && disableRootMotion) animator.applyRootMotion = false;

        agent.updateRotation = !faceAlongPath;
        agent.updatePosition = true;
        if (agent.speed <= 0f) agent.speed = 3.5f;
        if (agent.acceleration <= 0f) agent.acceleration = 8f;

        // Ensure the character starts ON the NavMesh
        if (!NavMesh.SamplePosition(transform.position, out var myHit, 2f, NavMesh.AllAreas))
        {
            if (NavMesh.SamplePosition(transform.position, out var nearby, 10f, NavMesh.AllAreas))
            {
                agent.Warp(nearby.position);
                Debug.Log("[SimpleNavMover] Warped character to nearest NavMesh point.");
            }
            else
            {
                Debug.LogError("[SimpleNavMover] Character is off NavMesh and no mesh found nearby.");
            }
        }
    }

    void Start()
    {
        if (autoMoveToFirstOnPlay)
        {
            if (stations.Count > 0 && stations[0])
                GoTo(stations[0]);
            else
                Debug.LogWarning("[SimpleNavMover] autoMoveToFirstOnPlay is ON but stations[0] is missing.");
        }
    }

    void Update()
    {
        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, agent.velocity.magnitude);

        if (!moving || currentTarget == null) return;

        if (!agent.pathPending)
        {
            // Optional facing
            if (faceAlongPath)
            {
                Vector3 v = agent.desiredVelocity.sqrMagnitude > 0.0001f ? agent.desiredVelocity : agent.velocity;
                v.y = 0f;
                if (v.sqrMagnitude > 0.0004f)
                {
                    Quaternion goal = Quaternion.LookRotation(v);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeedDegPerSec * Time.deltaTime);
                }
            }

            bool pathReady = agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete;
            float stop = Mathf.Max(stopDistance, agent.stoppingDistance);

            if (pathReady && agent.remainingDistance <= stop)
            {
                moving = false;
                agent.isStopped = true;
                onArrived?.Invoke(currentTarget);
            }
        }
    }

    // -------- Public API (hook buttons to these) --------

    public void GoToIndex(int index)
    {
        if (index < 0 || index >= stations.Count || stations[index] == null)
        {
            Debug.LogWarning($"[SimpleNavMover] GoToIndex: index {index} invalid or null.");
            return;
        }
        GoTo(stations[index]);
    }

    public void GoNext()
    {
        if (stations.Count == 0) { Debug.LogWarning("[SimpleNavMover] No stations set."); return; }
        int next = currentTarget ? (stations.IndexOf(currentTarget) + 1 + stations.Count) % stations.Count : 0;
        GoToIndex(next);
    }

    public void GoTo(Transform target)
    {
        if (target == null) { Debug.LogWarning("[SimpleNavMover] GoTo: target is null."); return; }

        // Snap destination to NavMesh
        Vector3 dest = target.position;
        if (!NavMesh.SamplePosition(dest, out var hit, 2f, NavMesh.AllAreas))
        {
            Debug.LogError($"[SimpleNavMover] Target '{target.name}' is not on the NavMesh (no hit within 2m).");
            return;
        }

        currentTarget = target;
        moving = true;

        agent.isStopped = false;
        agent.updatePosition = true;
        agent.stoppingDistance = Mathf.Max(0.01f, stopDistance);
        agent.SetDestination(hit.position);

        // Immediate feedback
        Debug.Log($"[SimpleNavMover] Moving to '{target.name}'. hasPath:{agent.hasPath} status:{agent.pathStatus}");
    }

    public void Stop()
    {
        moving = false;
        agent.isStopped = true;
    }

    public void Resume()
    {
        if (currentTarget == null) return;
        agent.isStopped = false;
        moving = true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (stations == null) return;
        Gizmos.color = Color.cyan;
        foreach (var s in stations)
        {
            if (!s) continue;
            Gizmos.DrawWireSphere(s.position, 0.075f);
            Gizmos.DrawLine(transform.position, s.position);
        }
    }
#endif
}
