using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;

    private void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        SceneManager.LoadScene("MainScene");
    }

        private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        SceneManager.LoadScene("MainScene");
    }

}
