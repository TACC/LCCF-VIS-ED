using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using System.Collections.Generic;

public class ClientReadyRelay : NetworkBehaviour
{
    public static ClientReadyRelay Instance;

    private readonly HashSet<int> _readyClientIds = new HashSet<int>();

    private void Awake()
    {
        Instance = this;
    }

    // Called by each client once they reach the waiting screen.
    [ServerRpc(RequireOwnership = false)]
    public void NotifyReadyServerRpc(NetworkConnection conn = null)
    {
        if (conn == null)
            return;

        if (!_readyClientIds.Add(conn.ClientId))
            return;

        int readyCount = _readyClientIds.Count;

        Debug.Log($"[ClientReadyRelay] Client ready. readyCount={readyCount}");

        ObserversUpdateWaitingText(readyCount);

        if (readyCount >= 2)
            ObserversGoToRoleSelection();
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversUpdateWaitingText(int readyCount)
    {
        if (WaitingUIController.Instance != null)
            WaitingUIController.Instance.UpdateWaitingText(readyCount);
    }

    [ObserversRpc]
    private void ObserversGoToRoleSelection()
    {

        RoleSelectionUI.Instance.ShowRoleSelection();

        Debug.Log("[ClientReadyRelay] All players ready -> Go to role selection (implement here).");
    }
}
