using UnityEngine;
using UnityEngine.UI;

namespace My.DemoScene
{

    /// <summary>
    /// Saves the character as a prefab with only the currently active clothing items.
    /// Attach this script to any GameObject in the scene.
    /// </summary>
    public class CharacterPrefabSaver : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The root GameObject of the character (e.g. Characters_mesh)")]
        public GameObject characterRoot;

        [Tooltip("Assign the save button here")]
        public Button saveButton;

        [Header("Save Settings")]
        [Tooltip("Prefix for the prefab name, e.g: Character")]
        public string prefixName = "Character";

        [Tooltip("Save folder path, must start with Assets/")]
        public string saveFolderPath = "Assets/SavedCharacters";

        private void Start()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(SavePrefab);
            else
                Debug.LogWarning("[CharacterPrefabSaver] Save button is not assigned!");
        }

        public void SavePrefab()
        {
    #if UNITY_EDITOR
            if (characterRoot == null)
            {
                Debug.LogError("[CharacterPrefabSaver] characterRoot is not assigned!");
                return;
            }

            CharacterPrefabSaverBridge.Save(characterRoot, prefixName, saveFolderPath);
    #else
            Debug.LogWarning("[CharacterPrefabSaver] This feature only works in the Unity Editor.");
    #endif
        }
    }
}