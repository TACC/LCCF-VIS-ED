using UnityEngine;

public class BrushDebugHUD : MonoBehaviour
{
    void OnGUI()
    {
        var bs = BrushManager.ActiveBrushes;
        int n = bs?.Count ?? 0;
        string s = $"Brushes: {n}\n";
        for (int i = 0; i < n; i++)
            if (bs[i]) s += $"  [{i}] scrubbing={bs[i].IsScrubbing} pos={bs[i].transform.position}\n";
        GUI.Label(new Rect(10,10,500,200), s);
    }
}
