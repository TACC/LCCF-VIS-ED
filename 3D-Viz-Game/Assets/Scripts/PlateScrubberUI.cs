using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class PlateScrubberUI : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Assign the RawImage component for this dirt layer")]
    public RawImage dirtLayer;

    [Header("Texture Settings")]
    public int textureSize = 256;
    public float brushRadius = 50f;

    private Texture2D dirtTexture;
    private bool isScrubbing = false;

    void Awake()
    {
        //auto-assign
        if (dirtLayer == null)
            dirtLayer = GetComponent<RawImage>();
    }

    void Start()
    {
        InitTexture();
    }

    void InitTexture()
    {
        //texture must have read/write enabled in the inspector
        var srcTex = dirtLayer.texture as Texture2D;
        if (srcTex == null || !srcTex.isReadable)
        {
            Debug.LogError("DirtOverlay.texture must be a readable Texture2D!");
            return;
        }

        dirtTexture = new Texture2D(srcTex.width, srcTex.height, srcTex.format, false);
        dirtTexture.SetPixels(srcTex.GetPixels());
        dirtTexture.Apply();
        dirtLayer.texture = dirtTexture;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isScrubbing = true;
        ScrubAt(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isScrubbing)
            ScrubAt(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isScrubbing = false;
    }

    void ScrubAt(PointerEventData eventData)
    {
        Canvas c = dirtLayer.canvas;
        Camera cam = (c.renderMode == RenderMode.ScreenSpaceCamera) ? c.worldCamera : null;

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                dirtLayer.rectTransform, eventData.position, cam))
            return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dirtLayer.rectTransform, eventData.position, cam, out local);

        Rect rect = dirtLayer.rectTransform.rect;
        //UV coords
        float u = (local.x - rect.xMin) / rect.width;
        float v = (local.y - rect.yMin) / rect.height;

        //texel coords using actual texture size
        int texX = Mathf.FloorToInt(u * dirtTexture.width);
        int texY = Mathf.FloorToInt(v * dirtTexture.height);

        //clear a little circle around the touch point
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

    void CheckIfClean()
    {
        // grab a fast snapshot of all texels
        Color32[] pix = dirtTexture.GetPixels32();
        int dirtyCount = 0;
        foreach (var p in pix)
            if (p.a > 10)  // use a tiny threshold to skip antialiasing 
                dirtyCount++;

        Debug.Log($"[CheckIfClean] remaining dirty pixels: {dirtyCount}");
        if (dirtyCount > 0)
            return;

        Debug.Log("✅ Plate fully clean — destroying!");
        // DESTROY THE WHOLE PLATE, not just the overlay:
        Destroy(transform.parent != null
                ? transform.parent.gameObject
                : gameObject);
    }
}
