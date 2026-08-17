using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace My.DemoScene
{

    public class CameraToUI : MonoBehaviour
    {
        [System.Serializable]
        public class CameraSetup
        {
            public string name = "Camera Setup";

            [Header("Camera (Optional)")]
            public Camera sourceCamera;
            public RawImage targetImage;
            public Image maskImage;
            public int textureSize = 512;

            [Header("Costume Switcher")]
            public GameObject nextTrigger;
            public GameObject previousTrigger;
            public Transform costumeParent;

            [Header("None/Disabled Overlay")]
            public GameObject disabledOverlay;

            [Header("Focus")]
            public bool focusOnCostume = false;
            public float focusDistance = 2f;

            [Header("Orbit / Follow Camera")]
            public Transform followTarget;
            public Vector3 orbitOffset = new Vector3(0f, 1.5f, -2f);
            public float lookAtHeightOffset = 1f;
            public bool enableOrbitFollow = false;

            [HideInInspector] public RenderTexture renderTexture;
            [HideInInspector] public int currentCostumeIndex = -1;
            [HideInInspector] public int lastActiveIndex = 0;
            [HideInInspector] public bool isDisabled = false;
        }

        [System.Serializable]
        public class ConflictEntry
        {
            public string name = "New Entry";
            public Transform parent;
            public List<Transform> conflictsWith = new List<Transform>();
        }

        public List<CameraSetup> cameraSetups = new List<CameraSetup>();

        [Header("Conflict Map")]
        public List<ConflictEntry> conflictMap = new List<ConflictEntry>();

        [Header("Click Animation Settings")]
        [SerializeField] private float punchScale    = 0.88f;
        [SerializeField] private float punchDuration = 0.08f;
        [SerializeField] private float returnDuration = 0.1f;

        private readonly Dictionary<Transform, HashSet<int>> _externallyBlockedIndices
            = new Dictionary<Transform, HashSet<int>>();

        private readonly Dictionary<GameObject, Vector3> _originalScales
            = new Dictionary<GameObject, Vector3>();

        public void BlockIndex(Transform parent, int index)
        {
            if (!_externallyBlockedIndices.ContainsKey(parent))
                _externallyBlockedIndices[parent] = new HashSet<int>();
            _externallyBlockedIndices[parent].Add(index);
        }

        public void UnblockIndex(Transform parent, int index)
        {
            if (_externallyBlockedIndices.TryGetValue(parent, out var set))
                set.Remove(index);
        }

        public bool IsIndexBlocked(Transform parent, int index)
        {
            return _externallyBlockedIndices.TryGetValue(parent, out var set) && set.Contains(index);
        }

        void Start()
        {
            foreach (var setup in cameraSetups)
            {
                RegisterScale(setup.nextTrigger);
                RegisterScale(setup.previousTrigger);

                if (setup.sourceCamera != null && setup.targetImage != null)
                {
                    var desc = new RenderTextureDescriptor(
                        setup.textureSize, setup.textureSize,
                        RenderTextureFormat.ARGB32, 16)
                    {
                        msaaSamples = 1,
                        useMipMap   = false,
                        dimension   = TextureDimension.Tex2D
                    };

                    setup.renderTexture      = new RenderTexture(desc);
                    setup.renderTexture.name = setup.name + "_RT";
                    setup.renderTexture.Create();

                    setup.sourceCamera.targetTexture = setup.renderTexture;
                    setup.targetImage.texture        = setup.renderTexture;

                    if (setup.maskImage != null)
                    {
                        Mask mask = setup.maskImage.GetComponent<Mask>();
                        if (mask == null)
                            mask = setup.maskImage.gameObject.AddComponent<Mask>();
                        mask.showMaskGraphic = false;

                        setup.targetImage.transform.SetParent(setup.maskImage.transform, false);

                        RectTransform rt = setup.targetImage.rectTransform;
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }

                    GameObject rightClickTarget = setup.maskImage != null
                        ? setup.maskImage.gameObject
                        : setup.targetImage.gameObject;

                    AddRightClickListener(rightClickTarget, setup);

                    if (setup.disabledOverlay != null)
                        AddRightClickListener(setup.disabledOverlay, setup);
                }

                if (setup.costumeParent != null)
                {
                    InitializeCostumes(setup);

                    var localSetup = setup;

                    if (setup.nextTrigger != null)
                        AddClickListener(setup.nextTrigger, () =>
                            StartCoroutine(PunchAndExecute(localSetup.nextTrigger,
                                () => SwitchCostume(localSetup, 1))));

                    if (setup.previousTrigger != null)
                        AddClickListener(setup.previousTrigger, () =>
                            StartCoroutine(PunchAndExecute(localSetup.previousTrigger,
                                () => SwitchCostume(localSetup, -1))));
                }
            }

            EnforceConflictsForAllActive();
        }

        void LateUpdate()
        {
            foreach (var setup in cameraSetups)
            {
                if (!setup.enableOrbitFollow) continue;
                if (setup.sourceCamera == null) continue;
                if (setup.isDisabled) continue;

                Transform target = GetFollowTarget(setup);
                if (target == null) continue;

                ApplyOrbit(setup, target);
            }
        }

        Transform GetFollowTarget(CameraSetup setup)
        {
            if (setup.followTarget != null)
                return setup.followTarget;

            if (setup.costumeParent != null &&
                setup.currentCostumeIndex >= 0 &&
                setup.currentCostumeIndex < setup.costumeParent.childCount)
            {
                return setup.costumeParent.GetChild(setup.currentCostumeIndex);
            }

            return null;
        }

        void ApplyOrbit(CameraSetup setup, Transform target)
        {
            Vector3 lookAtPos;
            if (setup.focusOnCostume &&
                setup.costumeParent != null &&
                setup.currentCostumeIndex >= 0 &&
                setup.currentCostumeIndex < setup.costumeParent.childCount)
            {
                GameObject costume = setup.costumeParent.GetChild(setup.currentCostumeIndex).gameObject;
                lookAtPos = GetBounds(costume).center;
            }
            else
            {
                lookAtPos = target.position + Vector3.up * setup.lookAtHeightOffset;
            }

            Vector3 offsetDir = (setup.orbitOffset == Vector3.zero)
                ? Vector3.back
                : setup.orbitOffset.normalized;

            Vector3 worldDir = target.rotation * offsetDir;
            setup.sourceCamera.transform.position = lookAtPos + worldDir * setup.focusDistance;
            setup.sourceCamera.transform.LookAt(lookAtPos);
        }

        private void RegisterScale(GameObject go)
        {
            if (go != null && !_originalScales.ContainsKey(go))
                _originalScales[go] = go.transform.localScale;
        }

        private IEnumerator PunchAndExecute(GameObject go, System.Action callback)
        {
            if (go == null) { callback?.Invoke(); yield break; }

            if (!_originalScales.TryGetValue(go, out Vector3 originalScale))
            {
                originalScale    = go.transform.localScale;
                _originalScales[go] = originalScale;
            }

            Vector3 targetScale = originalScale * punchScale;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / punchDuration;
                go.transform.localScale = Vector3.Lerp(originalScale, targetScale,
                    Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            callback?.Invoke();

            if (go != null && go.activeInHierarchy)
            {
                t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / returnDuration;
                    go.transform.localScale = Vector3.Lerp(targetScale, originalScale,
                        Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                go.transform.localScale = originalScale;
            }
        }

        void InitializeCostumes(CameraSetup setup)
        {
            int childCount = setup.costumeParent.childCount;
            if (childCount == 0) return;

            if (!setup.costumeParent.gameObject.activeInHierarchy)
            {
                setup.currentCostumeIndex = -1;
                setup.lastActiveIndex     = 0;
                SetDisabledState(setup, true);
                return;
            }

            int activeIndex = -1;
            for (int i = 0; i < childCount; i++)
            {
                if (setup.costumeParent.GetChild(i).gameObject.activeSelf)
                {
                    if (activeIndex == -1) activeIndex = i;
                    else                   setup.costumeParent.GetChild(i).gameObject.SetActive(false);
                }
            }

            if (activeIndex >= 0)
            {
                setup.currentCostumeIndex = activeIndex;
                setup.lastActiveIndex     = activeIndex;
                SetDisabledState(setup, false);

                if (setup.focusOnCostume && setup.sourceCamera != null)
                    FocusCamera(setup);
            }
            else
            {
                setup.currentCostumeIndex = -1;
                setup.lastActiveIndex     = 0;
                SetDisabledState(setup, true);
            }
        }

        void SetDisabledState(CameraSetup setup, bool disabled)
        {
            setup.isDisabled = disabled;

            if (setup.disabledOverlay != null)
                setup.disabledOverlay.SetActive(disabled);

            if (setup.sourceCamera != null)
                setup.sourceCamera.enabled = !disabled;

            if (setup.targetImage != null)
                setup.targetImage.enabled = !disabled;
        }

        void EnforceConflicts(Transform activatedParent)
        {
            foreach (var entry in conflictMap)
            {
                if (entry.parent != activatedParent) continue;
                foreach (var conflict in entry.conflictsWith)
                {
                    if (conflict != null)
                        DisableParent(conflict);
                }
            }
        }

        void EnforceConflictsForAllActive()
        {
            foreach (var setup in cameraSetups)
            {
                if (setup.costumeParent != null && !setup.isDisabled)
                    EnforceConflicts(setup.costumeParent);
            }
        }

        void DisableParent(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
                parent.GetChild(i).gameObject.SetActive(false);

            parent.gameObject.SetActive(false);

            foreach (var setup in cameraSetups)
            {
                if (setup.costumeParent == parent)
                {
                    if (setup.currentCostumeIndex >= 0)
                        setup.lastActiveIndex = setup.currentCostumeIndex;

                    setup.currentCostumeIndex = -1;
                    SetDisabledState(setup, true);
                }
            }
        }

        void EnableParentResume(Transform parent)
        {
            parent.gameObject.SetActive(true);

            foreach (var setup in cameraSetups)
            {
                if (setup.costumeParent != parent) continue;

                int childCount = parent.childCount;
                if (childCount > 0)
                {
                    for (int i = 0; i < childCount; i++)
                        parent.GetChild(i).gameObject.SetActive(false);

                    int resumeIndex = Mathf.Clamp(setup.lastActiveIndex, 0, childCount - 1);
                    if (IsIndexBlocked(parent, resumeIndex))
                        resumeIndex = FindNextAvailable(parent, resumeIndex, 1, childCount);

                    if (resumeIndex < 0)
                    {
                        SetDisabledState(setup, true);
                        return;
                    }

                    setup.currentCostumeIndex = resumeIndex;
                    setup.lastActiveIndex     = resumeIndex;
                    parent.GetChild(resumeIndex).gameObject.SetActive(true);
                }

                SetDisabledState(setup, false);
                EnforceConflicts(parent);

                if (setup.focusOnCostume && setup.sourceCamera != null && !setup.enableOrbitFollow)
                    FocusCamera(setup);
            }
        }

        void AddRightClickListener(GameObject target, CameraSetup setup)
        {
            EnsureRaycast(target);

            EventTrigger trigger = GetOrAddTrigger(target);
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };

            var localSetup = setup;
            entry.callback.AddListener((data) =>
            {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                    StartCoroutine(PunchAndExecute(target, () => ToggleDisabled(localSetup)));
            });

            trigger.triggers.Add(entry);
        }

        void ToggleDisabled(CameraSetup setup)
        {
            if (setup.costumeParent == null) return;

            if (!setup.isDisabled)
            {
                if (setup.currentCostumeIndex >= 0)
                    setup.lastActiveIndex = setup.currentCostumeIndex;

                if (setup.currentCostumeIndex >= 0 &&
                    setup.currentCostumeIndex < setup.costumeParent.childCount)
                    setup.costumeParent.GetChild(setup.currentCostumeIndex).gameObject.SetActive(false);

                setup.costumeParent.gameObject.SetActive(false);
                setup.currentCostumeIndex = -1;
                SetDisabledState(setup, true);
            }
            else
            {
                EnableParentResume(setup.costumeParent);
            }
        }

        void SwitchCostume(CameraSetup setup, int direction)
        {
            int childCount = setup.costumeParent.childCount;
            if (childCount == 0) return;

            if (setup.isDisabled)
            {
                EnableParentResume(setup.costumeParent);
                return;
            }

            if (setup.currentCostumeIndex >= 0 && setup.currentCostumeIndex < childCount)
                setup.costumeParent.GetChild(setup.currentCostumeIndex).gameObject.SetActive(false);

            int startIndex;
            if (setup.currentCostumeIndex < 0)
                startIndex = direction > 0 ? 0 : childCount - 1;
            else
            {
                startIndex = setup.currentCostumeIndex + direction;
                if (startIndex >= childCount) startIndex = 0;
                else if (startIndex < 0)     startIndex = childCount - 1;
            }

            int nextIndex = FindNextAvailable(setup.costumeParent, startIndex, direction, childCount);

            if (nextIndex < 0)
            {
                if (setup.currentCostumeIndex >= 0)
                    setup.costumeParent.GetChild(setup.currentCostumeIndex).gameObject.SetActive(true);
                return;
            }

            setup.currentCostumeIndex = nextIndex;
            setup.lastActiveIndex     = nextIndex;
            setup.costumeParent.GetChild(nextIndex).gameObject.SetActive(true);

            if (setup.focusOnCostume && setup.sourceCamera != null && !setup.enableOrbitFollow)
                FocusCamera(setup);
        }

        int FindNextAvailable(Transform parent, int startIndex, int direction, int childCount)
        {
            int dir = direction >= 0 ? 1 : -1;
            int idx = startIndex;

            for (int attempt = 0; attempt < childCount; attempt++)
            {
                if (!IsIndexBlocked(parent, idx))
                    return idx;

                idx += dir;
                if (idx >= childCount) idx = 0;
                else if (idx < 0)      idx = childCount - 1;
            }

            return -1;
        }

        void AddClickListener(GameObject target, UnityEngine.Events.UnityAction action)
        {
            Button btn = target.GetComponent<Button>();
            if (btn != null) { btn.onClick.AddListener(action); return; }

            EnsureRaycast(target);
            var trigger = GetOrAddTrigger(target);

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) => action());
            trigger.triggers.Add(entry);
        }

        void EnsureRaycast(GameObject target)
        {
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

        void FocusCamera(CameraSetup setup)
        {
            if (setup.enableOrbitFollow) return;
            if (setup.sourceCamera == null || setup.costumeParent == null) return;

            GameObject activeCostume = setup.costumeParent.GetChild(setup.currentCostumeIndex).gameObject;
            Bounds bounds            = GetBounds(activeCostume);
            Vector3 targetPos        = bounds.center;

            Vector3 direction = (setup.sourceCamera.transform.position - targetPos).normalized;
            setup.sourceCamera.transform.position = targetPos + direction * setup.focusDistance;
            setup.sourceCamera.transform.LookAt(targetPos);
        }

        Bounds GetBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        void OnDestroy()
        {
            foreach (var setup in cameraSetups)
            {
                if (setup.renderTexture != null)
                {
                    if (setup.sourceCamera != null)
                        setup.sourceCamera.targetTexture = null;
                    setup.renderTexture.Release();
                    Destroy(setup.renderTexture);
                }
            }
        }
    }
}