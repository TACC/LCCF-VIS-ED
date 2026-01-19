using UnityEngine;
using TMPro;

public class WaitingUIController : MonoBehaviour
{
    public static WaitingUIController Instance;

    public TextMeshProUGUI waitingText;

    private void Awake()
    {
        Debug.Log("[WaitingUIController] Awake called.");
        Instance = this;
    }

    public void UpdateWaitingText(int count)
    {
        if (waitingText != null)
            waitingText.text = $"Waiting for players... ({count}/4 joined)";
    }
}
