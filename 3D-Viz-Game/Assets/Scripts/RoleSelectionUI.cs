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
        RoleLockManager.Instance.SoftLockedRoles.OnListChanged += (changeEvent) => RefreshRoleButtons();
        RoleLockManager.Instance.HardLockedRoles.OnListChanged += (changeEvent) => RefreshRoleButtons();
    }

    void OnRoleSelected(string role)
    {
        selectedRole = role;
        continueButton.interactable = true;

        RoleLockManager.Instance.SoftLockRoleServerRpc(NetworkManager.Singleton.LocalClientId, role);
        RefreshRoleButtons();
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
            if (RoleLockManager.Instance.IsRoleTaken(selectedRole, NetworkManager.Singleton.LocalClientId))
            {
                Debug.LogWarning($"Role {selectedRole} is already taken");
                return;
            }
            localPlayer.SetRoleServerRpc(selectedRole);
            gameObject.SetActive(false); // hide role selection UI
        }
    }

    void RefreshRoleButtons()
    {
        foreach (Button b in roleButtons)
        {
            string roleName = b.name;
            bool isHardLocked = false;
            bool isSoftLocked = false;

            foreach (var v in RoleLockManager.Instance.HardLockedRoles)
            {
                if (v.ToString() == roleName)
                {
                    isHardLocked = true;
                    break;
                }
            }

            foreach (var r in RoleLockManager.Instance.SoftLockedRoles)
            {
                if (r.Role.ToString() == roleName)
                {
                    // let selection be interactable
                    if (r.ClientId == NetworkManager.Singleton.LocalClientId && roleName == selectedRole)
                    {
                        isSoftLocked = false;
                    }
                    else
                    {
                        isSoftLocked = true;
                    }

                    break;
                }
            }

            b.interactable = !(isHardLocked || isSoftLocked);
            ColorBlock cb = b.colors;
            cb.normalColor = (roleName == selectedRole) ? Color.gray : (isHardLocked || isSoftLocked) ? Color.gray : Color.white;
            b.colors = cb;
        }
    }

}
