using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{

    private Rigidbody rb;
    private float movementX;
    private float movementY;
    public float speed = 10;
    public TextMeshProUGUI countText;

    public GameObject guiPrefab;

    private NetworkVariable<int> count = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        if (IsServer && IsOwner)
        {
            Destroy(gameObject);
            return;
        }
        rb = GetComponent<Rigidbody>();

        if (!IsServer && IsOwner)
        {
            Debug.Log("Client spawning GUI!");
            GameObject guiInstance = Instantiate(guiPrefab);
            GUIInputPanel panel = guiInstance.GetComponentInChildren<GUIInputPanel>();
            Debug.Log("" + this);
            panel.Init(this);
        }

        count.OnValueChanged += (oldValue, newValue) =>
        {
            if (countText != null)
                countText.text = "Count: " + newValue.ToString();
        };
    }


    private void FixedUpdate()
    {
        if (!IsServer) return;

        Vector3 movementVector = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movementVector * speed);
        movementX = 0;
        movementY = 0;
    }

    void OnTriggerEnter(Collider c)
    {
        if (!IsServer) return;

        if (c.gameObject.CompareTag("Pickup"))
        {
            c.gameObject.SetActive(false);
            count.Value += 1;
            // SetCountText();
        }
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner || IsServer) return;

        Vector2 movement = value.Get<Vector2>();
        SubmitMovementServerRpc(movement);
    }

    public void OnMoveFromButtons(Vector2 direction)
    {
        Debug.Log("Button pressed with direction: " + direction);

        if (!IsClient || IsServer) return;

        SubmitMovementServerRpc(direction);
    }


    [ServerRpc(RequireOwnership = false)]
    void SubmitMovementServerRpc(Vector2 direction)
    {
        StartCoroutine(ApplyMovement(direction));
    }

    private IEnumerator ApplyMovement(Vector2 direction)
    {
        float duration = 0.2f; // how long the character moves
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