using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DishwashingUI : MonoBehaviour
{
    [Header("HUD (optional)")]
    public TMP_Text timerText;
    public TMP_Text scoreText;

    [Header("Transition Panel")]
    public GameObject maskScreen;
    public GameObject transitionPanel;
    public TMP_Text resultsTitleText;
    public TMP_Text resultsBodyText;
    public Button resultsContinueButton;
    public Button resultCloseButton;
    public Button startGameAgainButton;

    void Awake()
    {

    }

    private void fadeIn()
    {


    }

    private void fadeOut()
    {

    }

    public void startGame()
    {

    }

    public void ShowResults(float roundDuration, float timeLeft, int score)
    {
        // transition screen, show explaination text and video, continue button to advance.
        if (!transitionPanel) return;

        transitionPanel.SetActive(true);

        if (resultsTitleText)
            resultsTitleText.text = "Shift Results";

        // time used is (roundDuration - timeLeft)
        int timeUsed = Mathf.RoundToInt(roundDuration - timeLeft);
        if (resultsBodyText)
        {
            resultsBodyText.text = $"Plates cleaned: {score}\nTime used: {FormatTime(timeUsed)}";
        }

        if (startGameAgainButton)
        {
            startGameAgainButton.onClick.AddListener(HideResults);
        }
    }

    public void HideResults()
    {
        if (transitionPanel) transitionPanel.SetActive(false);
        if (maskScreen) maskScreen.SetActive(false);
    }

    private string FormatTime(int totalSeconds)
    {
        int m = totalSeconds / 60;
        int s = totalSeconds % 60;
        return $"{m:00}:{s:00}";
    }

    public void UpdateHUD(int score, float timeLeft)
    {
        if (timerText) timerText.text = FormatTime(Mathf.CeilToInt(timeLeft));
        if (scoreText) scoreText.text = score.ToString();
    }
}

