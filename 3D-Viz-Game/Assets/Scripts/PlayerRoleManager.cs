using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Collections; // text mesh supp

public class PlayerRoleManager : NetworkBehaviour
{
    private string assignedRole;

    public Camera playerCam;
    public TextMeshProUGUI roleText;
    public Text scoreText;

    private NetworkVariable<FixedString64Bytes> networkRole = new NetworkVariable<FixedString64Bytes>();


    // this runs every time a player is spawned in multiplayer
    public override void OnNetworkSpawn()
    {
        // hide UI and camera for remote players so they won't see eachother's on screen things
        if (!IsLocalPlayer)
        {
            if (roleText != null) roleText.gameObject.SetActive(false);
            if (playerCam != null) playerCam.enabled = false;
            return;
        }

        // assign main screen to show resturant
        if (IsServer && IsOwner && networkRole.Value.IsEmpty)
        {
            SetRoleServerRpc("Resturant");
        }
        
        // assign role based on UI choice
        networkRole.OnValueChanged += (oldValue, newValue) =>
        {
            ApplyRole(newValue.ToString());
        };

        if (!string.IsNullOrEmpty(networkRole.Value.ToString()))
        {
            ApplyRole(networkRole.Value.ToString());
        }
    }

private void ApplyRole(string assignedRole)
    {
        Debug.Log($"[Client {OwnerClientId}] Applying role: {assignedRole}");

        // finds the cam in the hierarchy, and then enables it for the client
        Camera roleCam = GameObject.Find($"{assignedRole}Cam")?.GetComponent<Camera>();
        if (roleCam != null)
        {
            playerCam.enabled = false; // disable the prefab's built-in cam
            roleCam.enabled = true;

            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.worldCamera = roleCam;
        }

        // display role name - temporary for testing
        if (roleText != null)
        {
            roleText.text = assignedRole.ToUpper();
        }
        Debug.Log($"[Client {OwnerClientId}] Spawned as {assignedRole}");

    }

    // method runs on the server but is called by clients
    // sets the role
    [ServerRpc]
    public void SetRoleServerRpc(string chosenRole)
    {
        networkRole.Value = chosenRole;
    }

}


