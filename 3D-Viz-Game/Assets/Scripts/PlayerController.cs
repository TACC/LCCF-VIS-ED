using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Component.Transforming;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerController : NetworkBehaviour
{

    private Rigidbody rb;
    private float movementX;
    private float movementY;
    public float speed = 10;
    public TextMeshProUGUI countText;

    public GameObject guiPrefab;

    /* Instatiate network objects that we want synced between the server and client
    Will be edited but overall logic for setting up UI for each station*/
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        rb = GetComponent<Rigidbody>();

        if (base.Owner.IsLocalClient && !IsServerInitialized)
        {
            Debug.Log("Client spawning GUI!");
            // create a copy of the prefab and finds GUIInputPanel
            GameObject guiInstance = Instantiate(guiPrefab);
            GUIInputPanel panel = guiInstance.GetComponentInChildren<GUIInputPanel>();
            panel.Init(this);
        }
    }

    /*Player movement stuff. Doesn't work for now.*/
    private void FixedUpdate()
    {
        if (!IsServerInitialized) return;

        Vector3 movementVector = new Vector3(movementX, 0.0f, movementY);
        rb.linearVelocity = movementVector.normalized * speed;

        movementX = 0;
        movementY = 0;
    }


   /*Player movement networking for grabbing what button was pressed*/
    public void OnMove(InputValue value)
    {
        if (!IsOwner || IsServerInitialized) return;

        Vector2 movement = value.Get<Vector2>();
        SubmitMovement(direction: movement);
    }

    /*Player movement networking, sending movement to server*/
    public void OnMoveFromButtons(Vector2 direction)
    {
        Debug.Log("Button pressed with direction: " + direction);

        if (!IsClientInitialized || IsServerInitialized) return;

        SubmitMovement(direction);
    }


    // sends movement to server
    [ServerRpc]
    void SubmitMovement(Vector2 direction)
    {
        StartCoroutine(ApplyMovement(direction));
    }

    private IEnumerator ApplyMovement(Vector2 direction)
    {
        // how long the character moves
        float duration = 0.2f;
        float timer = 0f;

        while (timer < duration)
        {
            Vector3 movementVector = new Vector3(direction.x, 0.0f, direction.y);
            rb.AddForce(movementVector * speed);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

}