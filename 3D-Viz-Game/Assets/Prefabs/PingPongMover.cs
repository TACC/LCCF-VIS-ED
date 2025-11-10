using UnityEngine;
using System.Collections;

public class PingPongMover : MonoBehaviour
{
    public Transform target;           // assign your Character root (same object with SimpleBobber)
    public SimpleBobber bobber;        // assign the same SimpleBobber on the root
    public Vector3 direction = Vector3.forward;
    public float distance = 1f;        // how far to move each way
    public float moveTime = 1.2f;      // time to move in one direction
    public float waitTime = 3f;        // pause at each end
    public bool useLocal = false;      // move in local or world space

    Vector3 a, b;

    IEnumerator Start()
    {
        if (!target) target = transform;
        Vector3 start = useLocal ? target.localPosition : target.position;
        Vector3 d = (useLocal ? target.TransformDirection(direction) : direction).normalized * distance;

        a = start;
        b = start + d;

        while (true)
        {
            yield return Move(a, b);  yield return new WaitForSeconds(waitTime);
            yield return Move(b, a);  yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator Move(Vector3 from, Vector3 to)
    {
        if (bobber) bobber.SetMoving(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, moveTime);
            float s = Mathf.SmoothStep(0f, 1f, t);           // ease in/out
            if (useLocal) target.localPosition = Vector3.LerpUnclamped(from, to, s);
            else          target.position      = Vector3.LerpUnclamped(from, to, s);
            yield return null;
        }
        if (bobber) bobber.SetMoving(false);
    }
}
