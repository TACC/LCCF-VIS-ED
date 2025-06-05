using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class RoleSelectionUI : MonoBehaviour
{
    public Button[] roleButtons;
    public Button continueButton;
    private string selectedRole = "";
    private PlayerRoleManager localPlayer;

    void Start()
    {
        // do not show the pick station UI for main camera
        if (NetworkManager.Singleton.IsHost)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // continue button cannot be clicked until player pressed a role button
        continueButton.interactable = false;
        continueButton.onClick.AddListener(OnContinueClicked);

        foreach (Button button in roleButtons)
        {
            string role = button.name;
            button.onClick.AddListener(() => OnRoleSelected(role));
        }
    }

    void OnRoleSelected(string role)
    {
        selectedRole = role;
        continueButton.interactable = true;

        foreach (Button b in roleButtons)
        {
            ColorBlock cb = b.colors;
            cb.normalColor = (b.name == role) ? Color.gray : Color.white;
            b.colors = cb;
        }
    }

    void OnContinueClicked()
    {
        // Find the local player
        PlayerRoleManager[] players = FindObjectsByType<PlayerRoleManager>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.IsLocalPlayer)
            {
                localPlayer = player;
                break;
            }
        }

        if (localPlayer != null)
        {
            localPlayer.SetRoleServerRpc(selectedRole);
            gameObject.SetActive(false); // hide role selection UI
        }
    }

}
