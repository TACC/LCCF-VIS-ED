using UnityEngine;

public class TaskPoint : MonoBehaviour
{
    [Header("Where to stand")]
    [Tooltip("If null, the player will stand at this transform.")]
    public Transform approachPoint;            // optional custom approach position
    public float stopDistance = 0.35f;          // NavMeshAgent stopping distance override

    [Header("What to face")]
    [Tooltip("Usually the counter, stove, or workstation the player should face.")]
    public Transform lookAtTarget;              // target object to face
    [Tooltip("Degrees considered 'aligned' when facing the target.")]
    public float faceAngleThreshold = 5f;

    [Header("Facing options")]
    [Tooltip("If true, face the target's forward instead of its position.")]
    public bool matchLookTargetForward = true;
    [Tooltip("Extra clockwise (+) or counterclockwise (-) yaw when facing the target.")]
    public float additionalYaw = 90f;
    [Tooltip("If true, flips facing 180° (for stations that face opposite direction).")]
    public bool invertFacing180 = false;        // ✅ for dish or reversed stations

    [Header("Animation to play")]
    [Tooltip("Trigger name used in the player's Animator (must match Animator state name).")]
    public string animTrigger = "Order";        // ✅ required by PlayerNavAnimator

    [Header("Special options")]
    [Tooltip("If true, starts the knife Timeline loop when performing this task (only for kitchen).")]
    public bool enableKnife = false;            // ✅ true only on kitchen station

    // Computed approach position
    public Vector3 ApproachPosition => (approachPoint ? approachPoint.position : transform.position);

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        var p = ApproachPosition;
        Gizmos.DrawWireSphere(p, 0.08f);

        if (lookAtTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p, lookAtTarget.position);

            // facing arc preview
            Vector3 baseDir = matchLookTargetForward
                ? lookAtTarget.forward
                : (lookAtTarget.position - p);

            baseDir.y = 0f;

            if (invertFacing180)
                baseDir = -baseDir; // ✅ preview the 180° flip in Scene view

            if (baseDir.sqrMagnitude > 0.001f)
            {
                Vector3 fwd = Quaternion.Euler(0f, additionalYaw, 0f) * baseDir.normalized;
                Quaternion left = Quaternion.AngleAxis(-faceAngleThreshold, Vector3.up);
                Quaternion right = Quaternion.AngleAxis(+faceAngleThreshold, Vector3.up);
                Gizmos.DrawRay(p, left * fwd * 0.3f);
                Gizmos.DrawRay(p, right * fwd * 0.3f);
            }
        }
    }
}
