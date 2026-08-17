using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace My.DemoScene
{

    public class AccessorySlotManager : MonoBehaviour
    {
        [System.Serializable]
        public class SlotSetup
        {
            public string name = "Slot Setup";

            public Transform costumeParent;

            public GameObject addButton;
            public GameObject nextButton;
            public GameObject previousButton;

            public float disabledAlpha = 0.3f;

            [HideInInspector] public int currentIndex = -1;
            [HideInInspector] public int lastIndex     = 0;
            [HideInInspector] public bool isActive     = false;
        }

        public List<SlotSetup> slots = new List<SlotSetup>();

        public CameraToUI cameraToUI;

        [Header("Click Animation")]
        [SerializeField] private float punchScale     = 0.88f;
        [SerializeField] private float punchDuration  = 0.08f;
        [SerializeField] private float returnDuration = 0.1f;

        private readonly Dictionary<GameObject, Vector3> _originalScales
            = new Dictionary<GameObject, Vector3>();

        // ------------------------------------------------------------------

        void Start()
        {
            if (cameraToUI == null)
                cameraToUI = FindAnyObjectByType<CameraToUI>();

            foreach (var slot in slots)
            {
                RegisterScale(slot.addButton);
                RegisterScale(slot.nextButton);
                RegisterScale(slot.previousButton);

                var s = slot;

                if (slot.addButton != null)
                {
                    AddClickListener(slot.addButton, () =>
                        StartCoroutine(PunchAndExecute(s.addButton, () => TryActivateSlot(s))));
                    AddRightClickListener(slot.addButton, () =>
                        StartCoroutine(PunchAndExecute(s.addButton, () => DeactivateSlot(s))));
                }

                if (slot.nextButton != null)
                    AddClickListener(slot.nextButton, () =>
                        StartCoroutine(PunchAndExecute(s.nextButton, () => SwitchSlot(s, 1))));

                if (slot.previousButton != null)
                    AddClickListener(slot.previousButton, () =>
                        StartCoroutine(PunchAndExecute(s.previousButton, () => SwitchSlot(s, -1))));

                SetButtonsAlpha(slot, false);
            }
        }

        // ------------------------------------------------------------------
        // Activate / Deactivate
        // ------------------------------------------------------------------

        void TryActivateSlot(SlotSetup slot)
        {
            if (slot.isActive) { DeactivateSlot(slot); return; }
            if (slot.costumeParent == null) return;

            int childCount = slot.costumeParent.childCount;
            if (childCount == 0) return;

            int start  = Mathf.Clamp(slot.lastIndex, 0, childCount - 1);
            int target = FindAvailable(slot, start, 1, childCount);
            if (target < 0) return;

            Activate(slot, target);
        }

        void Activate(SlotSetup slot, int index)
        {
            slot.currentIndex = index;
            slot.lastIndex    = index;
            slot.isActive     = true;

            slot.costumeParent.GetChild(index).gameObject.SetActive(true);
            cameraToUI?.BlockIndex(slot.costumeParent, index);

            SetButtonsAlpha(slot, true);
        }

        void DeactivateSlot(SlotSetup slot)
        {
            if (!slot.isActive) return;

            if (slot.currentIndex >= 0 && slot.costumeParent != null &&
                slot.currentIndex < slot.costumeParent.childCount)
            {
                slot.costumeParent.GetChild(slot.currentIndex).gameObject.SetActive(false);
                cameraToUI?.UnblockIndex(slot.costumeParent, slot.currentIndex);
            }

            slot.lastIndex    = slot.currentIndex >= 0 ? slot.currentIndex : slot.lastIndex;
            slot.currentIndex = -1;
            slot.isActive     = false;

            SetButtonsAlpha(slot, false);
        }

        // ------------------------------------------------------------------
        // Switch
        // ------------------------------------------------------------------

        void SwitchSlot(SlotSetup slot, int direction)
        {
            if (!slot.isActive) { TryActivateSlot(slot); return; }
            if (slot.costumeParent == null) return;

            int childCount = slot.costumeParent.childCount;
            if (childCount == 0) return;

            if (slot.currentIndex >= 0)
            {
                slot.costumeParent.GetChild(slot.currentIndex).gameObject.SetActive(false);
                cameraToUI?.UnblockIndex(slot.costumeParent, slot.currentIndex);
            }

            int start = slot.currentIndex < 0
                ? (direction > 0 ? 0 : childCount - 1)
                : Wrap(slot.currentIndex + direction, childCount);

            int next = FindAvailable(slot, start, direction, childCount);

            if (next < 0)
            {
                if (slot.currentIndex >= 0)
                {
                    slot.costumeParent.GetChild(slot.currentIndex).gameObject.SetActive(true);
                    cameraToUI?.BlockIndex(slot.costumeParent, slot.currentIndex);
                }
                return;
            }

            Activate(slot, next);
        }

        // ------------------------------------------------------------------
        // Button alpha
        // ------------------------------------------------------------------

        void SetButtonsAlpha(SlotSetup slot, bool active)
        {
            SetGraphicAlpha(slot.addButton,      active ? 1f : slot.disabledAlpha);
            SetGraphicAlpha(slot.nextButton,     active ? 1f : slot.disabledAlpha);
            SetGraphicAlpha(slot.previousButton, active ? 1f : slot.disabledAlpha);
        }

        void SetGraphicAlpha(GameObject go, float alpha)
        {
            if (go == null) return;
            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true))
            {
                Color c = graphic.color;
                c.a = alpha;
                graphic.color = c;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        int FindAvailable(SlotSetup caller, int startIndex, int direction, int childCount)
        {
            int dir = direction >= 0 ? 1 : -1;
            int idx = startIndex;

            for (int i = 0; i < childCount; i++)
            {
                if (!IsUsedByCameraToUI(caller.costumeParent, idx) &&
                    !IsUsedByOtherSlot(caller, idx))
                    return idx;

                idx = Wrap(idx + dir, childCount);
            }

            return -1;
        }

        bool IsUsedByCameraToUI(Transform parent, int index)
        {
            if (cameraToUI == null) return false;
            foreach (var setup in cameraToUI.cameraSetups)
            {
                if (setup.costumeParent == parent &&
                    setup.currentCostumeIndex == index &&
                    !setup.isDisabled)
                    return true;
            }
            return false;
        }

        bool IsUsedByOtherSlot(SlotSetup caller, int index)
        {
            foreach (var slot in slots)
            {
                if (slot == caller) continue;
                if (slot.costumeParent == caller.costumeParent &&
                    slot.currentIndex == index &&
                    slot.isActive)
                    return true;
            }
            return false;
        }

        static int Wrap(int value, int count)
        {
            if (value >= count) return 0;
            if (value < 0)      return count - 1;
            return value;
        }

        // ------------------------------------------------------------------
        // Animation
        // ------------------------------------------------------------------

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
                original            = go.transform.localScale;
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

        // ------------------------------------------------------------------
        // Click listeners
        // ------------------------------------------------------------------

        void AddClickListener(GameObject target, UnityEngine.Events.UnityAction action)
        {
            EnsureRaycast(target);
            var trigger = GetOrAddTrigger(target);
            var entry   = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) =>
            {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Left)
                    action();
            });
            trigger.triggers.Add(entry);
        }

        void AddRightClickListener(GameObject target, UnityEngine.Events.UnityAction action)
        {
            EnsureRaycast(target);
            var trigger = GetOrAddTrigger(target);
            var entry   = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) =>
            {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                    action();
            });
            trigger.triggers.Add(entry);
        }

        void EnsureRaycast(GameObject target)
        {
            if (target == null) return;
            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image img = target.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                graphic   = img;
            }
            graphic.raycastTarget = true;
        }

        EventTrigger GetOrAddTrigger(GameObject target)
        {
            var t = target.GetComponent<EventTrigger>();
            if (t == null) t = target.AddComponent<EventTrigger>();
            return t;
        }
    }
}