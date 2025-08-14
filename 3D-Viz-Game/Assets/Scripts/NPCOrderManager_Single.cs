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
        public string name;
        public string unit;
        [HideInInspector] public int assignedValue;
    }

    [Header("Ingredient Setup")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("UI References")]
    public TextMeshProUGUI orderText;
    public ScrollRect scrollRect;
    public OrderTicketUI orderTicketUI;   // same ticket script as before

    [Header("Typing Effect Settings")]
    public float typingSpeed = 0.03f;
    public float delayBetweenLines = 0.5f;

    // NPC links (new + legacy so you can use either)
    [Header("NPC Hand-off")]
    public SingleNPCManager3D singleNpcManager;  // 3D single NPC
    public NPCManager legacy2DNpcManager;        // optional fallback

    private List<string> npcOrderLines = new List<string>();
    private string previousLines = "";

    void Start()
    {
        GenerateOrder();
    }

    public void GenerateOrder()
    {
        npcOrderLines.Clear();
        previousLines = "";
        if (orderText) orderText.text = "";

        var correctValues = new List<string>();

        foreach (var ingredient in ingredients)
        {
            ingredient.assignedValue = Random.Range(1, 61); // 1..60
            string line = $"I'll take {ingredient.assignedValue}{ingredient.unit} of {ingredient.name}.";
            npcOrderLines.Add(line);

            // This is the contract the ticket expects: value + unit, in row order
            correctValues.Add($"{ingredient.assignedValue}{ingredient.unit}");
        }

        ShuffleList(npcOrderLines);
        if (orderTicketUI) orderTicketUI.PopulateDropdowns(correctValues);

        StopAllCoroutines();
        StartCoroutine(DisplayOrderLinesWithTyping());
    }

    private IEnumerator DisplayOrderLinesWithTyping()
    {
        foreach (string line in npcOrderLines)
        {
            string currentLine = "";

            for (int i = 0; i < line.Length; i++)
            {
                currentLine += line[i];
                if (orderText) orderText.text = previousLines + currentLine;

                Canvas.ForceUpdateCanvases();
                if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;

                yield return new WaitForSeconds(typingSpeed);
            }

            previousLines += currentLine + "\n\n";
            if (orderText) orderText.text = previousLines;

            Canvas.ForceUpdateCanvases();
            if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;

            yield return new WaitForSeconds(delayBetweenLines);
        }
    }

    private void ShuffleList(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    // Called by the ticket when answers are correct
    public void ShowThanksAndReset()
    {
        if (orderText) orderText.text = "Thanks!";
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
        StartCoroutine(DelayThenSwap());
    }

    private IEnumerator DelayThenSwap()
    {
        yield return new WaitForSeconds(2f);

        // Hand off to whichever NPC controller you’re using
        if (singleNpcManager) singleNpcManager.BeginNextRound();  // 3D single NPC
        else if (legacy2DNpcManager) legacy2DNpcManager.SwapNPCs(); // old 2D flow
        else GenerateOrder(); // fallback: just make a new order
    }
}
