// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.UI;

// public class ScoreManager : NetworkBehaviour
// {
//     public static ScoreManager Instance;
//     public Text scoreText;
//     // synced variable
//     private NetworkVariable<int> totalScore = new NetworkVariable<int>();

//     // only have one scoremanager in the scene
//     private void Awake()
//     {
//         if (Instance == null) Instance = this;
//     }

//     public override void OnNetworkSpawn()
//     {
//         // listens for changes to totalScore and then calls UpdateScoreDisplay
//         totalScore.OnValueChanged += UpdateScoreDisplay;
//         base.OnNetworkSpawn();
//         Debug.Log("[ScoreManager] NetworkSpawned");
//     }

//     private void UpdateScoreDisplay(int oldVal, int newVal)
//     {
//         if (scoreText != null)
//             scoreText.text = newVal.ToString();
//     }

//     [ServerRpc(RequireOwnership = false)]
//     public void AddScoreServerRPC()
//     {
//         totalScore.Value += 1;
//         Debug.Log("Score is now: " + totalScore.Value);
//     }
// }
