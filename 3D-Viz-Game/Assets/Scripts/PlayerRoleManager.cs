using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections.Generic;

public class PlayerRoleManager : NetworkBehaviour
{
    private NetworkVariable<FixedString64Bytes> networkRole = new NetworkVariable<FixedString64Bytes>();
    // foro future customization
    //private NetworkVariable<FixedString64Bytes> characterName = new NetworkVariable<FixedString64Bytes>();
    //private NetworkVariable<FixedString64Bytes> skinColor = new NetworkVariable<FixedString64Bytes>();

    public Camera playerCam;
    public TextMeshProUGUI roleText;

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

        if (!networkRole.Value.IsEmpty)
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
        if (RoleLockManager.Instance.IsRoleTaken(chosenRole))
        {
            Debug.LogWarning($"Role {chosenRole} is already taken");
            return;
        }

        //set the values that we want saved and shared
        networkRole.Value = chosenRole;
        RoleLockManager.Instance.MarkRoleTaken(chosenRole);

        Debug.Log($"Player {OwnerClientId} assigned role {chosenRole}");
    }
    
}


