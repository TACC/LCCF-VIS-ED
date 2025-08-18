using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

public enum GameMode { Ordering, Sorting, Burger }

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Persist Across Scenes")]
    public bool dontDestroyOnLoad = true;

    [Header("Global Timer")]
    [Tooltip("Total game time in seconds (e.g., 120–180).")]
    public int totalGameSeconds = 150; // default 2.5 minutes

    // -------- Rules --------
    [Header("Ordering Rules")]
    public int orderingCorrectPoints = 100;
    public int orderingWrongPoints = -10;
    public float orderingBonusThreshold = 30f; // seconds
    public int orderingBonusPoints = 5;

    [Header("Sorting Rules")]
    [Tooltip("+10 per correct item, awarded in a batch at round end via AwardSortingBatch.")]
    public int sortingPerItemCorrectPoints = 10;
    [Tooltip("+20 if the entire round is perfect (pass grantAllCorrectBonus=true to CompleteTask).")]
    public int sortingAllCorrectBonus = 20;
    [Tooltip("+5 if the round is finished under this time.")]
    public float sortingBonusThreshold = 15f; // seconds
    public int sortingBonusPoints = 5;

    [Header("Burger Rules")]
    public int burgerCorrectPoints = 100;
    public int burgerWrongPoints = -10;
    public float burgerBonusThreshold = 15f; // seconds
    public int burgerBonusPoints = 5;

    // -------- HUD (UGUI) --------
    [Header("UGUI (Optional)")]
    [Tooltip("Assign TMP objects for on-screen HUD. If left empty and Auto Wire is on, the script will look for objects named 'ScoreText' and 'TimerText'.")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    [Tooltip("Shown before the score number.")]
    public string scorePrefix = "Score: ";

    [Tooltip("If true, auto-locate 'ScoreText' and 'TimerText' in the scene by name when missing.")]
    public bool autoWireUI = true;

    // -------- Events --------
    [System.Serializable] public class IntEvent : UnityEvent<int> { }
    [System.Serializable] public class FloatEvent : UnityEvent<float> { }
    [System.Serializable] public class ModeEvent : UnityEvent<GameMode> { }

    public IntEvent OnScoreChanged;
    public FloatEvent OnTimeChanged;
    public UnityEvent OnSessionStarted;
    public UnityEvent OnSessionEnded;
    public ModeEvent OnModeChanged;

    // -------- State --------
    public int Score { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool SessionRunning { get; private set; }
    public GameMode CurrentMode { get; private set; }

    // hidden per-task timer
    private float currentTaskStart = -1f;
    private Coroutine timerCo;

    // ================== Lifecycle ==================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Hook internal UI listeners
        OnScoreChanged.AddListener(HandleScoreChangedUI);
        OnTimeChanged.AddListener(HandleTimeChangedUI);
    }

    private void OnDisable()
    {
        OnScoreChanged.RemoveListener(HandleScoreChangedUI);
        OnTimeChanged.RemoveListener(HandleTimeChangedUI);
    }

    private void Start()
    {
        TryAutoWireUI();
        // Optional: initialize UI in editor play without starting a session
        RefreshScoreUI();
        RefreshTimerUI();
    }

    // ================== Session control ==================
    public void StartSession(int totalSecondsOverride = -1)
    {
        Score = 0;
        TimeRemaining = (totalSecondsOverride > 0) ? totalSecondsOverride : totalGameSeconds;
        SessionRunning = true;
        currentTaskStart = -1f;

        OnScoreChanged?.Invoke(Score);
        OnTimeChanged?.Invoke(TimeRemaining);
        OnSessionStarted?.Invoke();

        if (timerCo != null) StopCoroutine(timerCo);
        timerCo = StartCoroutine(SessionClock());
    }

    public void EndSession()
    {
        if (!SessionRunning) return;
        SessionRunning = false;
        if (timerCo != null) StopCoroutine(timerCo);
        OnSessionEnded?.Invoke();
    }

    private IEnumerator SessionClock()
    {
        while (SessionRunning && TimeRemaining > 0f)
        {
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining < 0f) TimeRemaining = 0f;
            OnTimeChanged?.Invoke(TimeRemaining);
            yield return null;
        }
        EndSession();
    }

    // ================== Mode control ==================
    public void SetMode(GameMode mode)
    {
        CurrentMode = mode;
        OnModeChanged?.Invoke(CurrentMode);
    }

    // ================== Task timing ==================
    // Call when a new task (order/round/burger) becomes active/visible
    public void StartTask()
    {
        currentTaskStart = Time.time;
    }

    // SORTING ONLY: Award +10 per correct item at end of round (call before CompleteTask)
    public void AwardSortingBatch(int correctCount)
    {
        if (CurrentMode != GameMode.Sorting || !SessionRunning) return;
        int toAdd = Mathf.Max(0, correctCount) * sortingPerItemCorrectPoints;
        if (toAdd != 0)
        {
            Score += toAdd;
            OnScoreChanged?.Invoke(Score);
        }
    }

    // Complete the current task.
    // isCorrect: task outcome (Ordering/Burger). For Sorting, pass true if you want time bonus even when not perfect.
    // grantAllCorrectBonus (Sorting only): pass true if EVERY item was correct to grant the +20 all-correct bonus.
    public void CompleteTask(bool isCorrect, bool grantAllCorrectBonus = false)
    {
        if (!SessionRunning) return;

        float taskSecs = (currentTaskStart > 0f) ? Time.time - currentTaskStart : 9999f;

        switch (CurrentMode)
        {
            case GameMode.Ordering:
                Apply(isCorrect, orderingCorrectPoints, orderingWrongPoints,
                      orderingBonusThreshold, orderingBonusPoints, taskSecs);
                break;

            case GameMode.Sorting:
                // Per-item points already granted via AwardSortingBatch(...)
                // Here we handle time bonus and optional all-correct bonus.
                Apply(isCorrect, 0, 0, sortingBonusThreshold, sortingBonusPoints, taskSecs);
                if (grantAllCorrectBonus)
                {
                    Score += sortingAllCorrectBonus;
                    OnScoreChanged?.Invoke(Score);
                }
                break;

            case GameMode.Burger:
                Apply(isCorrect, burgerCorrectPoints, burgerWrongPoints,
                      burgerBonusThreshold, burgerBonusPoints, taskSecs);
                break;
        }

        // reset for next task
        currentTaskStart = -1f;
    }

    // Core scoring applier
    private void Apply(bool correct, int correctPts, int wrongPts, float bonusThresh, int bonusPts, float secs)
    {
        if (correct)
        {
            Score += correctPts;
            if (bonusPts != 0 && bonusThresh > 0f && secs <= bonusThresh)
                Score += bonusPts;
        }
        else
        {
            Score += wrongPts; // can be 0 or negative
        }
        OnScoreChanged?.Invoke(Score);
    }

    // ================== HUD helpers ==================
    private void TryAutoWireUI()
    {
        if (!autoWireUI) return;

        if (scoreText == null)
        {
            var go = GameObject.Find("ScoreText");
            if (go) scoreText = go.GetComponent<TextMeshProUGUI>();
        }

        if (timerText == null)
        {
            var go = GameObject.Find("TimerText");
            if (go) timerText = go.GetComponent<TextMeshProUGUI>();
        }
    }

    private void HandleScoreChangedUI(int newScore) => RefreshScoreUI();
    private void HandleTimeChangedUI(float t) => RefreshTimerUI();

    private void RefreshScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{scorePrefix}{Score}";
        }
    }

    private void RefreshTimerUI()
    {
        if (timerText == null) return;
        int secs = Mathf.CeilToInt(Mathf.Max(0, TimeRemaining));
        int m = secs / 60;
        int s = secs % 60;
        timerText.text = $"{m:0}:{s:00}";
    }

    // Apply a score delta immediately without ending the current task.
// Use for penalties like wrong attempt (-10) during an active order.
public void AddPenalty(int points)
{
    if (!SessionRunning) return;
    Score += points;
    OnScoreChanged?.Invoke(Score);
}

}
