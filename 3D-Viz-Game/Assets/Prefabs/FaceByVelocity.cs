using UnityEngine;

public class FaceByVelocity : MonoBehaviour
{
    public Transform target;                 // usually the same root you move
    [Tooltip("Degrees per second to turn")]
    public float rotationSpeed = 540f;       // 360–720 feels good
    [Tooltip("Ignore tiny jitters below this speed (m/s)")]
    public float minSpeed = 0.02f;
    [Tooltip("If your model’s front isn’t +Z, add a yaw offset (e.g., 180).")]
    public float yawOffsetDegrees = 0f;

    Vector3 _prevPos;

    void Awake()
    {
        if (!target) target = transform;
        _prevPos = target.position;
    }

    void Update()
    {
        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        Vector3 vel = (target.position - _prevPos) / dt;
        _prevPos = target.position;

        // yaw-only facing
        vel.y = 0f;
        if (vel.sqrMagnitude < minSpeed * minSpeed) return;

        Vector3 dir = vel.normalized;
        Quaternion want = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, yawOffsetDegrees, 0f);
        target.rotation = Quaternion.RotateTowards(target.rotation, want, rotationSpeed * dt);
    }
}
