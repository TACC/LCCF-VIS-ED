using UnityEngine;

[DisallowMultipleComponent]
public class StationDisplayBinder : MonoBehaviour
{
    [Header("Station Mapping")]
    [Range(0, 4)]
    public int targetDisplay = 0;

    [Header("References")]
    public Camera stationCamera;
    public Canvas stationCanvas;

    private void Reset()
    {
        stationCamera = GetComponentInChildren<Camera>(true);
        stationCanvas = GetComponentInChildren<Canvas>(true);
    }

    private void Awake()
    {
        Apply();
    }

    [ContextMenu("Apply Binding")]
    public void Apply()
    {
        if (stationCamera == null)
        {
            Debug.LogError($"[StationDisplayBinder] Missing stationCamera on {name}");
            return;
        }

        stationCamera.targetDisplay = targetDisplay;

        if (stationCanvas != null)
        {
            stationCanvas.targetDisplay = targetDisplay;

            // Screen Space - Camera so this canvas is tied to THIS station camera.
            if (stationCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                stationCanvas.worldCamera = stationCamera;
            }
        }

        Debug.Log($"[StationDisplayBinder] {name} bound to Display {targetDisplay}");
    }
}