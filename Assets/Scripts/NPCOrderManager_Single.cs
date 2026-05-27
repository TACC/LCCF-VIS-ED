using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCOrderManager_Single : MonoBehaviour
{
    [System.Serializable]
    public class Ingredient
    {
        public string name;      // e.g., "Lettuce"
        public string unit;      // e.g., "g", "ml", "oz"
        [HideInInspector] public int assignedValue;
    }

    [Header("Ingredient Setup")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("UI References")]
    public TextMeshProUGUI orderText;   // scrollable text content
    public ScrollRect scrollRect;       // optional (auto-scroll)
    public OrderTicketUI orderTicketUI;

    [Header("Typing Effect Settings")]
    public float typingSpeed = 0.03f;
    public float delayBetweenLines = 0.5f;

    [Header("NPC Hand-off")]
    public SingleNPCManager3D singleNpcManager;  // 3D single NPC flow
    public NPCManager legacy2DNpcManager;        // optional fallback

    private List<string> npcOrderLines = new List<string>();
    private string previousLines = "";           // accumulated text already shown

    void Start()
    {
        EnsureSessionAndMode();
        GenerateOrder();
    }

    private void EnsureSessionAndMode()
    {
        if (GameSession.Instance == null) return;
        if (!GameSession.Instance.SessionRunning)
            GameSession.Instance.StartSession(150); // ~2.5 minutes
        GameSession.Instance.SetMode(GameMode.Ordering);
    }

    public void GenerateOrder()
    {
        npcOrderLines.Clear();
        previousLines = "";
        if (orderText) orderText.text = "";

        var correctValues = new List<string>();

        // Decide how many ingredients to omit: 0..2 (clamped by available ingredients)
        int maxOmit = Mathf.Min(2, ingredients.Count);
        int omitCount = Random.Range(0, maxOmit + 1); // int Range is [min, max) => +1 to include max

        // Pick random unique indices to omit
        var indices = new List<int>();
        for (int i = 0; i < ingredients.Count; i++) indices.Add(i);
        ShuffleList(indices);
        var omitted = new HashSet<int>();
        for (int i = 0; i < omitCount; i++) omitted.Add(indices[i]);

        // Build order + answers (answers must match ticket row order)
        for (int i = 0; i < ingredients.Count; i++)
        {
            var ingredient = ingredients[i];

            if (omitted.Contains(i))
            {
                // Not mentioned in NPC lines; correct answer is BLANK
                correctValues.Add("");
                continue;
            }

            ingredient.assignedValue = Random.Range(1, 61); // 1..60
            string line = $"I'll take {ingredient.assignedValue}{ingredient.unit} of {ingredient.name}.";
            npcOrderLines.Add(line);

            correctValues.Add($"{ingredient.assignedValue}{ingredient.unit}");
        }

        // Shuffle only the SPOKEN lines for variety (ticket row order stays fixed)
        ShuffleList(npcOrderLines);

        // Populate the player's ticket
        if (orderTicketUI) orderTicketUI.PopulateDropdowns(correctValues);

        // Start a hidden task timer for THIS ticket (e.g., for bonus/penalty logic)
        if (GameSession.Instance != null) GameSession.Instance.StartTask();

        // Type out the order lines
        StopAllCoroutines();
        StartCoroutine(DisplayOrderLinesWithTyping());
    }

    private IEnumerator DisplayOrderLinesWithTyping()
    {
        yield return AppendLinesWithTyping(npcOrderLines, delayBetweenLines);
    }

    // Public: called by OrderTicketUI when player submits wrong answers
    public void ShowCorrectionLines(List<string> corrections)
    {
        if (corrections == null || corrections.Count == 0) return;

        // Clear previous NPC lines so the old order disappears (like when saying "Thanks!")
        StopAllCoroutines();
        previousLines = "";
        if (orderText) orderText.text = "";

        // Type only the correction lines
        StartCoroutine(AppendLinesWithTyping(corrections, delayBetweenLines));
    }

    // Public: called by OrderTicketUI on wrong attempt; do NOT end task
    public void RegisterWrongAttempt()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.AddPenalty(-10); // immediate -10; adjust to taste
        }
    }

    // Public: called by OrderTicketUI when all answers are correct
    public void ShowThanksAndReset()
    {
        if (GameSession.Instance != null) GameSession.Instance.CompleteTask(true);

        if (orderText) orderText.text = "Thanks!";
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;

        StartCoroutine(DelayThenSwap());
    }

    private IEnumerator DelayThenSwap()
    {
        yield return new WaitForSeconds(2f);

        if (singleNpcManager)        singleNpcManager.BeginNextRound(); // preferred 3D flow
        else if (legacy2DNpcManager) legacy2DNpcManager.SwapNPCs();     // legacy 2D flow
        else                         GenerateOrder();                   // fallback
    }

    // -----------------------
    // Shared typing helper
    // -----------------------
    private IEnumerator AppendLinesWithTyping(IEnumerable<string> lines, float perLineDelay)
    {
        foreach (var line in lines)
        {
            string currentLine = "";

            for (int i = 0; i < line.Length; i++)
            {
                currentLine += line[i];
                if (orderText) orderText.text = previousLines + currentLine;

                Canvas.ForceUpdateCanvases();
                if (scrollRect) scrollRect.verticalNormalizedPosition = 0f; // 0 = bottom

                yield return new WaitForSeconds(typingSpeed);
            }

            previousLines += currentLine + "\n\n";
            if (orderText) orderText.text = previousLines;

            Canvas.ForceUpdateCanvases();
            if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;

            yield return new WaitForSeconds(perLineDelay);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
