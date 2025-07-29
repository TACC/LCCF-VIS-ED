using UnityEngine;
using System.Collections;
public class GameManager : MonoBehaviour
{
    public BunSlideManager bunSpawner;

    private int difficultyLevel = 0; // 0 = 7 buns, 1 = 8 buns, 2 = 10 buns
    private readonly int[] bunCounts = { 7, 8, 10 };
    private readonly float[] timeLimits = { 30f, 20f, 15f };

    private void Start()
    {
        DropZoneManager.Instance.onStackComplete = EvaluateResult;
        StartRound();
    }

    void StartRound()
    {
        int bunCount = bunCounts[difficultyLevel];
        DropZoneManager.Instance.StartDropPhase(bunCount);
        bunSpawner.SpawnBuns(bunCount);
    }

    void EvaluateResult(bool correctOrder, float timeTaken)
    {
        bool success = correctOrder;

// Only enforce time limit on levels 0 and 1
if (difficultyLevel < 2 && timeTaken > timeLimits[difficultyLevel])
{
    success = false;
}

        Debug.Log($"Result: {(success ? "CORRECT" : "WRONG")} in {timeTaken:F1}s");

        if (success && difficultyLevel < 2)
            difficultyLevel++;
        else if (!success && difficultyLevel == 2)
            difficultyLevel--;

        StartCoroutine(TransitionToNextRound());

        if (!success)
        {
            DropZoneManager.Instance.FlashErrorZone();
        }


    }

    private IEnumerator TransitionToNextRound()
    {
        yield return new WaitForSeconds(0.5f); // small delay to let the last bun finish snapping

        DropZoneManager.Instance.ClearBin();

        yield return new WaitForSeconds(0.6f); // let the buns fall

        StartRound(); // spawn next batch
    }



}
