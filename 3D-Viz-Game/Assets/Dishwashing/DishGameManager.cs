using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DishGameManager : MonoBehaviour
{
    [Header("Refs")]
    public DishlineController dishline;
    public GameObject parallelize;

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
    bool paralleled;

    void Start()
    {
        if (!dishline) dishline = Object.FindAnyObjectByType<DishlineController>();
        HideResults();
        StartRound();
    }

    public void StartRound()
    {
        timeLeft = roundDuration;
        score = 0;
        running = true;

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

        ShowResults();
    }

    public void ParaButton()
    {
        paralleled = true;
        parallelize.SetActive(false);
    }

    void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndRound();
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
            resultsBodyText.text = $"Plates cleaned: {score}\nTime used: {FormatTime(timeUsed)}";
        if (parallelize)
        {
            parallelize.SetActive(true);
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
