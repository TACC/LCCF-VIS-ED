using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    int score = 0;

    [SerializeField] TextMeshProUGUI scoreText;

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        scoreText.text = $"Score: {score}";
    }

    public void ResetScore()
    {
        score = 0;
        UpdateScoreText();
    }
}
