using UnityEngine;

public class FrontDirtSpawner : MonoBehaviour
{
    public DirtSpot dirtPrefab;
    // front of the plate so dirt appears correctly
    public Transform face;
    public int count = 10;
    public float radius = 0.13f;
    public Vector2 scaleRange = new Vector2(0.06f, 0.14f);
    public float surfaceOffset = 0.0015f;

    void Reset() { if (!face) face = transform; }

    public void Spawn()
    {
        for (int i = 0; i < count; i++)
        {
            // random point in a disk on the front plane
            float a = Random.value * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Random.value) * radius;
            Vector3 local = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);

            Vector3 worldPos = face.TransformPoint(local) + face.forward * surfaceOffset;
            Quaternion worldRot = Quaternion.LookRotation(face.forward, face.up);

            var spot = Instantiate(dirtPrefab, worldPos, worldRot, transform);
            float s = Random.Range(scaleRange.x, scaleRange.y);
            spot.transform.localScale = new Vector3(s, s, s);
            spot.transform.Rotate(face.forward, Random.Range(0f, 360f), Space.World);
        }
    }
}
