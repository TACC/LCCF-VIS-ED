using UnityEngine;
using UnityEngine.AI;

public class CharacterAnimatorDriver : MonoBehaviour
{
    public Animator animator;
    public NavMeshAgent agent;                 // optional
    public Transform orderStation;
    public Transform cookingStation;
    public float arriveRadius = 0.35f;
    public float moveThreshold = 0.05f;

    private Vector3 lastPos;
    private enum Planned { None, Order, Cook }
    private Planned planned = Planned.None;

    void Reset() {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start() { lastPos = transform.position; }

    void Update()
    {
        // --- movement speed drives Idle/Moving ---
        float speed = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = transform.position;
        if (agent) speed = agent.velocity.magnitude;   // prefer agent if present

        animator.SetFloat("Speed", speed);

        // --- fire Order/Cook only when we ARRIVE at the target we planned ---
        if (planned == Planned.Order && Arrived(orderStation)) {
            animator.ResetTrigger("Cook");
            animator.SetTrigger("Order");
            planned = Planned.None;
        }
        else if (planned == Planned.Cook && Arrived(cookingStation)) {
            animator.ResetTrigger("Order");
            animator.SetTrigger("Cook");
            planned = Planned.None;
        }
    }

    bool Arrived(Transform t)
    {
        if (!t) return false;
        if (agent)
            return !agent.pathPending &&
                   agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveRadius);

        // non-NavMesh fallback
        var a = transform.position; a.y = 0;
        var b = t.position;        b.y = 0;
        return Vector3.Distance(a, b) <= arriveRadius;
    }

    // Call these from your input/AI when you choose a destination:
    public void GoToOrder()
    {
        planned = Planned.Order;
        if (agent && orderStation) agent.SetDestination(orderStation.position);
    }

    public void GoToCook()
    {
        planned = Planned.Cook;
        if (agent && cookingStation) agent.SetDestination(cookingStation.position);
    }

    // Animation Event at the end of Order/Cooking clips (if not using Exit Time)
    public void OnActionAnimationComplete() => animator.SetTrigger("ActionDone");
}
