using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject waitingPanel;

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (waitingPanel != null)
            waitingPanel.SetActive(false);
    }

    public void OnJoinGameButtonClicked()
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager not found.");
            return;
        }
        networkManager.ClientManager.OnClientConnectionState -= OnClientConnected;
        networkManager.ClientManager.OnClientConnectionState += OnClientConnected;
        networkManager.ClientManager.StartConnection();
    }

    private void OnClientConnected(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("Client connected successfully.");
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnected;

            // Show waiting room UI
            mainMenuPanel.SetActive(false);
            waitingPanel.SetActive(true);
            networkManager.StartCoroutine(WaitThenSendReadySignal());
        }
    }

    private IEnumerator WaitForGameManagerAndNotifyJoin()
    {
        while (NetworkGameManager.Instance == null)
            yield return null;

        NetworkGameManager.Instance.PlayerPressedStartServerRpc();
    }

    private IEnumerator WaitThenSendReadySignal()
    {
        // Wait for player object to spawn
        PlayerRoleManager localPlayer = null;
        while (localPlayer == null)
        {
            var players = FindObjectsByType<PlayerRoleManager>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.Owner.IsLocalClient)
                {
                    localPlayer = player;
                    break;
                }
            }

            yield return null;
        }

        Debug.Log("[MainMenuUI] Found local player. Sending ready signal to server.");
        // Wait until the relay exists and is spawned, then notify ready.
        while (ClientReadyRelay.Instance == null || !ClientReadyRelay.Instance.IsSpawned)
            yield return null;

        ClientReadyRelay.Instance.NotifyReadyServerRpc();
    }


    public void OnWaitingScreenStartButtonPressed()
    {
        networkManager.StartCoroutine(WaitThenSendReadySignal());
    }

    private void OnDisable()
    {
        if (networkManager != null)
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnected;
    }

    private void OnDestroy()
    {
        if (networkManager != null)
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnected;
    }

}
