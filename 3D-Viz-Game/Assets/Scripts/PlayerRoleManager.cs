using FishNet.Object;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections.Generic;

public class PlayerRoleManager : NetworkBehaviour
{
    private string networkRole = "";
    public Camera playerCam;
    public TextMeshProUGUI roleText;

    // this runs every time a player is spawned in multiplayer
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (!base.Owner.IsLocalClient || base.Owner.ClientId == 0)
        {
            if (playerCam != null) playerCam.enabled = false;
            if (roleText != null) roleText.gameObject.SetActive(false);
            return;
        }

        if (base.Owner.IsLocalClient && Owner.ClientId == 0)
        {
            Debug.Log("Host running with Resturant camera.");
            SetRoleServerRpc("Resturant");
            return;
        }

        if (base.Owner.IsLocalClient && string.IsNullOrEmpty(networkRole))
        {
            Debug.Log("Client waiting for role selection.");
        }
    }

    
    [ObserversRpc]
    private void SyncRoleToClient(string role)
    {
        ApplyRole(role);
    }

    private void ApplyRole(string assignedRole)
    {
        if (!IsOwner)
            return;
        Debug.Log($"[Client {base.Owner.ClientId}] Applying role: {assignedRole}");

        //finds the cam in the hierarchy, and then enables it for the client
        Camera roleCam = GameObject.Find($"{assignedRole}Cam")?.GetComponent<Camera>();
        if (roleCam != null)
        {
            playerCam.enabled = false; //disable the prefab's built-in cam
            roleCam.enabled = true;
            
            Canvas canvas = roleCam.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                canvas.worldCamera = roleCam;
            }
        }

        //disable role select background
        GameObject bgCanvas = GameObject.Find("RoleSelectionUI-background");
        if (bgCanvas != null)
        {
            bgCanvas.SetActive(false);
        }

        //display role name - temporary for testing
        if (roleText != null)
        {
            roleText.text = assignedRole.ToUpper();
        }
        Debug.Log($"[Client {base.Owner.ClientId}] Spawned as {assignedRole}");

    }

    //method runs on the server but is called by clients
    //sets the role
    [ServerRpc]
    public void SetRoleServerRpc(string chosenRole)
    {
        if (RoleLockManager.Instance.IsRoleTaken(chosenRole, base.Owner.ClientId))
        {
            Debug.LogWarning($"Role {chosenRole} is already taken");
            return;
        }

        //set the values that we want saved and shared
        networkRole = chosenRole;
        if (base.Owner.ClientId != 0)
        {
            RoleLockManager.Instance.HardLockRoleServerRpc(chosenRole);
        }
        //RoleLockManager.Instance.HardLockRoleServerRpc(chosenRole);
        SyncRoleToClient(chosenRole); //double check this

        Debug.Log($"Player {base.Owner.ClientId} assigned role {chosenRole}");
    }

}


