using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance;

    public readonly SyncVar<int> connectedPlayers = new SyncVar<int>();
    public readonly SyncVar<int> readyPlayers = new SyncVar<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        connectedPlayers.Value = 0;
        readyPlayers.Value = 0;
    }



    [Server]
    public void PlayerJoined()
    {
        connectedPlayers.Value++;
        Debug.Log($"Players connected: {connectedPlayers.Value}");

        UpdateWaitingUI();

        if (connectedPlayers.Value == 1)
        {
            ShowRoleSelectionUI();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerPressedStartServerRpc()
    {
        readyPlayers.Value++;
        Debug.Log($"[NetworkGameManager] PlayerPressedStartServerRpc called. Total ready: {readyPlayers.Value}");
        UpdateWaitingUI();
        Debug.Log($"{readyPlayers.Value}/4 players ready");

        if (readyPlayers.Value == 1)
        {
            Debug.Log("[NetworkGameManager] 4 players ready. Starting role selection...");
            ShowRoleSelectionUI();
        }
    }

    [ObserversRpc]
    private void UpdateWaitingUI()
    {
        Debug.Log("Updating waiting UI on clients...");
        WaitingUIController.Instance?.UpdateWaitingText(readyPlayers.Value);
    }

    [ObserversRpc]
    private void ShowRoleSelectionUI()
    {
        Debug.Log("All players ready — showing role selection.");
        RoleSelectionUI.Instance.ShowRoleSelection();
    }

    // [ServerRpc(RequireOwnership = false)]
    // public void NotifyPlayerJoinedServerRpc()
    // {
    //     PlayerJoined();
    // }


}
