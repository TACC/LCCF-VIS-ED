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

        networkManager.ClientManager.StartConnection();

        networkManager.ClientManager.OnClientConnectionState += OnClientConnected;
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
            // Count this player as 'ready'
            StartCoroutine(WaitThenSendReadySignal());
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
        localPlayer.MarkPlayerReadyServerRpc();
    }


    public void OnWaitingScreenStartButtonPressed()
    {
        StartCoroutine(WaitThenSendReadySignal());
    }

}
