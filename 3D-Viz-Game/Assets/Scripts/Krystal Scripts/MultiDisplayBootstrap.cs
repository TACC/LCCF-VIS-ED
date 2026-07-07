using UnityEngine;

public class MultiDisplayBootstrap : MonoBehaviour
{
    [Tooltip("Activate up to 4 displays (0..3). Display 0 is always active.")]
    public int targetDisplayCount = 4;

    private static bool _done;

    private void Awake()
    {
        if (_done) return;
        _done = true;

        int available = Display.displays.Length;
        Debug.Log($"[MultiDisplayBootstrap] Displays available: {available}");

        int toActivate = Mathf.Clamp(targetDisplayCount, 1, available);

        for (int i = 1; i < toActivate; i++)
        {
            Debug.Log($"[MultiDisplayBootstrap] Activating display {i}");
            Display.displays[i].Activate();
        }
    }
}