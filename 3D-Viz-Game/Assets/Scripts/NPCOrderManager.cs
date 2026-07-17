using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCOrderManager : MonoBehaviour
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
    public OrderTicketUI orderTicketUI; // Reference to the ticket script

    [Header("Typing Effect Settings")]
    public float typingSpeed = 0.03f;
    public float delayBetweenLines = 0.5f;

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
        orderText.text = "";

        List<string> correctValues = new List<string>();

        foreach (var ingredient in ingredients)
        {
            ingredient.assignedValue = Random.Range(1, 61); // 1 to 60 inclusive
            string line = $"I'll take {ingredient.assignedValue}{ingredient.unit} of {ingredient.name}.";
            npcOrderLines.Add(line);

            // Collect correct value+unit for the dropdown
            correctValues.Add($"{ingredient.assignedValue}{ingredient.unit}");
            
        }

        ShuffleList(npcOrderLines);
        orderTicketUI.PopulateDropdowns(correctValues); // Send correct values to ticket

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
                orderText.text = previousLines + currentLine;

                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;

                yield return new WaitForSeconds(typingSpeed);
            }

            previousLines += currentLine + "\n\n";
            orderText.text = previousLines;

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;

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

    public NPCManager npcManager;

public void ShowThanksAndReset()
{
    orderText.text = "Thanks!";
    scrollRect.verticalNormalizedPosition = 1f;
    StartCoroutine(DelayThenSwap());
}

private IEnumerator DelayThenSwap()
{
    yield return new WaitForSeconds(2f);
    npcManager.SwapNPCs();
}


private IEnumerator ResetAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    GenerateOrder();
}

}
