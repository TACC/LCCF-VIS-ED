using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditorInternal;
using UnityEngine.Video;

public class NPCOrderManager_Single : MonoBehaviour
{
    [System.Serializable]
    public class Ingredient
    {
        public string name;      // e.g., "Lettuce"
        public string unit;      // e.g., "g", "ml", "oz"
        [HideInInspector] public int assignedValue;

        public DragDrop draggableItem;
    }

    public float ordernumber = 1; // For single NPC flow, this can be set to 1 or more for multiple rounds

    [Header("Ingredient Setup")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("UI References")]
    public TextMeshProUGUI orderText;   // scrollable text content
    public ScrollRect scrollRect;       // optional (auto-scroll)
    public OrderTicketUI orderTicketUI;

    //CONNOR ADDED
    public OrderTicketUI orderTicketUIP2; //Phase 2

    public GameObject transitionPanelIn;  // optional UI panel for transitions (e.g., fade in/out)
    public GameObject transitionPanelOut; // optional UI panel for transitions (e.g., fade in/out)
    public GameObject videoPanel;

    public GameObject ordTicket1;
    public GameObject ordTicket2;

    public GameObject task;
    public Animator taskAnim;
    public Animator tickAnim;
    public Animator tickPAnim;

    public Animator textAnim;
    public GameObject npc;

    public GameObject countertop;

    public GameObject itemPool;

    //For bun answers only
    public int smallBun = 10;
    public int mediumBun = 20;
    public int largeBun = 30;

    //public int counterIngredient = 6;

    public bool bunChosen = false; // To ensure only one bun is chosen per order

    public bool phase2 = false; // Set to true to start directly in phase 2 (e.g., for testing)
    public bool taskOut = false; // Set to true to skip task animation (e.g., for testing)
    public VideoPlayer videoPlayer; // optional VideoPlayer for cutscenes or feedback

    [Header("Typing Effect Settings")]
    public float typingSpeed = 0.03f;
    public float delayBetweenLines = 0.5f;

    [Header("NPC Hand-off")]
    public SingleNPCManager3D singleNpcManager;  // 3D single NPC flow
    public NPCManager legacy2DNpcManager;        // optional fallback

    private List<string> npcOrderLines = new List<string>();
    private string previousLines = "";           // accumulated text already shown

    [SerializeField] private GameObject[] slotDrops; // Delay before showing task after order
    [SerializeField] private GameObject[] slotDropsP2; // Delay before showing task after order
    [SerializeField] private GameObject[] unitDrops;
    void Start()
    {
        EnsureSessionAndMode();
        //ordTicket2.SetActive(false);
        ordTicket1.SetActive(true);
        itemPool.SetActive(true); 
        resetSlots(slotDrops);

        bunChosen = false;
        countertop.SetActive(true);
        GenerateOrder();
    }

    private void EnsureSessionAndMode()
    {
        if (GameSession.Instance == null) return;
        if (!GameSession.Instance.SessionRunning)
            GameSession.Instance.StartSession(150); // ~2.5 minutes
        GameSession.Instance.SetMode(GameMode.Ordering);
    }

    //Dropdown for phase 1
    public void GenerateOrder()
    {
        if (!phase2)
        {
        bunChosen = false; // Reset bun choice for new order
        npcOrderLines.Clear();
        previousLines = "";
        if (orderText) orderText.text = "";

        foreach (var ing in ingredients)
            {
                if (ing.draggableItem != null)
                {
                    ing.draggableItem.ResetToPool();
                }
            }

        var correctValues = new List<string>();

        var mediumBunIng = ingredients[6];
        var largeBunIng = ingredients[7];

        int bunRoll = Random.Range(0, 3);
        ingredients[0].name = bunRoll == 0 ? "Small Bun" : bunRoll == 1 ? "Medium Bun" : "Large Bun";
        ingredients[0].assignedValue = bunRoll == 0 ? smallBun : bunRoll == 1 ? mediumBun : largeBun;
        ingredients.RemoveAt(7);
        ingredients.RemoveAt(6);

        // Decide how many ingredients to omit: 0..2 (clamped by available ingredients)
        int maxOmit = Mathf.Min(2, ingredients.Count - 2); // -2 to take out two of large, medium, or small
        int omitCount = Random.Range(0, maxOmit + 1); // int Range is [min, max) => +1 to include max

        // Pick random unique indices to omit
        //shuffle the list prior
        //then after choosing the first bun (large, medium, or small) we can skip rest of buns in
        var indices = new List<int>();
        for (int i = 0; i < ingredients.Count; i++) indices.Add(i);
        ShuffleList(indices);
        var omitted = new HashSet<int>();
        for (int i = 0; i < omitCount; i++) omitted.Add(indices[i]);


        
        // DO NOT RANDOMIZE INGREDIENTS
        // Build order + answers (answers must match ticket row order)
        for (int i = 0; i < ingredients.Count; i++)
        {
            var ingredient = ingredients[i];

            if (omitted.Contains(i))
            {
                // Not mentioned in NPC lines; correct answer is BLANK
                correctValues.Add("None");
                continue;
            }

            if (ingredient.name.ToLower().Contains("bun")) 
            {
                if (!bunChosen)
                    {
                        bunChosen = true;
                        string line = $"I'll take a {ingredient.name} ({ingredient.assignedValue}{ingredient.unit}).";
                        npcOrderLines.Add(line);
                        correctValues.Add($"{ingredient.assignedValue}{ingredient.unit}");
                        
                        if (ingredient.draggableItem != null)
                        {
                            ingredient.draggableItem.itemValue = $"{ingredient.assignedValue}{ingredient.unit}";
                            ingredient.draggableItem.ingredientName = "Bun";
                        }
                    }
                else
                    {
                        // If a bun has already been chosen, skip this one (treat as omitted)
                        correctValues.Add("None");
                        if (ingredient.draggableItem != null)
                        {
                            ingredient.draggableItem.itemValue = "None";
                            ingredient.draggableItem.ingredientName = "Bun";
                        }
                    }
                    
                continue;
                
            }
            else
            {
                ingredient.assignedValue = Random.Range(1, 61); // 1..60
                string line = $"I'll take {ingredient.assignedValue}{ingredient.unit} of {ingredient.name}.";
                npcOrderLines.Add(line);
            }
            

            correctValues.Add($"{ingredient.assignedValue}{ingredient.unit}");
            if (ingredient.draggableItem != null)
            {
                ingredient.draggableItem.itemValue = $"{ingredient.assignedValue}{ingredient.unit}";
                ingredient.draggableItem.ingredientName = ingredient.name;
            }
        }
        print($"Correct values for ticket: {string.Join(", ", correctValues)}");
        // Shuffle only the SPOKEN lines for variety (ticket row order stays fixed)
        ShuffleList(npcOrderLines);

        print($"Ingredients count: {ingredients.Count}, CorrectValues count: {correctValues.Count}");
        print($"Correct values for ticket: {string.Join(", ", correctValues)}");

        // Populate the player's ticket
        if (orderTicketUI) orderTicketUI.PopulateDropdowns(correctValues);

        // Start a hidden task timer for THIS ticket (e.g., for bonus/penalty logic)
        if (GameSession.Instance != null) GameSession.Instance.StartTask();

        // Type out the order lines
        StopAllCoroutines();
        StartCoroutine(DisplayOrderLinesWithTyping());
        ingredients.Add(mediumBunIng);
        ingredients.Add(largeBunIng);
        }
        else
        {
            
            GenerateOrderP2();
            resetSlots(unitDrops);
            resetSlots(slotDropsP2);
            
        }
        
    }

    //Dropdown for Phase 2
    public void GenerateOrderP2()
    {
        bunChosen = false; // Reset bun choice for new order
        print("Generating phase 2 order...");
        
        npcOrderLines.Clear();
        previousLines = "";
        if (orderText) orderText.text = "";

        var correctValues = new List<string>();
        var dataTypes = new List<string>();

        var mediumBunIng = ingredients[6];
        var largeBunIng = ingredients[7];
        int bunRoll = Random.Range(0, 3);
        ingredients[0].name = bunRoll == 0 ? "Small Bun" : bunRoll == 1 ? "Medium Bun" : "Large Bun";
        ingredients[0].assignedValue = bunRoll == 0 ? smallBun : bunRoll == 1 ? mediumBun : largeBun;
        ingredients.RemoveAt(7);
        ingredients.RemoveAt(6);

        // Decide how many ingredients to omit: 0..2 (clamped by available ingredients)
        int maxOmit = Mathf.Min(2, ingredients.Count - 2);
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
                // Not mentioned in NPC lines; correct answer is BLANK (Might switch to "None")
                correctValues.Add("None");
                dataTypes.Add("None");
                continue;
            }

            if (ingredient.name.ToLower().Contains("bun")) 
            {
                if (!bunChosen)
                    {
                        bunChosen = true;
                        string line = $"I'll take a {ingredient.name} ({ingredient.assignedValue}{ingredient.unit}).";
                        npcOrderLines.Add(line);
                        correctValues.Add($"{ingredient.assignedValue}");
                        dataTypes.Add($"{ingredient.unit}");
                    }
                else
                    {
                        // If a bun has already been chosen, skip this one (treat as omitted)
                        correctValues.Add("None");
                        dataTypes.Add("None");
                    }
                    
                continue;
                
            }
            else
            {
                ingredient.assignedValue = Random.Range(1, 61); // 1..60
                string line = $"I'll take {ingredient.assignedValue}{ingredient.unit} of {ingredient.name}.";
                npcOrderLines.Add(line);
                //Put them in two seperate drop downs
                correctValues.Add($"{ingredient.assignedValue}");
                dataTypes.Add($"{ingredient.unit}");
            }

            
        }

        // Shuffle only the SPOKEN lines for variety (ticket row order stays fixed)
        ShuffleList(npcOrderLines);

        //Debug.Log($"orderTicketUI = {orderTicketUI}");
        // Populate the player's ticket
        if (orderTicketUIP2)
        {
            //orderTicketUI.PopulateDropdowns(correctValues);
            Debug.Log($"Calling PopulateDropdownsUnits - dataTypes: {string.Join(", ", dataTypes)}, correctValues: {string.Join(", ", correctValues)}");
            orderTicketUIP2.PopulateDropdownsUnits(dataTypes, correctValues);
        } 

        // Start a hidden task timer for THIS ticket (e.g., for bonus/penalty logic)
        if (GameSession.Instance != null) GameSession.Instance.StartTask();

        // Type out the order lines
        StopAllCoroutines();
        StartCoroutine(DisplayOrderLinesWithTyping());

        ingredients.Add(mediumBunIng);
        ingredients.Add(largeBunIng);
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
        ordernumber--;

        StartCoroutine(DelayThenSwap());
    }

    //Connor edited
    private IEnumerator DelayThenSwap()
    {
        float animDelay = 2f;
        yield return new WaitForSeconds(animDelay);
        if (ordernumber > 0)
        {
            if (singleNpcManager)        singleNpcManager.BeginNextRound(); // preferred 3D flow
            else if (legacy2DNpcManager) legacy2DNpcManager.SwapNPCs();     // legacy 2D flow
            else                         GenerateOrder();
        }
        else if (!phase2)
        {
            // No more orders; could show a "closing time" message or transition to another scene
            if (orderText) orderText.text = "All done for today!";
            //task.SetActive(false);
            taskAnim.SetTrigger("anim2");
            tickAnim.SetTrigger("animT2");
            textAnim.SetTrigger("TextoutAnim");
            taskOut = false;
            // Transition logic and next game phase
            transitionPanelIn.SetActive(true);           
            //wait for anim to finish
            yield return new WaitForSeconds(animDelay);
            countertop.SetActive(false);
            ordTicket1.SetActive(false);
            videoPanel.SetActive(true);
            videoPlayer.Play();
            yield return new WaitForSeconds(animDelay);
            transitionPanelIn.SetActive(false);
            yield return new WaitForSeconds(16f);
            //transition to next part
            transitionPanelOut.SetActive(true);
            yield return new WaitForSeconds(animDelay);
            countertop.SetActive(true);
            videoPanel.SetActive(false);
            yield return new WaitForSeconds(animDelay);
            
            transitionPanelOut.SetActive(false);
            //Start 2nd phase
            //IF NOT SECOND TRUE THEN END
            secondPhase();
            
        }
        else
        {
            if (orderText) orderText.text = "Yay! (Standing end of phase2/Game)"; //Placeholder (Might have it leave screen with TextoutAnim)
            taskAnim.SetTrigger("anim2");
            tickPAnim.SetTrigger("animP2");
            // End video/transition
            //THIS IS THE CURRENT END OF GAME
        }
    }

    private void secondPhase()
    {
        //Set new parameter names and change UI for phase 2 of minigame
        npc.SetActive(false);
        ordernumber = 1;
        print("Second phase starting...");
        phase2 = true;
        bunChosen = false; // Reset bun choice for phase 2
        singleNpcManager.BeginNextRound();
    }

    // -----------------------
    // Shared typing helper
    // -----------------------
    private IEnumerator AppendLinesWithTyping(IEnumerable<string> lines, float perLineDelay)
    {
        
        if (!taskOut)
        {
            textAnim.SetTrigger("TextinAnim");
            taskAnim.SetTrigger("anim1");
            tickAnim.SetTrigger("animT1"); //first phase ticket anim
            if (phase2)
            {
                tickPAnim.SetTrigger("animP1"); //second phase anim
            } 
            taskOut = true;
        }
        foreach (var line in lines)
        {
            string currentLine = "";
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '<')
                {
                    int tagEnd = line.IndexOf('>', i);
                    if (tagEnd != -1)
                    {
                        string fullTag = line.Substring(i, tagEnd - i + 1);
                        currentLine += fullTag;
                        i = tagEnd + 1;
                        continue;
                    } 
                }
                currentLine += line[i];
                if (orderText) orderText.text = previousLines + currentLine;

                Canvas.ForceUpdateCanvases();
                if (scrollRect) scrollRect.verticalNormalizedPosition = 0f; // 0 = bottom

                yield return new WaitForSeconds(typingSpeed);
                i++;
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

    private void resetSlots(GameObject[] slotArray)
    {
        for (int i = 0; i < slotArray.Length; i++)
        {
            slotArray[i].SetActive(false);
        }

    }
}
