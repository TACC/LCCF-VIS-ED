using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace My.DemoScene
{

    public class RandomOutfitButton : MonoBehaviour
    {
        public CameraToUI cameraToUI;
        public ParentableSwitcher parableSwitcher;
        public AccessorySlotManager accessorySlotManager;

        public GameObject triggerUI;
        public Transform costumeOnlyParent;

        [SerializeField] private float punchScale = 0.88f;
        [SerializeField] private float punchDuration = 0.08f;
        [SerializeField] private float returnDuration = 0.1f;

        private readonly Dictionary<GameObject, Vector3> _originalScales = new();

        private static readonly HashSet<string> CostumeExemptSetups = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "Expressions",
            "Color"
        };

        void Start()
        {
            if (cameraToUI == null) cameraToUI = FindAnyObjectByType<CameraToUI>();
            if (parableSwitcher == null) parableSwitcher = FindAnyObjectByType<ParentableSwitcher>();
            if (accessorySlotManager == null) accessorySlotManager = FindAnyObjectByType<AccessorySlotManager>();

            if (triggerUI != null)
            {
                RegisterScale(triggerUI);
                AddClickListener(triggerUI, () => StartCoroutine(PunchAndExecute(triggerUI, Randomize)));
            }
        }

        void Randomize()
        {
            bool costumeOnlyMode = false;

            if (cameraToUI != null)
            {
                RandomizeCameraToUI();

                if (costumeOnlyParent != null)
                {
                    foreach (var setup in cameraToUI.cameraSetups)
                    {
                        if (setup.costumeParent == costumeOnlyParent && !setup.isDisabled)
                        {
                            costumeOnlyMode = true;
                            break;
                        }
                    }
                }
            }

            if (costumeOnlyMode)
            {
                if (parableSwitcher != null) RandomizeParentableSwitcherExemptOnly();
                if (accessorySlotManager != null) DeactivateAllAccessorySlots();
            }
            else
            {
                if (accessorySlotManager != null) RandomizeAccessorySlots();
                if (parableSwitcher != null) RandomizeParentableSwitcher();
            }
        }

        void RandomizeCameraToUI()
        {
            var setups = cameraToUI.cameraSetups;
            var disabledByConflict = new bool[setups.Count];

            foreach (int i in ShuffledIndices(setups.Count))
            {
                var setup = setups[i];

                if (disabledByConflict[i]) { ApplyCameraSetupDisabled(setup); continue; }
                if (setup.costumeParent == null || setup.costumeParent.childCount == 0) continue;

                int index = PickRandomAvailable(setup.costumeParent);
                if (index < 0) { ApplyCameraSetupDisabled(setup); continue; }

                ApplyCameraSetupActive(setup, index);

                foreach (var entry in cameraToUI.conflictMap)
                {
                    if (entry.parent != setup.costumeParent) continue;
                    foreach (var conflict in entry.conflictsWith)
                    {
                        if (conflict == null) continue;
                        for (int j = 0; j < setups.Count; j++)
                            if (setups[j].costumeParent == conflict)
                                disabledByConflict[j] = true;
                    }
                }
            }
        }

        void ApplyCameraSetupActive(CameraToUI.CameraSetup setup, int index)
        {
            var parent = setup.costumeParent;
            if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);

            for (int i = 0; i < parent.childCount; i++)
                parent.GetChild(i).gameObject.SetActive(i == index);

            setup.currentCostumeIndex = index;
            setup.lastActiveIndex = index;
            setup.isDisabled = false;

            if (setup.disabledOverlay != null) setup.disabledOverlay.SetActive(false);
            if (setup.sourceCamera != null) setup.sourceCamera.enabled = true;
            if (setup.targetImage != null) setup.targetImage.enabled = true;
        }

        void ApplyCameraSetupDisabled(CameraToUI.CameraSetup setup)
        {
            var parent = setup.costumeParent;
            if (parent == null) return;

            if (setup.currentCostumeIndex >= 0 && setup.currentCostumeIndex < parent.childCount)
                parent.GetChild(setup.currentCostumeIndex).gameObject.SetActive(false);

            if (setup.currentCostumeIndex >= 0) setup.lastActiveIndex = setup.currentCostumeIndex;

            setup.currentCostumeIndex = -1;
            setup.isDisabled = true;
            parent.gameObject.SetActive(false);

            if (setup.disabledOverlay != null) setup.disabledOverlay.SetActive(true);
            if (setup.sourceCamera != null) setup.sourceCamera.enabled = false;
            if (setup.targetImage != null) setup.targetImage.enabled = false;
        }

        void RandomizeAccessorySlots()
        {
            var slots = accessorySlotManager.slots;

            foreach (var slot in slots)
            {
                if (slot.costumeParent == null) continue;

                if (slot.isActive && slot.currentIndex >= 0)
                {
                    slot.costumeParent.GetChild(slot.currentIndex).gameObject.SetActive(false);
                    cameraToUI?.UnblockIndex(slot.costumeParent, slot.currentIndex);
                }

                if (slot.currentIndex >= 0) slot.lastIndex = slot.currentIndex;
                slot.currentIndex = -1;
                slot.isActive = false;
                SetSlotButtonsAlpha(slot, false);
            }

            var usedTypes = new HashSet<string>();

            if (cameraToUI != null)
            {
                foreach (var setup in cameraToUI.cameraSetups)
                {
                    if (setup.isDisabled || setup.costumeParent == null || setup.currentCostumeIndex < 0) continue;
                    string t = GetPrefix(setup.costumeParent.GetChild(setup.currentCostumeIndex).name);
                    if (!string.IsNullOrEmpty(t)) usedTypes.Add(t);
                }
            }

            foreach (int si in ShuffledIndices(slots.Count))
            {
                var slot = slots[si];
                if (slot.costumeParent == null) continue;

                int childCount = slot.costumeParent.childCount;
                if (childCount == 0) continue;

                var available = new List<int>();
                for (int i = 0; i < childCount; i++)
                {
                    string t = GetPrefix(slot.costumeParent.GetChild(i).name);
                    if (!string.IsNullOrEmpty(t) && usedTypes.Contains(t)) continue;
                    available.Add(i);
                }

                if (available.Count == 0) continue;

                int chosen = available[Random.Range(0, available.Count)];
                string chosenType = GetPrefix(slot.costumeParent.GetChild(chosen).name);
                if (!string.IsNullOrEmpty(chosenType)) usedTypes.Add(chosenType);

                slot.costumeParent.GetChild(chosen).gameObject.SetActive(true);
                cameraToUI?.BlockIndex(slot.costumeParent, chosen);

                slot.currentIndex = chosen;
                slot.lastIndex = chosen;
                slot.isActive = true;

                SetSlotButtonsAlpha(slot, true);
            }
        }

        void DeactivateAllAccessorySlots()
        {
            foreach (var slot in accessorySlotManager.slots)
            {
                if (slot.costumeParent == null) continue;

                if (slot.isActive && slot.currentIndex >= 0)
                {
                    slot.costumeParent.GetChild(slot.currentIndex).gameObject.SetActive(false);
                    cameraToUI?.UnblockIndex(slot.costumeParent, slot.currentIndex);
                }

                if (slot.currentIndex >= 0) slot.lastIndex = slot.currentIndex;
                slot.currentIndex = -1;
                slot.isActive = false;
                SetSlotButtonsAlpha(slot, false);
            }
        }

        static string GetPrefix(string name)
        {
            int u = name.IndexOf('_');
            return u > 0 ? name.Substring(0, u).ToLowerInvariant() : "";
        }

        void SetSlotButtonsAlpha(AccessorySlotManager.SlotSetup slot, bool active)
        {
            float alpha = active ? 1f : slot.disabledAlpha;
            SetGraphicAlpha(slot.addButton, alpha);
            SetGraphicAlpha(slot.nextButton, alpha);
            SetGraphicAlpha(slot.previousButton, alpha);
        }

        void RandomizeParentableSwitcher()
        {
            foreach (var setup in parableSwitcher.setups)
            {
                if (setup.objectParent == null) continue;
                int childCount = setup.objectParent.childCount;
                if (childCount == 0) continue;

                int index = Random.Range(0, childCount);

                if (setup.currentIndex >= 0 && setup.currentIndex < childCount)
                    setup.objectParent.GetChild(setup.currentIndex).gameObject.SetActive(false);

                setup.objectParent.GetChild(index).gameObject.SetActive(true);
                setup.currentIndex = index;
                setup.lastActiveIndex = index;
                setup.isDisabled = false;

                if (setup.triggerUI != null)
                {
                    var cg = setup.triggerUI.GetComponent<CanvasGroup>() ?? setup.triggerUI.AddComponent<CanvasGroup>();
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }

        void RandomizeParentableSwitcherExemptOnly()
        {
            foreach (var setup in parableSwitcher.setups)
            {
                if (setup.objectParent == null) continue;
                int childCount = setup.objectParent.childCount;
                if (childCount == 0) continue;

                if (CostumeExemptSetups.Contains(setup.name))
                {
                    int index = Random.Range(0, childCount);

                    if (setup.currentIndex >= 0 && setup.currentIndex < childCount)
                        setup.objectParent.GetChild(setup.currentIndex).gameObject.SetActive(false);

                    setup.objectParent.GetChild(index).gameObject.SetActive(true);
                    setup.currentIndex = index;
                    setup.lastActiveIndex = index;
                    setup.isDisabled = false;

                    if (setup.triggerUI != null)
                    {
                        var cg = setup.triggerUI.GetComponent<CanvasGroup>() ?? setup.triggerUI.AddComponent<CanvasGroup>();
                        cg.alpha = 1f;
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }
                else
                {
                    if (setup.currentIndex >= 0 && setup.currentIndex < childCount)
                    {
                        setup.objectParent.GetChild(setup.currentIndex).gameObject.SetActive(false);
                        setup.lastActiveIndex = setup.currentIndex;
                    }

                    setup.currentIndex = -1;
                    setup.isDisabled = true;

                    if (setup.triggerUI != null)
                    {
                        var cg = setup.triggerUI.GetComponent<CanvasGroup>() ?? setup.triggerUI.AddComponent<CanvasGroup>();
                        cg.alpha = setup.disabledAlpha;
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }
            }
        }

        int PickRandomAvailable(Transform parent)
        {
            var available = new List<int>();
            for (int i = 0; i < parent.childCount; i++)
                if (!cameraToUI.IsIndexBlocked(parent, i))
                    available.Add(i);

            return available.Count == 0 ? -1 : available[Random.Range(0, available.Count)];
        }

        static List<int> ShuffledIndices(int count)
        {
            var list = new List<int>(count);
            for (int i = 0; i < count; i++) list.Add(i);

            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }

        void SetGraphicAlpha(GameObject go, float alpha)
        {
            if (go == null) return;
            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true))
            {
                Color c = graphic.color; c.a = alpha; graphic.color = c;
            }
        }

        void RegisterScale(GameObject go)
        {
            if (go != null && !_originalScales.ContainsKey(go))
                _originalScales[go] = go.transform.localScale;
        }

        IEnumerator PunchAndExecute(GameObject go, System.Action callback)
        {
            if (go == null) { callback?.Invoke(); yield break; }

            if (!_originalScales.TryGetValue(go, out Vector3 original))
            {
                original = go.transform.localScale;
                _originalScales[go] = original;
            }

            Vector3 punched = original * punchScale;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / punchDuration;
                go.transform.localScale = Vector3.Lerp(original, punched, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            callback?.Invoke();

            if (go != null && go.activeInHierarchy)
            {
                t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / returnDuration;
                    go.transform.localScale = Vector3.Lerp(punched, original, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                go.transform.localScale = original;
            }
        }

        void AddClickListener(GameObject target, UnityEngine.Events.UnityAction action)
        {
            Button btn = target.GetComponent<Button>();
            if (btn != null) { btn.onClick.AddListener(action); return; }

            EnsureRaycast(target);

            var trigger = GetOrAddTrigger(target);
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) =>
            {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Left)
                    action();
            });
            trigger.triggers.Add(entry);
        }

        void EnsureRaycast(GameObject target)
        {
            if (target == null) return;

            Graphic g = target.GetComponent<Graphic>();
            if (g == null)
            {
                var img = target.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
            }
            else g.raycastTarget = true;
        }

        EventTrigger GetOrAddTrigger(GameObject target)
        {
            var t = target.GetComponent<EventTrigger>();
            return t != null ? t : target.AddComponent<EventTrigger>();
        }
    }
}