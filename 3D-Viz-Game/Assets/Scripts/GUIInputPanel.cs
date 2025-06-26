using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;

public class GUIInputPanel : MonoBehaviour
{
    private Button upButton;
    private Button downButton;
    private Button leftButton;
    private Button rightButton;

    // know which player object to send input to when buttons are pressed
    private PlayerController localPlayer;

    public void Init(PlayerController player)
    {
        localPlayer = player;

        // find buttons in the hierarchy
        Button[] buttons = GetComponentsInChildren<Button>();
        upButton = buttons[0];
        leftButton = buttons[1];
        downButton = buttons[2];
        rightButton = buttons[3];

        // hook up button events to movement by calling method from PlayerController script
        upButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.up));
        downButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.down));
        leftButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.left));
        rightButton.onClick.AddListener(() => localPlayer.OnMoveFromButtons(Vector2.right));
    }
}