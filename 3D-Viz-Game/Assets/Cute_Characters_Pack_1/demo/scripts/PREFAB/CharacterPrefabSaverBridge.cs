#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace My.DemoScene
{

    /// <summary>
    /// Bridge between CharacterPrefabSaver and the Editor API.
    /// Do NOT place this file inside Assets/Editor/.
    /// The #if UNITY_EDITOR block handles editor-only compilation safely.
    /// </summary>
    public static class CharacterPrefabSaverBridge
    {
        public static void Save(GameObject characterRoot, string prefixName, string saveFolderPath)
        {
            // 1. Create the folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(saveFolderPath))
            {
                CreateFolderRecursive(saveFolderPath);
                AssetDatabase.Refresh();
            }

            // 2. Find the next available number
            int nextNumber = GetNextPrefabNumber(saveFolderPath, prefixName);

            // 3. Build the prefab name
            string prefabName = $"{prefixName}{nextNumber}_Prefab";
            string fullPath = $"{saveFolderPath}/{prefabName}.prefab";

            // 4. Create a deep copy of the character
            GameObject copy = Object.Instantiate(characterRoot);
            copy.name = prefabName;

            // 5. Reset position and rotation
            copy.transform.position = Vector3.zero;
            copy.transform.rotation = Quaternion.identity;
            copy.transform.localScale = characterRoot.transform.localScale;

            // 6. Remove all inactive children recursively
            RemoveInactiveChildren(copy.transform);

            // 7. Save as prefab first — with all components still intact.
            //    We clean components AFTER saving to avoid dependency errors.
            bool success = false;
            try
            {
                PrefabUtility.SaveAsPrefabAsset(copy, fullPath);
                success = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterPrefabSaver] Save error: {e.Message}");
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }

            if (success)
            {
                AssetDatabase.Refresh();

                // 8. Load prefab asset in isolation and strip unwanted components/objects.
                GameObject prefabAsset = PrefabUtility.LoadPrefabContents(fullPath);
                CleanupForPrefab(prefabAsset);
                PrefabUtility.SaveAsPrefabAsset(prefabAsset, fullPath);
                PrefabUtility.UnloadPrefabContents(prefabAsset);

                AssetDatabase.Refresh();
                Debug.Log($"[CharacterPrefabSaver] Prefab saved: {fullPath}");
            }
        }

        /// <summary>
        /// Removes unwanted components and named child objects from the prefab asset.
        /// </summary>
        private static void CleanupForPrefab(GameObject root)
        {
            // Scripts to remove by exact type name.
            // ThirdPersonController depends on CharacterController so it must go first.
            var scriptsToRemove = new HashSet<string>
            {
                "ThirdPersonController",
                "ThirdPersonCharacter",
                "StarterAssetsInputs",
                "CharacterAudio",
                "FootstepSounds",
                "CharacterPrefabSaver",  // remove the tool itself from the prefab
            };

            // Pass 1: remove all gameplay MonoBehaviours (including anything ThirdPerson-related)
            foreach (var comp in root.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                if (scriptsToRemove.Contains(typeName) || typeName.Contains("ThirdPerson"))
                    Object.DestroyImmediate(comp);
            }

            // Pass 2: now safe to remove CharacterController
            var cc = root.GetComponent<CharacterController>();
            if (cc != null) Object.DestroyImmediate(cc);

            // Animator — remove only if controller slot is empty
            var animator = root.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController == null)
                Object.DestroyImmediate(animator);

            // Remove named child objects and Point Lights recursively
            RemoveNamedAndTypedObjects(root.transform);
        }

        /// <summary>
        /// Recursively finds and destroys:
        /// - GameObjects named "CustomisationCamera" or "FootSteps"
        /// - GameObjects containing a Point Light component
        /// </summary>
        private static void RemoveNamedAndTypedObjects(Transform parent)
        {
            var toDestroy = new List<GameObject>();

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string childName = child.gameObject.name;

                bool shouldDestroy = false;

                if (childName == "CustomisationCamera" || childName == "FootSteps")
                    shouldDestroy = true;

                var light = child.GetComponent<Light>();
                if (light != null && light.type == LightType.Point)
                    shouldDestroy = true;

                if (shouldDestroy)
                    toDestroy.Add(child.gameObject);
                else
                    RemoveNamedAndTypedObjects(child);
            }

            foreach (var obj in toDestroy)
                Object.DestroyImmediate(obj);
        }

        /// <summary>
        /// Recursively removes all inactive children from the given transform.
        /// </summary>
        private static void RemoveInactiveChildren(Transform parent)
        {
            var toDestroy = new List<GameObject>();

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeSelf)
                    toDestroy.Add(child.gameObject);
                else
                    RemoveInactiveChildren(child);
            }

            foreach (var obj in toDestroy)
                Object.DestroyImmediate(obj);
        }

        /// <summary>
        /// Scans the folder for existing prefabs and returns the next number after the highest found.
        /// </summary>
        private static int GetNextPrefabNumber(string folderPath, string prefix)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            int maxNumber = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                if (fileName.StartsWith(prefix))
                {
                    string afterPrefix = fileName.Substring(prefix.Length);
                    int underscoreIndex = afterPrefix.IndexOf('_');
                    if (underscoreIndex > 0)
                    {
                        string numberStr = afterPrefix.Substring(0, underscoreIndex);
                        if (int.TryParse(numberStr, out int num))
                        {
                            if (num > maxNumber) maxNumber = num;
                        }
                    }
                }
            }

            return maxNumber + 1;
        }

        /// <summary>
        /// Creates nested folders step by step, e.g. "Assets/SavedCharacters/Sub".
        /// </summary>
        private static void CreateFolderRecursive(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif