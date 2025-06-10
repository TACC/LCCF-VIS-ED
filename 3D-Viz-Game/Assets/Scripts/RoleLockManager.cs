using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;

public struct LockedRoleData : INetworkSerializable, IEquatable<LockedRoleData>
{
    public ulong ClientId;
    public FixedString64Bytes Role;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Role);
    }

    public bool Equals(LockedRoleData other)
    {
        return ClientId == other.ClientId && Role.Equals(other.Role);
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

public class RoleLockManager : NetworkBehaviour
{
    public static RoleLockManager Instance { get; private set; }
    public NetworkList<LockedRoleData> SoftLockedRoles;
    public NetworkList<FixedString64Bytes> HardLockedRoles;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SoftLockedRoles = new NetworkList<LockedRoleData>();
            HardLockedRoles = new NetworkList<FixedString64Bytes>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public bool IsRoleTaken(string role, ulong requestingClientId = ulong.MaxValue)
    {
        foreach (var r in HardLockedRoles)
        {
            if (r.ToString() == role)
            {
                return true;
            }
        }

        foreach (var r in SoftLockedRoles)
        {
            if (r.Role.ToString() == role)
            {
                if (r.ClientId == requestingClientId)
                continue;

                return true;
            }
        }

        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SoftLockRoleServerRpc(ulong clientId, string role)
    {
        for (int i = SoftLockedRoles.Count - 1; i >= 0; i--)
        {
            if (SoftLockedRoles[i].ClientId == clientId)
            {
                SoftLockedRoles.RemoveAt(i);
            }
        }

        // prevent softlocking a hard locked role
        foreach (var r in HardLockedRoles)
        {
            if (r.ToString() == role)
                return;
        }

        SoftLockedRoles.Add(new LockedRoleData
        {
            ClientId = clientId,
            Role = role
        });

        Debug.Log($"[Server] Soft lock by client {clientId} on role {role}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void HardLockRoleServerRpc(string role)
    {
        bool alreadyLocked = false;
        foreach (var r in HardLockedRoles)
        {
            if (r.ToString() == role)
            {
                alreadyLocked = true;
                break;
            }
        }

        if (!alreadyLocked)
        {
            HardLockedRoles.Add(role);
            Debug.Log($"[Server] Hard lock role {role}");
        }

        // clean up softlocks
        for (int i = SoftLockedRoles.Count - 1; i >= 0; i--)
        {
            if (SoftLockedRoles[i].Role.ToString() == role)
            {
                SoftLockedRoles.RemoveAt(i);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnlockSoftLockServerRpc(ulong clientId)
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