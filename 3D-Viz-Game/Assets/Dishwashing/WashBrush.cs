using UnityEngine;

public class WashBrush : MonoBehaviour
{
    public bool IsScrubbing { get; private set; }

    public void UpdateFromPointer(
        Camera cam,
        LayerMask plateLayer,
        float surfaceOffset,
        float rayMaxDistance,
        Vector2 screenPos,
        bool pressed)
    {
        IsScrubbing = pressed;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, rayMaxDistance, plateLayer))
        {
            transform.position = hit.point + hit.normal * surfaceOffset;
            transform.rotation = Quaternion.LookRotation(hit.normal);
        }
    }
}
