using UnityEngine;

public class WorkSlotGizmos : MonoBehaviour
{
    public Transform slot0, slot1, slot2;
    public float size = 0.12f;

    void OnDrawGizmos()
    {
        Draw(slot0, Color.cyan);
        Draw(slot1, Color.cyan);
        Draw(slot2, Color.cyan);
    }
    void Draw(Transform t, Color c)
    {
        if (!t) return;
        Gizmos.color = c;
        Gizmos.DrawWireCube(t.position, Vector3.one * size);
        
        Gizmos.DrawRay(t.position, t.forward * (size * 1.2f));
    }
}
