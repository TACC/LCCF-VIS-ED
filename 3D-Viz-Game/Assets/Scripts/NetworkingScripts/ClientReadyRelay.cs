using FishNet.Object;
using UnityEngine;

public class ClientReadyRelay : NetworkBehaviour
{
    public static ClientReadyRelay Instance;

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyReadyServerRpc()
    {
        Debug.Log("[ClientReadyRelay] Client is ready!");
        NetworkGameManager.Instance.PlayerPressedStartServerRpc();
    }
}
