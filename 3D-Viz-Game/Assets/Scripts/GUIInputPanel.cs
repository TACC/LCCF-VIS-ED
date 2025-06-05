using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GUIInputPanel : MonoBehaviour
{
    private Button upButton;
    private Button downButton;
    private Button leftButton;
    private Button rightButton;

    private PlayerController localPlayer;

    public void Init(PlayerController player)
    {
        localPlayer = player;

        Button[] buttons = GetComponentsInChildren<Button>();
        upButton = buttons[0];
        leftButton = buttons[1];
        downButton = buttons[2];
        rightButton = buttons[3];

        // Hook up button events to movement
        upButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.up));
        downButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.down));
        leftButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.left));
        rightButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.right));
    }
}