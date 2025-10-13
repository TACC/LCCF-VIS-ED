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

    // this runs every time a player is spawned in multiplayer

    public override void OnStartClient()
    {
        base.OnStartClient();

        // if im not the owner
        if (!base.Owner.IsLocalClient)
        {
            playerCam.enabled = false;
            return;
        }

        // if im just a client
        playerCam.enabled = false;
        Debug.Log("Client waiting for role selection.");

    }



    [ObserversRpc]
    private void SyncRoleToClient(string role)
    {
        ApplyRole(role);
    }
    

private void ApplyRole(string assignedRole)
{
    if (!IsOwner) return;

    string[] allRoles = { "Cashier", "Kitchen", "Stocker", "Dishwasher" };

    foreach (string role in allRoles)
    {
        GameObject roleGO = GameObject.Find($"{role}Cam");

        if (roleGO != null)
        {
            bool shouldEnable = role == assignedRole;
            roleGO.SetActive(shouldEnable);

            Debug.Log($"{(shouldEnable ? "Enabled" : "Disabled")} {roleGO.name}");
        }
        else
        {
            Debug.LogWarning($"Could not find: {role}Cam");
        }
    }

    // Hide the selection background
    GameObject bgCanvas = GameObject.Find("RoleSelectionUI-background");
    if (bgCanvas != null)
        bgCanvas.SetActive(false);
}




    //method runs on the server but is called by clients
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
        // if (base.Owner.ClientId != 0)
        // {
        //     RoleLockManager.Instance.HardLockRoleServerRpc(chosenRole);
        // }
        RoleLockManager.Instance.HardLockRoleServerRpc(chosenRole);
        SyncRoleToClient(chosenRole);

        Debug.Log($"Player {base.Owner.ClientId} assigned role {chosenRole}");
    }

}


