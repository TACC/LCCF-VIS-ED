using UnityEngine;

namespace My.DemoScene
{
    public class DemoControls : MonoBehaviour
    {
        [Header("★ Cute Modular Character Pack – Demo Controls ★")]

        [Header("MOVEMENT")]
        [TextArea(1, 1)] public string movement = "W / A / S / D  →  Move character";

        [Header("CAMERA")]
        [TextArea(2, 2)] public string camera =
            "Scroll Wheel ↑  →  Zoom In  (closer to character)\n" +
            "Scroll Wheel ↓  →  Zoom Out (further from character)";

        [Header("WARDROBE")]
        [TextArea(4, 4)] public string wardrobe =
            "E                      →  Open / Close Wardrobe\n" +
            "Mid Click + Drag       →  Scroll character up / down in preview\n" +
            "Left Click  on item    →  Show / Hide that part\n" +
            "Right Click on item    →  Enable / Disable that part";

        [Header("ANIMATIONS")]
        [TextArea(7, 7)] public string animations =
            "1  →  Wave\n" +
            "2  →  Yes\n" +
            "3  →  No\n" +
            "4  →  Talk\n" +
            "5  →  Dance\n" +
            "6  →  Disappointed\n" +
            "7  →  Laugh";

        [Header("SAVE PREFAB")]
        [TextArea(3, 3)] public string savePrefab =
            "PREFABSAVER (Hierarchy)  →  Set your save path here\n" +
            "[ SAVE PREFAB ] button   →  Export character as Prefab instantly";
    }
}