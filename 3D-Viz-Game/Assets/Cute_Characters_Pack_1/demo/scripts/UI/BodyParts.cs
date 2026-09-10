using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.DemoScene
{

    public class UIBodyPartToggleManager : MonoBehaviour
    {
        [System.Serializable]
        public class BodyPartToggle
        {
            public GameObject onElement;
            public GameObject offElement;
            public List<GameObject> targetObjects = new List<GameObject>();
        }

        [System.Serializable]
        public class AutoHideRule
        {
            public List<GameObject> watchObjects = new List<GameObject>();
            public List<int> hideToggleIndexes = new List<int>();
        }

        [Header("Body Part Toggles")]
        [SerializeField] private List<BodyPartToggle> bodyPartToggles = new List<BodyPartToggle>();

        [Header("Auto-Hide Rules")]
        [SerializeField] private List<AutoHideRule> autoHideRules = new List<AutoHideRule>();

        [Header("All On / All Off")]
        [SerializeField] private GameObject allOnElement;
        [SerializeField] private GameObject allOffElement;

        [Header("Menu Navigation")]
        [SerializeField] private GameObject bodySelectionMenu;
        [SerializeField] private GameObject customisationMenu;
        [SerializeField] private GameObject toBodySelectionElement;
        [SerializeField] private GameObject returnElement;

        [Header("Click Animation Settings")]
        [SerializeField] private float flashAlpha    = 0.3f;
        [SerializeField] private float flashDownTime = 0.06f;
        [SerializeField] private float flashUpTime   = 0.1f;
        [SerializeField] private float fadeDuration  = 0.15f;

        private readonly Dictionary<int, bool> _userToggleState = new Dictionary<int, bool>();
        private readonly HashSet<int> _autoHiddenIndexes = new HashSet<int>();

        private void Start()
        {
            InitializeToggles();
            SetupNavigation();
        }

        private void Update()
        {
            ApplyAutoHideRules();
        }

        private void ApplyAutoHideRules()
        {
            if (autoHideRules == null || autoHideRules.Count == 0) return;

            HashSet<int> shouldHide = new HashSet<int>();

            foreach (var rule in autoHideRules)
            {
                if (rule.watchObjects == null || rule.watchObjects.Count == 0) continue;

                bool anyActive = false;
                foreach (var watchObj in rule.watchObjects)
                {
                    if (watchObj != null && watchObj.activeInHierarchy)
                    {
                        anyActive = true;
                        break;
                    }
                }

                if (anyActive)
                {
                    foreach (int idx in rule.hideToggleIndexes)
                        shouldHide.Add(idx);
                }
            }

            foreach (int idx in shouldHide)
            {
                if (_autoHiddenIndexes.Contains(idx)) continue;
                if (idx < 0 || idx >= bodyPartToggles.Count) continue;

                var toggle = bodyPartToggles[idx];
                SetTargetObjects(toggle, false);
                HideElement(toggle.onElement);
                ShowElement(toggle.offElement);

                _autoHiddenIndexes.Add(idx);
            }

            List<int> toRestore = new List<int>();
            foreach (int idx in _autoHiddenIndexes)
                if (!shouldHide.Contains(idx))
                    toRestore.Add(idx);

            foreach (int idx in toRestore)
            {
                if (idx < 0 || idx >= bodyPartToggles.Count) continue;
                var toggle = bodyPartToggles[idx];

                bool userWantsOn = !_userToggleState.ContainsKey(idx) || _userToggleState[idx];

                if (userWantsOn)
                {
                    SetTargetObjects(toggle, true);
                    ShowElement(toggle.onElement);
                    HideElement(toggle.offElement);
                }
                else
                {
                    SetTargetObjects(toggle, false);
                    HideElement(toggle.onElement);
                    ShowElement(toggle.offElement);
                }

                _autoHiddenIndexes.Remove(idx);
            }
        }

        private void InitializeToggles()
        {
            for (int i = 0; i < bodyPartToggles.Count; i++)
            {
                var toggle = bodyPartToggles[i];

                if (toggle.onElement == null || toggle.offElement == null)
                {
                    Debug.LogWarning("[UIBodyPartToggleManager] Missing references, skipping toggle.");
                    continue;
                }

                if (toggle.targetObjects == null || toggle.targetObjects.Count == 0)
                {
                    Debug.LogWarning("[UIBodyPartToggleManager] targetObjects list is empty, skipping toggle.");
                    continue;
                }

                _userToggleState[i] = true;
                SetTargetObjects(toggle, true);
                ShowElement(toggle.onElement);
                HideElement(toggle.offElement);

                var t = toggle;
                int index = i;

                AddClickHandler(toggle.onElement, () =>
                    StartCoroutine(FlashAndExecute(t.onElement, () =>
                    {
                        _userToggleState[index] = false;

                        if (!_autoHiddenIndexes.Contains(index))
                            SetTargetObjects(t, false);

                        HideElement(t.onElement);
                        ShowElement(t.offElement);
                        StartCoroutine(FadeIn(t.offElement));
                    })));

                AddClickHandler(toggle.offElement, () =>
                    StartCoroutine(FlashAndExecute(t.offElement, () =>
                    {
                        _userToggleState[index] = true;

                        if (!_autoHiddenIndexes.Contains(index))
                            SetTargetObjects(t, true);

                        HideElement(t.offElement);
                        ShowElement(t.onElement);
                        StartCoroutine(FadeIn(t.onElement));
                    })));
            }
        }

        private void SetupNavigation()
        {
            if (allOnElement != null)
                AddClickHandler(allOnElement, () =>
                    StartCoroutine(FlashAndExecute(allOnElement, () =>
                    {
                        for (int i = 0; i < bodyPartToggles.Count; i++)
                        {
                            _userToggleState[i] = true;
                            var toggle = bodyPartToggles[i];
                            if (!_autoHiddenIndexes.Contains(i))
                                SetTargetObjects(toggle, true);
                            ShowElement(toggle.onElement);
                            HideElement(toggle.offElement);
                        }
                    })));

            if (allOffElement != null)
                AddClickHandler(allOffElement, () =>
                    StartCoroutine(FlashAndExecute(allOffElement, () =>
                    {
                        for (int i = 0; i < bodyPartToggles.Count; i++)
                        {
                            _userToggleState[i] = false;
                            var toggle = bodyPartToggles[i];
                            SetTargetObjects(toggle, false);
                            HideElement(toggle.onElement);
                            ShowElement(toggle.offElement);
                        }
                    })));

            if (toBodySelectionElement != null)
                AddClickHandler(toBodySelectionElement, () =>
                    StartCoroutine(FlashAndExecute(toBodySelectionElement, () =>
                        StartCoroutine(FadeOutIn(customisationMenu, bodySelectionMenu)))));

            if (returnElement != null)
                AddClickHandler(returnElement, () =>
                    StartCoroutine(FlashAndExecute(returnElement, () =>
                        StartCoroutine(FadeOutIn(bodySelectionMenu, customisationMenu)))));
        }

        private void SetTargetObjects(BodyPartToggle toggle, bool active)
        {
            if (toggle.targetObjects == null) return;
            foreach (var obj in toggle.targetObjects)
                if (obj != null) obj.SetActive(active);
        }

        private void ShowElement(GameObject go)
        {
            if (go == null) return;
            ResetAlpha(go);
            go.SetActive(true);
        }

        private void HideElement(GameObject go)
        {
            if (go == null) return;
            ResetAlpha(go);
            go.SetActive(false);
        }

        private void ResetAlpha(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        private IEnumerator FlashAndExecute(GameObject go, System.Action callback)
        {
            if (go == null) { callback?.Invoke(); yield break; }

            CanvasGroup cg = GetOrAddCanvasGroup(go);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / flashDownTime;
                cg.alpha = Mathf.Lerp(1f, flashAlpha, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            cg.alpha = flashAlpha;

            callback?.Invoke();

            if (go != null && go.activeInHierarchy)
            {
                t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / flashUpTime;
                    cg.alpha = Mathf.Lerp(flashAlpha, 1f, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                cg.alpha = 1f;
            }
        }

        private IEnumerator FadeIn(GameObject go)
        {
            if (go == null) yield break;

            CanvasGroup cg = GetOrAddCanvasGroup(go);
            cg.alpha = 0f;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / fadeDuration;
                cg.alpha = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            cg.alpha = 1f;
        }

        private IEnumerator FadeOutIn(GameObject outMenu, GameObject inMenu)
        {
            if (outMenu != null)
            {
                CanvasGroup cgOut = GetOrAddCanvasGroup(outMenu);
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / fadeDuration;
                    cgOut.alpha = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                cgOut.alpha = 0f;
                outMenu.SetActive(false);
            }

            if (inMenu != null)
            {
                inMenu.SetActive(true);
                CanvasGroup cgIn = GetOrAddCanvasGroup(inMenu);
                cgIn.alpha = 0f;

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / fadeDuration;
                    cgIn.alpha = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                cgIn.alpha = 1f;
            }
        }

        private void AddClickHandler(GameObject go, System.Action callback)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((_) => callback());
            trigger.triggers.Add(entry);
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }
    }
}