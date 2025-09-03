using UnityEngine;

[ExecuteAlways]
public class MatchHudToCameraViewport : MonoBehaviour
{
    public Camera targetCamera;

    void LateUpdate()
    {
        if (!targetCamera) return;
        var r = targetCamera.pixelRect;
        var w = (float)Screen.width;
        var h = (float)Screen.height;

        // normalize camera viewport to [0..1] anchors
        var min = new Vector2(r.xMin / w, r.yMin / h);
        var max = new Vector2(r.xMax / w, r.yMax / h);

        var rt = transform as RectTransform;
        if (!rt) return;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
