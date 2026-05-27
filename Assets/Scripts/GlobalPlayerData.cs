// FOR FUTURE CHARACTER CUSTOMIZATION USE
// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;

// // Simple container for player data
// public class PlayerData
// {
//     public string Role;
//     public string PlayerName;
//     // Add other data like skin, customization, etc.
// }

// // Singleton global player data manager
// public class GlobalPlayerData : MonoBehaviour
// {
//     public static GlobalPlayerData Instance { get; private set; }

//     // Store player data by client ID
//     private Dictionary<ulong, PlayerData> clientPlayerData = new Dictionary<ulong, PlayerData>();

//     void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     // Set or update player data for a client
//     public void SetPlayerData(ulong clientId, PlayerData data)
//     {
//         clientPlayerData[clientId] = data;
//     }

//     // Get player data for a client, or null if none exists
//     public PlayerData GetPlayerData(ulong clientId)
//     {
//         clientPlayerData.TryGetValue(clientId, out PlayerData data);
//         return data;
//     }
// }
