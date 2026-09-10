using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace My.DemoScene
{

    public class ParentableSwitcher : MonoBehaviour
    {
        [System.Serializable]
        public class SwitcherSetup
        {
            public string name = "New Setup";

            [Header("UI Trigger")]
            public GameObject triggerUI;

            [Header("Object Group")]
            public Transform objectParent;

            [Header("Disabled Görünüm")]
            [Range(0.1f, 1f)]
            public float disabledAlpha = 0.4f;

            [Header("Kısıtlamalar")]
            public bool canBeDisabled = true;

            [HideInInspector] public int currentIndex = -1;
            [HideInInspector] public int lastActiveIndex = 0;
            [HideInInspector] public bool isDisabled = false;
        }

        [System.Serializable]
        public class AdjustGroup
        {
            public string name = "New Adjust Group";

            [Header("Ayarlanacak Objeler")]
            public List<Transform> targets = new List<Transform>();

            [Header("UI")]
            public GameObject upButton;
            public GameObject downButton;

            [Header("Ayarlar")]
            public float moveSpeed = 0.5f;
            public float minY = -1f;
            public float maxY = 1f;

            [Header("Availability")]
            [Range(0.1f, 1f)]
            public float unavailableAlpha = 0.5f;

            [HideInInspector] public bool isAvailable = true;
            [HideInInspector] public bool holdingUp = false;
            [HideInInspector] public bool holdingDown = false;
        }

        public List<SwitcherSetup> setups = new List<SwitcherSetup>();

        [Header("Adjust Y")]
        public List<AdjustGroup> adjustGroups = new List<AdjustGroup>();

        [Header("Click Animation Settings")]
        [SerializeField] private float punchScale    = 0.88f;
        [SerializeField] private float punchDuration = 0.08f;
        [SerializeField] private float returnDuration = 0.1f;

        private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
        private HashSet<GameObject> runningPunches = new HashSet<GameObject>();

        // -----------------------------------------------------------------------
        void Start()
        {
            foreach (var setup in setups)
            {
                if (setup.objectParent == null) continue;

                InitializeSetup(setup);

                if (setup.triggerUI != null)
                {
                    var local = setup;
                    AddLeftClickListener(setup.triggerUI, () =>
                    {
                        StartCoroutine(PunchAndExecute(local.triggerUI, () => OnLeftClick(local)));
                    });
                    AddRightClickListener(setup.triggerUI, () =>
                    {
                        StartCoroutine(PunchAndExecute(local.triggerUI, () => OnRightClick(local)));
                    });
                }
            }

            foreach (var group in adjustGroups)
            {
                var localGroup = group;

                if (group.upButton != null)
                    AddHoldListener(group.upButton, localGroup, true);

                if (group.downButton != null)
                    AddHoldListener(group.downButton, localGroup, false);

                UpdateAdjustGroupAvailability(group);
            }
        }

        void Update()
        {
            foreach (var group in adjustGroups)
            {
                UpdateAdjustGroupAvailability(group);

                if (!group.isAvailable) continue;

                float direction = 0f;
                if (group.holdingUp) direction += 1f;
                if (group.holdingDown) direction -= 1f;

                if (direction == 0f) continue;

                float delta = direction * group.moveSpeed * Time.deltaTime;

                foreach (var target in group.targets)
                {
                    if (target == null || !target.gameObject.activeInHierarchy) continue;

                    Vector3 pos = target.localPosition;
                    pos.y = Mathf.Clamp(pos.y + delta, group.minY, group.maxY);
                    target.localPosition = pos;
                }
            }
        }

        // -----------------------------------------------------------------------
        // ANİMASYON
        // -----------------------------------------------------------------------
        private IEnumerator PunchAndExecute(GameObject go, System.Action callback)
        {
            if (go == null) { callback?.Invoke(); yield break; }

            if (!originalScales.ContainsKey(go))
                originalScales[go] = go.transform.localScale;

            Vector3 originalScale = originalScales[go];

            if (runningPunches.Contains(go))
            {
                go.transform.localScale = originalScale;
                callback?.Invoke();
                yield break;
            }

            runningPunches.Add(go);

            Vector3 targetScale = originalScale * punchScale;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / punchDuration;
                go.transform.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            callback?.Invoke();

            if (go != null && go.activeInHierarchy)
            {
                t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / returnDuration;
                    go.transform.localScale = Vector3.Lerp(targetScale, originalScale, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                go.transform.localScale = originalScale;
            }

            runningPunches.Remove(go);
        }

        // -----------------------------------------------------------------------
        // SETUP & STATE
        // -----------------------------------------------------------------------
        void UpdateAdjustGroupAvailability(AdjustGroup group)
        {
            bool anyActive = false;
            foreach (var target in group.targets)
            {
                if (target != null && target.gameObject.activeInHierarchy)
                {
                    anyActive = true;
                    break;
                }
            }

            if (anyActive != group.isAvailable)
            {
                group.isAvailable = anyActive;
                if (!anyActive)
                {
                    group.holdingUp = false;
                    group.holdingDown = false;
                }
            }

            float alpha = anyActive ? 1f : group.unavailableAlpha;

            SetButtonAlpha(group.upButton, alpha, anyActive);
            SetButtonAlpha(group.downButton, alpha, anyActive);
        }

        void SetButtonAlpha(GameObject btn, float alpha, bool interactable)
        {
            if (btn == null) return;
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
            cg.interactable = interactable;
            cg.blocksRaycasts = true;
        }

        void InitializeSetup(SwitcherSetup setup)
        {
            int childCount = setup.objectParent.childCount;
            if (childCount == 0) return;

            int activeIndex = -1;
            for (int i = 0; i < childCount; i++)
            {
                if (setup.objectParent.GetChild(i).gameObject.activeSelf)
                {
                    if (activeIndex == -1)
                        activeIndex = i;
                    else
                        setup.objectParent.GetChild(i).gameObject.SetActive(false);
                }
            }

            if (activeIndex >= 0)
            {
                setup.currentIndex = activeIndex;
                setup.lastActiveIndex = activeIndex;
                SetDisabledState(setup, false);
            }
            else
            {
                setup.currentIndex = -1;
                setup.lastActiveIndex = 0;
                SetDisabledState(setup, true);
            }
        }

        void SetDisabledState(SwitcherSetup setup, bool disabled)
        {
            setup.isDisabled = disabled;
            if (setup.triggerUI == null) return;

            CanvasGroup cg = setup.triggerUI.GetComponent<CanvasGroup>();
            if (cg == null) cg = setup.triggerUI.AddComponent<CanvasGroup>();

            cg.alpha = disabled ? setup.disabledAlpha : 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        void OnLeftClick(SwitcherSetup setup)
        {
            int childCount = setup.objectParent.childCount;
            if (childCount == 0) return;

            if (setup.isDisabled)
            {
                EnableParentResume(setup);
                return;
            }

            if (setup.currentIndex >= 0 && setup.currentIndex < childCount)
                setup.objectParent.GetChild(setup.currentIndex).gameObject.SetActive(false);

            if (setup.currentIndex < 0)
                setup.currentIndex = 0;
            else
            {
                setup.currentIndex++;
                if (setup.currentIndex >= childCount)
                    setup.currentIndex = 0;
            }

            setup.objectParent.GetChild(setup.currentIndex).gameObject.SetActive(true);
            setup.lastActiveIndex = setup.currentIndex;
        }

        void OnRightClick(SwitcherSetup setup)
        {
            if (setup.objectParent == null) return;

            // canBeDisabled false ise sağ tıklama hiçbir şey yapmaz
            if (!setup.canBeDisabled) return;

            if (!setup.isDisabled)
            {
                if (setup.currentIndex >= 0 && setup.currentIndex < setup.objectParent.childCount)
                    setup.objectParent.GetChild(setup.currentIndex).gameObject.SetActive(false);

                if (setup.currentIndex >= 0)
                    setup.lastActiveIndex = setup.currentIndex;

                setup.currentIndex = -1;
                SetDisabledState(setup, true);
            }
            else
            {
                EnableParentResume(setup);
            }
        }

        void EnableParentResume(SwitcherSetup setup)
        {
            int childCount = setup.objectParent.childCount;
            if (childCount == 0) return;

            for (int i = 0; i < childCount; i++)
                setup.objectParent.GetChild(i).gameObject.SetActive(false);

            int resumeIndex = Mathf.Clamp(setup.lastActiveIndex, 0, childCount - 1);
            setup.objectParent.GetChild(resumeIndex).gameObject.SetActive(true);

            setup.currentIndex = resumeIndex;
            setup.lastActiveIndex = resumeIndex;
            SetDisabledState(setup, false);
        }

        // -----------------------------------------------------------------------
        // EVENT LISTENERS
        // -----------------------------------------------------------------------
        void AddHoldListener(GameObject target, AdjustGroup group, bool isUp)
        {
            EnsureRaycast(target);

            EventTrigger trigger = GetOrAddTrigger(target);

            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener((data) =>
            {
                if (!group.isAvailable) return;
                if (isUp) group.holdingUp = true;
                else group.holdingDown = true;
                StartCoroutine(PunchAndExecute(target, null));
            });
            trigger.triggers.Add(downEntry);

            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener((data) =>
            {
                if (isUp) group.holdingUp = false;
                else group.holdingDown = false;
            });
            trigger.triggers.Add(upEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) =>
            {
                if (isUp) group.holdingUp = false;
                else group.holdingDown = false;
            });
            trigger.triggers.Add(exitEntry);
        }

        void AddLeftClickListener(GameObject target, UnityEngine.Events.UnityAction action)
        {
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

        void AddRightClickListener(GameObject target, UnityEngine.Events.UnityAction action)
        {
            EnsureRaycast(target);
            var trigger = GetOrAddTrigger(target);

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) =>
            {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                    action();
            });
            trigger.triggers.Add(entry);
        }

        void EnsureRaycast(GameObject target)
        {
            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image img = target.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                graphic = img;
            }
            graphic.raycastTarget = true;
        }

        EventTrigger GetOrAddTrigger(GameObject target)
        {
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<EventTrigger>();
            return trigger;
        }
    }
}