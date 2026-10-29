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
    public DishwashingUI ui;

    [Header("Round")]
    public float roundDuration = 20f;
    float timeLeft;
    int score;       // number of plates cleaned
    bool running;
    bool continueRound; //if the game should continue, for 2nd playthrough
    bool parallelized;

    void Start()
    {
        if (!dishline) dishline = Object.FindAnyObjectByType<DishlineController>();
        if (!ui) ui = Object.FindAnyObjectByType<DishwashingUI>();

        StartRound();
        continueRound = false;
        parallelized = false;
        score = 0;
    }

    public void StartRound()
    {
        // check for 2nd play through, increased spawning speed, more plates, and multiple parallel

        timeLeft = roundDuration;
        score = 0;
        running = true;

        parallelize.SetActive(false);
        if (dishline)
        {
            dishline.OnPlateStacked += HandlePlateStacked;
            dishline.StartSpawning();
        }

        ui.HideResults();
        //UpdateHUD();
    }

    public void EndRound()
    {
        //idea: 
        // 1. immediately take away control
        // 2. show animation of text "Time's Up!" emerging
        // 3. fade to black then back
        // 4. clear all plates on screen
        // 5. transition screen up

        if (!running) return;
        running = false;

        if (dishline)
        {
            dishline.OnPlateStacked -= HandlePlateStacked;
            dishline.StopSpawning();
        }

        //update variables

        Debug.Log("round over");
        //ShowResults();
        if (continueRound && !running)
        {
            continueRound = true;
            StartRound();
        }
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

        if (score >= 5 && !parallelized)
        {
            parallelize.SetActive(true);
        }

        //UpdateHUD();
    }

    void HandlePlateStacked(PlateController plate)
    {
        score++;
        //UpdateHUD();
    }
}
