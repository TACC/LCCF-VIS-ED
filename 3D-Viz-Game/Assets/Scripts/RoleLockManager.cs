using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

// global manager keeping track if the roles are taken in the start screen
public class RoleLockManager : MonoBehaviour
{
    public static RoleLockManager Instance { get; private set; }

    private HashSet<string> takenRoles = new HashSet<string>();

    private void Awake()
    {
        // make sure station name isn't destroyed when new scene is loaded
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

    }

    public bool IsRoleTaken(string role)
    {
        return takenRoles.Contains(role);
    }

    public void MarkRoleTaken(string role)
    {
        takenRoles.Add(role);
        Debug.Log($"Role locked: {role}");
    }

}