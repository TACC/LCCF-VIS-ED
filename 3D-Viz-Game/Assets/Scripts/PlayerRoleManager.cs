using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // text mesh supp

//using UnityEngine.InputSystem; // for manually testing inputs

public class PlayerRoleManager : NetworkBehaviour
{
    string[] roles = {"Resturant","Cashier", "Kitchen", "Stocker", "Dishwasher" };
    private string assignedRole;

    public Camera playerCam; 
    public TextMeshProUGUI roleText;
    public UnityEngine.UI.Button scoreButton; // test - delete later
    public Text scoreText;


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

        // assign role based on join order
        int roleIndex = (int)OwnerClientId % roles.Length;
        assignedRole = roles[roleIndex];

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


        //logic as to how to spawn different UI elements on different roles
        // note to self - this logic will be used for cashier making a ticket and handing it over to kitchen
        GameObject buttonObj = GameObject.Find("ScoreButton");
        GameObject score = GameObject.Find("ScoreText");

        if (buttonObj != null && score != null)
        {
            scoreButton = buttonObj.GetComponent<Button>();
            scoreText = score.GetComponent<Text>();


            scoreButton.gameObject.SetActive(false); // everyone gets a button
            scoreText.gameObject.SetActive(false);


            // if (assignedRole == "Cashier")
            // {
            //     Debug.Log("After checking assigned role");
            //     // only enabling score on the cashier's screen

            //     scoreText.gameObject.SetActive(true);
            //     //scoreButton.gameObject.SetActive(false);
            // }
        }

        // connecting client to matching player model
        
    }

}


