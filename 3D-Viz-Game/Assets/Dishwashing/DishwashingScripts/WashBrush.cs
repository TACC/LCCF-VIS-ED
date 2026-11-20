using UnityEngine;

public class WashBrush : MonoBehaviour
{
    public bool IsScrubbing { get; private set; }

    [Header("Visual")]
    public Transform visual;
    public float visualLift = 0.001f;

    //adding new brushes
    public Vector2 newBrushOffset { get; set; }

    // debug (optional)
    public bool LastHitPlate { get; private set; }
    public string LastHitName { get; private set; }

    public void UpdateFromPointer(
     Camera cam, PlateController plate, float surfaceOffset, Vector2 screenPos, bool pressed)
    {
        IsScrubbing = pressed;
        if (cam == null || plate == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane platePlane = plate.GetBrushPlane();

        if (platePlane.Raycast(ray, out float hitDistance))
        {
            Vector3 hitPoint = ray.GetPoint(hitDistance);
            Vector3 planeNormal = platePlane.normal;

            transform.position = hitPoint + planeNormal * surfaceOffset;
            transform.rotation = Quaternion.LookRotation(planeNormal);

            if (visual)
                visual.localPosition = new Vector3(0f, 0f, visualLift);

            LastHitPlate = true;
            LastHitName = plate.name;
        }
        else
        {
            LastHitPlate = false;
            LastHitName = null;
        }
    }

}
