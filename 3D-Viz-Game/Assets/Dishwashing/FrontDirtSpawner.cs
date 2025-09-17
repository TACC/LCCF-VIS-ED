
using UnityEngine;
using UnityEngine.Rendering;

public class FrontDirtSpawner : MonoBehaviour
{
    public DirtSpot dirtPrefab;
    public Transform face;
    public int count = 10;
    public float radius = 0.13f;
    public Vector2 scaleRange = new Vector2(0.06f, 0.14f);
    public float surfaceOffset = 0.0015f;

    [Header("Raycast Surface")]
    public bool spawnRaycastSurface = true;
    public Vector2 surfaceSize = new Vector2(0.45f, 0.45f);
    public float raycastSurfaceOffset = 0.0025f;
    public Material invisibleMat;

    void Reset() { if (!face) face = transform; }

    public void Spawn()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "RaycastSurface")
                Destroy(child.gameObject);
        }

        if (spawnRaycastSurface)
            CreateRaycastSurface();

        for (int i = 0; i < count; i++)
        {
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

    void CreateRaycastSurface()
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surface.name = "RaycastSurface";

        surface.transform.SetParent(transform);
        surface.transform.position = face.position + face.forward * raycastSurfaceOffset;
        surface.transform.rotation = face.rotation;
        surface.transform.localScale = new Vector3(surfaceSize.x, surfaceSize.y, 1f);

        surface.layer = gameObject.layer;

        if (invisibleMat)
            surface.GetComponent<Renderer>().material = invisibleMat;
        else
            surface.GetComponent<Renderer>().enabled = false;

        Destroy(surface.GetComponent<MeshCollider>());
        var box = surface.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 1f, 0.0001f);
        box.isTrigger = false;
    }
}
