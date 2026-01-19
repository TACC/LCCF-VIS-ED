using UnityEngine;
using UnityEngine.UI;
using FishNet.Managing;
using System.Collections;
using FishNet.Object;
using TMPro;


public class RoleSelectionUI : MonoBehaviour
{
    public Button[] roleButtons;
    public Button continueButton;
    private string selectedRole = "";
    private PlayerRoleManager localPlayer;
    private NetworkManager networkManager;
    public TextMeshProUGUI stationText;


    // stuff for waiting screen->role select
    public static RoleSelectionUI Instance;
    public GameObject waitingScreen; // drag the WaitingScreenUI in inspector
    public GameObject roleSelectionScreen; // drag the RoleSelectionUI canvas here

    public GameObject roleSelectionBackground;

    public GameObject roleSelectionRoot;

    void Start()
    {
        waitingScreen.SetActive(true);
        //roleSelectionScreen.SetActive(false);
        foreach (Transform child in roleSelectionScreen.transform)
            child.gameObject.SetActive(false);
        StartCoroutine(WaitForClientIdAndHideIfHost());
    }

    private IEnumerator WaitForClientIdAndHideIfHost()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();

        // wait for the network to be fully initialized
        while (networkManager == null ||
               !networkManager.IsClientStarted ||
               networkManager.ClientManager.Connection == null)
        {
            yield return null;
        }

        //Debug.Log("Client detected — initializing RoleSelectionUI.");
        InitUI();
    }

    private void InitUI()
    {
        continueButton.interactable = false;
        continueButton.onClick.AddListener(OnContinueClicked);

        foreach (Button button in roleButtons)
        {
            string role = button.name;
            button.onClick.AddListener(() => OnRoleSelected(role));
        }

        StartCoroutine(WaitForRoleLockManagerThenInit());
    }

    private void OnRoleSelected(string role)
    {
        selectedRole = role;
        continueButton.interactable = true;

        if (RoleLockManager.Instance == null)
        {
            Debug.LogWarning("RoleLockManager is not yet initialized.");
            return;
        }

        int localClientId = networkManager.ClientManager.Connection.ClientId;
        RoleLockManager.Instance.SoftLockRoleServerRpc(localClientId, role);

        RefreshRoleButtons();
    }

    private void OnContinueClicked()
    {
        // Find the local player
        PlayerRoleManager[] players = FindObjectsByType<PlayerRoleManager>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.Owner.IsLocalClient)
            {
                localPlayer = player;
                break;
            }
        }

        if (localPlayer != null)
        {
            int localClientId = networkManager.ClientManager.Connection.ClientId;
            if (RoleLockManager.Instance.IsRoleTaken(selectedRole, localClientId))
            {
                Debug.LogWarning($"Role {selectedRole} is already taken");
                return;
            }

            localPlayer.SetRoleServerRpc(selectedRole);
            //gameObject.SetActive(false); // hide role selection UI

            // if (roleSelectionBackground != null)
            //     roleSelectionBackground.SetActive(false);
        }
    }

    private IEnumerator WaitForRoleLockManagerThenInit()
    {
        while (RoleLockManager.Instance == null)
            yield return null;

        RefreshRoleButtons();
    }

    private void RefreshRoleButtons()
    {
        foreach (Button b in roleButtons)
        {
            string roleName = b.name;
            bool isHardLocked = false;
            bool isSoftLocked = false;
            int localClientId = networkManager.ClientManager.Connection.ClientId;

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
                    if (r.ClientId == localClientId && roleName == selectedRole)
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

    void Awake()
    {
        Instance = this;
    }
    public void ShowRoleSelection()
    {
        waitingScreen.SetActive(false);
        roleSelectionScreen.SetActive(true);
        EnableAllChildren(roleSelectionScreen);
        StartCoroutine(WaitForClientIdAndHideIfHost());
    }

    private void EnableAllChildren(GameObject root)
    {
        root.SetActive(true);

        foreach (Transform child in root.transform)
        {
            child.gameObject.SetActive(true);
            EnableAllChildren(child.gameObject);
        }
    }

    public void HideRoleUI()
    {
        if (roleSelectionRoot != null)
            roleSelectionRoot.SetActive(false);

        if (roleSelectionBackground != null)
            roleSelectionBackground.SetActive(false);
    }
}
