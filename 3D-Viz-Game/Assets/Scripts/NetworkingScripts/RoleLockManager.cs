using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System;
using System.Collections.Generic;

public struct LockedRoleData : IEquatable<LockedRoleData>
{
    public int ClientId;
    public string Role;

    public bool Equals(LockedRoleData other)
    {
        return ClientId == other.ClientId && Role == other.Role;
    }

    public override bool Equals(object obj)
    {
        return obj is LockedRoleData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, Role);
    }
}

// manages soft and hard locking of roles in a multiplayer game.
// ensures players cannot select the same role simultaneously.
public class RoleLockManager : NetworkBehaviour
{
    public static RoleLockManager Instance { get; private set; }
    // locked by clients
    public readonly SyncList<LockedRoleData> SoftLockedRoles = new SyncList<LockedRoleData>();
    // locked by server
    public readonly SyncList<string> HardLockedRoles = new SyncList<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;
    }

    // checks if a role is currently taken by another client or hard-locked.
    // returns true if the role is already locked, false otherwise.
    public bool IsRoleTaken(string role, int requestingClientId = int.MaxValue)
    {
        // check if role is hardlocked
        foreach (var r in HardLockedRoles)
        {
            if (r == role)
                return true;
        }

        // then check if it's soft locked by another player
        foreach (var r in SoftLockedRoles)
        {
            if (r.Role == role && r.ClientId != requestingClientId)
                return true;
        }

        return false;
    }

    /* server-only method to request a soft lock on a role for a specific client.
        removes any previous soft lock by the same client before applying the new one,
        so we avoid one client soft locking multiple roles */
    [ServerRpc(RequireOwnership = false)]
    public void SoftLockRoleServerRpc(int clientId, string role)
    {
        // removes existing softlocks
        for (int i = SoftLockedRoles.Count - 1; i >= 0; i--)
        {
            if (SoftLockedRoles[i].ClientId == clientId)
                SoftLockedRoles.RemoveAt(i);
        }

        // don't softlock if already hard locked
        if (HardLockedRoles.Contains(role))
            return;

        // soft lock role
        SoftLockedRoles.Add(new LockedRoleData
        {
            ClientId = clientId,
            Role = role
        });

        Debug.Log($"[Server] Soft lock by client {clientId} on role {role}");
    }

    /*  apply hardlock to role and removes any softlocks it has*/
    [ServerRpc(RequireOwnership = false)]
    public void HardLockRoleServerRpc(string role)
    {
        // prevent double hard lock
        if (HardLockedRoles.Contains(role))
            return;

        // add hardlock
        HardLockedRoles.Add(role);
        Debug.Log($"[Server] Hard lock role {role}");

        // remove softlock
        for (int i = SoftLockedRoles.Count - 1; i >= 0; i--)
        {
            if (SoftLockedRoles[i].Role == role)
            {
                SoftLockedRoles.RemoveAt(i);
            }
        }
    }

    // removes all soft locks associated with a client incase they disconnect
    // or incase of failures
    [ServerRpc(RequireOwnership = false)]
    public void UnlockSoftLockServerRpc(int clientId)
    {
        for (int i = SoftLockedRoles.Count - 1; i >= 0; i--)
        {
            if (SoftLockedRoles[i].ClientId == clientId)
            {
                Debug.Log($"[Server] Removing soft lock by client {clientId} on role {SoftLockedRoles[i].Role}");
                SoftLockedRoles.RemoveAt(i);
            }
        }
    }
}
