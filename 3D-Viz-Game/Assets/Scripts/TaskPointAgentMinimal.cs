using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TaskPointAgentMinimal : MonoBehaviour
{
    [Header("Rotation Targets")]
    public Transform rotateRoot;              // optional; defaults to this.transform
    public float moveTurnSpeed = 540f;        // deg/sec while moving
    public float faceTurnSpeed = 720f;        // deg/sec when aligning to lookAtTarget
    public float faceSlackDegrees = 3f;       // extra forgiveness beyond TaskPoint.faceAngleThreshold
    public float minVelocityToFace = 0.03f;   // m/s threshold to consider "moving"

    [Header("Animation (optional)")]
    public Animator animator;                 // optional
    public bool fireAnimTriggerOnFace = true;

    [Header("Debug")]
    public bool debugDraw;

    NavMeshAgent agent;
    TaskPoint current;
    bool arrived;
    bool firedTrigger;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();

        // We'll drive rotation manually (both while moving and at the station)
        agent.updateRotation = false;
    }

    /// Call this from your button
    public void GoTo(TaskPoint tp)
    {
        if (tp == null) return;

        current = tp;
        arrived = false;
        firedTrigger = false;

        agent.isStopped = false;
        agent.stoppingDistance = Mathf.Max(0.01f, tp.stopDistance);
        agent.SetDestination(tp.ApproachPosition);
    }

    void Update()
    {
        if (current == null) return;

        // 1) Arrival check
        if (!arrived && Arrived(agent, current))
        {
            arrived = true;
            agent.isStopped = true; // we’ll rotate in place now
        }

        // 2) Rotation behavior
        if (!arrived)
        {
            FaceMoveDirection();           // face along the path while moving
        }
        else
        {
            bool facing = FaceLookTarget(current); // final alignment at station
            if (facing && fireAnimTriggerOnFace && animator && !firedTrigger)
            {
                if (!string.IsNullOrEmpty(current.animTrigger))
                    animator.SetTrigger(current.animTrigger);
                firedTrigger = true;
            }
        }

        if (debugDraw)
        {
            var t = rotateRoot ? rotateRoot : transform;
            Debug.DrawRay(t.position, t.forward * 0.6f, Color.green);
            if (current.lookAtTarget)
            {
                var flat = current.lookAtTarget.position; flat.y = t.position.y;
                Debug.DrawLine(t.position, flat, Color.yellow);
            }
        }
    }

    // ---------- helpers ----------

    static bool Arrived(NavMeshAgent a, TaskPoint tp)
    {
        if (a.pathPending) return false;
        float stop = Mathf.Max(a.stoppingDistance, tp.stopDistance);
        return a.remainingDistance <= stop;
    }

    void FaceMoveDirection()
    {
        var t = rotateRoot ? rotateRoot : transform;

        // Prefer agent.desiredVelocity; fall back to velocity
        Vector3 v = agent.desiredVelocity.sqrMagnitude > 0.0001f ? agent.desiredVelocity : agent.velocity;
        if (v.sqrMagnitude < (minVelocityToFace * minVelocityToFace)) return;

        v.y = 0f;
        Quaternion goal = Quaternion.LookRotation(v);
        t.rotation = Quaternion.RotateTowards(t.rotation, goal, moveTurnSpeed * Time.deltaTime);
    }

    bool FaceLookTarget(TaskPoint tp)
    {
        var t = rotateRoot ? rotateRoot : transform;

        Vector3 targetPos = tp.lookAtTarget ? tp.lookAtTarget.position : (t.position + t.forward);
        Vector3 dir = targetPos - t.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return true;

        Quaternion goal = Quaternion.LookRotation(dir);
        t.rotation = Quaternion.RotateTowards(t.rotation, goal, faceTurnSpeed * Time.deltaTime);

        float threshold = Mathf.Max(0f, tp.faceAngleThreshold) + Mathf.Max(0f, faceSlackDegrees);
        float angle = Vector3.Angle(t.forward, dir);
        return angle <= threshold;
    }
}
