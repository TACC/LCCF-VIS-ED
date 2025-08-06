using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class PlateScrubber3D : MonoBehaviour
{
    public Renderer dirtRenderer; 
    public int textureSize = 256;
    public float brushRadius = 20f;

    private Texture2D dirtTexture;
    private bool isScrubbing = false;

    void Start()
    {
        InitTexture();
    }

    void InitTexture()
    {
        var srcTex = dirtRenderer.material.GetTexture("_DirtTex") as Texture2D;

        dirtTexture = new Texture2D(srcTex.width, srcTex.height, srcTex.format, false);
        dirtTexture.SetPixels(srcTex.GetPixels());
        dirtTexture.Apply();

        dirtRenderer.material.SetTexture("_DirtTex", dirtTexture);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
            TryScrub(Input.mousePosition);
#else
        if (Input.touchCount > 0)
            TryScrub(Input.GetTouch(0).position);
#endif
    }

    void TryScrub(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject != gameObject) return;

            Vector2 uv = hit.textureCoord;

            int texX = Mathf.FloorToInt(uv.x * dirtTexture.width);
            int texY = Mathf.FloorToInt(uv.y * dirtTexture.height);

            int rad = Mathf.CeilToInt(brushRadius);
            for (int y = -rad; y <= rad; y++)
            {
                for (int x = -rad; x <= rad; x++)
                {
                    if (x * x + y * y <= brushRadius * brushRadius)
                    {
                        int px = Mathf.Clamp(texX + x, 0, dirtTexture.width - 1);
                        int py = Mathf.Clamp(texY + y, 0, dirtTexture.height - 1);
                        dirtTexture.SetPixel(px, py, Color.clear);
                    }
                }
            }

            dirtTexture.Apply();
            CheckIfClean();
        }
    }

    void CheckIfClean()
    {
        Color32[] pix = dirtTexture.GetPixels32();
        int dirtyCount = 0;
        foreach (var p in pix)
            if (p.a > 250)
                dirtyCount++;

        if (dirtyCount <= 0)
        {
            //Debug.Log("Plate fully clean (3D)");
            Destroy(gameObject);
        }
    }
}
