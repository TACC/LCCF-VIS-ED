using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class DishGameManager : MonoBehaviour
{
    [Header("Refs")]
    public DishlineController dishline;
    public GameObject parallelize;  //parallelize button itself
    public BrushManager brushes;

    [Header("Round")]
    public float roundDuration = 20f;

    [Header("HUD (optional)")]
    public TMP_Text timerText;
    public TMP_Text scoreText;

    [Header("Results Panel (optional)")]
    public GameObject resultsPanel;
    public TMP_Text resultsTitleText;
    public TMP_Text resultsBodyText;
    public Button resultsCloseButton;

    float timeLeft;
    int score;       // number of plates cleaned
    bool running;
    bool continueRound; //if the game should continue, for 2nd playthrough
    bool parallelized;

    void Start()
    {
        if (!dishline) dishline = Object.FindAnyObjectByType<DishlineController>();
        HideResults();
        StartRound();
        continueRound = false;
        parallelized = false;
        score = 0;
    }

    public void StartRound()
    {
        timeLeft = roundDuration;
        score = 0;
        running = true;

        parallelize.SetActive(false);
        if (dishline)
        {
            dishline.OnPlateStacked += HandlePlateStacked;
            dishline.StartSpawning();
        }

        UpdateHUD();
        HideResults();
    }

    public void EndRound()
    {
        if (!running) return;
        running = false;

        if (dishline)
        {
            dishline.OnPlateStacked -= HandlePlateStacked;
            dishline.StopSpawning();
        }
        Debug.Log("round over");
        ShowResults();
    }

    public void ParaButton()
    {
        parallelized = true;
        brushes.addBrushes();
        parallelize.SetActive(false);
    }

    void Update()
    {
        //if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndRound();
        }

        if (continueRound && !running)
        {
            continueRound = false;
            StartRound();
        }

        if (score >= 5 && !parallelized)
        {
            parallelize.SetActive(true);
        }

        UpdateHUD();
    }

    void HandlePlateStacked(PlateController plate)
    {
        score++;
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (timerText) timerText.text = FormatTime(Mathf.CeilToInt(timeLeft));
        if (scoreText) scoreText.text = score.ToString();
    }

    // result helpers
    void ShowResults()
    {
        if (!resultsPanel) return;

        resultsPanel.SetActive(true);

        if (resultsTitleText)
            resultsTitleText.text = "Shift Results";

        // time used is (roundDuration - timeLeft)
        int timeUsed = Mathf.RoundToInt(roundDuration - timeLeft);
        if (resultsBodyText)
        {
            resultsBodyText.text = $"Plates cleaned: {score}\nTime used: {FormatTime(timeUsed)}";
        }

        if (resultsCloseButton)
        {
            resultsCloseButton.onClick.RemoveAllListeners();
            resultsCloseButton.onClick.AddListener(HideResults);
        }
    }

    public void HideResults()
    {
        if (resultsPanel) resultsPanel.SetActive(false);
    }

    string FormatTime(int totalSeconds)
    {
        int m = totalSeconds / 60;
        int s = totalSeconds % 60;
        return $"{m:00}:{s:00}";
    }
}
