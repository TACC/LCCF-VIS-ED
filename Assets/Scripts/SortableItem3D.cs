using UnityEngine;
using System.Collections;
using TMPro;
using System.Text.RegularExpressions;

public class SortableItem3D : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The number used for sorting & checking.")]
    public int value = 0;
    [Tooltip("Where the item returns if not placed.")]
    public Transform home;

    [Header("UI")]
    [Tooltip("Assign a TMP text in children, or leave blank to auto-find the first one.")]
    public TMP_Text valueText;
    public string labelFormat = "{0}";
    public bool autoFindLabel = true;

    [Header("Auto-Init Value")]
    [Tooltip("Parse a number from the label's text on Awake (e.g., '7' or 'x10').")]
    public bool initFromLabel = true;
    [Tooltip("Fallback: copy DragHandler3D.ingredientValue if label has no number.")]
    public bool initFromDragHandler = true;

    [Header("Motion")]
    public float moveDuration = 0.25f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [HideInInspector] public int slotIndex = -1; // -1 = not placed

    [Header("Debug")]
    public bool debugLogs = true;   // <- turn this on per item to see logs

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        MaybeFindLabel();

        if (debugLogs)
        {
            var labelText = valueText ? valueText.text : "<none>";
            Debug.Log($"[SI] Awake '{name}': start value={value}, label='{labelText}'", this);
        }

        bool set = false;

        // 1) From label
        if (initFromLabel && TryInitFromLabel(out int labelVal))
        {
            if (debugLogs) Debug.Log($"[SI] '{name}': init value FROM LABEL = {labelVal}", this);
            value = labelVal;
            set = true;
        }

        // 2) From DragHandler.ingredientValue
        if (!set && initFromDragHandler && TryInitFromDragHandler(out int dragVal))
        {
            if (debugLogs) Debug.Log($"[SI] '{name}': init value FROM DRAGHANDLER = {dragVal}", this);
            value = dragVal;
            set = true;
        }

        if (debugLogs && !set)
            Debug.Log($"[SI] '{name}': no init source found; keeping inspector value={value}", this);

        RefreshLabel();
        if (debugLogs && valueText)
            Debug.Log($"[SI] '{name}': after RefreshLabel -> label='{valueText.text}'", this);
    }

    // Keep references fresh in editor without stomping text
    void OnValidate()
    {
        if (autoFindLabel && valueText == null)
            MaybeFindLabel();

        if (!Application.isPlaying && value == 0 && initFromLabel && valueText != null)
        {
            if (TryExtractInt(valueText.text, out int parsed))
            {
                value = parsed;
#if UNITY_EDITOR
                if (debugLogs)
                    Debug.Log($"[SI] (Validate) '{name}': adopted value {value} from existing label '{valueText.text}'", this);
#endif
            }
        }
    }

    void MaybeFindLabel()
    {
        if (valueText == null && autoFindLabel)
        {
            valueText = GetComponentInChildren<TMP_Text>(true);
            if (debugLogs)
                Debug.Log($"[SI] '{name}': valueText {(valueText ? "FOUND" : "NOT FOUND")} (autoFindLabel={autoFindLabel})", this);
        }
    }

    bool TryInitFromLabel(out int parsed)
    {
        parsed = 0;
        if (valueText == null) return false;

        bool ok = TryExtractInt(valueText.text, out parsed);
        if (debugLogs)
            Debug.Log($"[SI] '{name}': TryInitFromLabel text='{valueText.text}' -> ok={ok} parsed={parsed}", this);
        return ok;
    }

    bool TryInitFromDragHandler(out int parsed)
    {
        parsed = 0;
        var dh = GetComponent<DragHandler3D>();
        if (dh == null)
        {
            if (debugLogs) Debug.Log($"[SI] '{name}': no DragHandler3D found", this);
            return false;
        }

        parsed = dh.ingredientValue;
        bool ok = parsed != 0; // treat 0 as 'unset'
        if (debugLogs)
            Debug.Log($"[SI] '{name}': TryInitFromDragHandler ingredientValue={dh.ingredientValue} -> ok={ok}", this);
        return ok;
    }

    static bool TryExtractInt(string s, out int parsed)
    {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;

        var m = Regex.Match(s, @"-?\d+");
        if (!m.Success) return false;
        return int.TryParse(m.Value, out parsed);
    }

    public void RefreshLabel()
    {
        if (valueText != null)
            valueText.text = string.Format(labelFormat, value);
    }

    public void SetValue(int v)
    {
        if (debugLogs) Debug.Log($"[SI] '{name}': SetValue {value} -> {v}", this);
        value = v;
        RefreshLabel();
        if (debugLogs && valueText)
            Debug.Log($"[SI] '{name}': after SetValue -> label='{valueText.text}'", this);
    }

    public void SetPhysicsKinematic(bool k)
    {
        if (rb) rb.isKinematic = k;
    }

    public Coroutine MoveTo(Transform target) => StartCoroutine(MoveToCo(target.position));
    public Coroutine MoveTo(Vector3 worldPos) => StartCoroutine(MoveToCo(worldPos));

    IEnumerator MoveToCo(Vector3 dest)
    {
        SetPhysicsKinematic(true); // lock during tween
        Vector3 start = transform.position;
        float t = 0f;
        float dur = Mathf.Max(0.01f, moveDuration);
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float e = moveCurve.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(start, dest, e);
            yield return null;
        }
        // stay kinematic after snapping
    }

    public void ReturnHome()
    {
        if (home) StartCoroutine(MoveToCo(home.position));
    }
}
