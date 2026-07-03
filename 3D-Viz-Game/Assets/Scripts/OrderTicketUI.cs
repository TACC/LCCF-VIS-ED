using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using NUnit.Framework.Constraints;

public class OrderTicketUI : MonoBehaviour
{
    [System.Serializable]
    public class IngredientDropdownPair
    {
        [Header("Row Wiring")]
        public string ingredientName;        // e.g., "Lettuce"
        public TMP_Dropdown dropdown;        // player's choice control

        [Tooltip("Container of the row (so we can hide it when correct). Optional.")]
        public GameObject rowRoot;

        [Tooltip("If provided, we can gray-out/lock instead of hiding.")]
        public CanvasGroup rowCanvasGroup;
    }

    [System.Serializable]
    public class UnitDropdownPair
    {
        [Header("Row Wiring")]
        public string UnitName;        // e.g., "g"
        public TMP_Dropdown dropdown;        // player's choice control

        [Tooltip("Container of the row (so we can hide it when correct). Optional.")]
        public GameObject rowRoot;

        [Tooltip("If provided, we can gray-out/lock instead of hiding.")]
        public CanvasGroup rowCanvasGroup;
    }

    [Header("Dropdown Mapping (order must match 'correctValues' from NPC manager)")]
    public List<IngredientDropdownPair> ingredientDropdowns = new List<IngredientDropdownPair>();
    //Phase 2
    public List<IngredientDropdownPair> phase2ItemDropdowns = new List<IngredientDropdownPair>();
    public List<UnitDropdownPair> dataTypeDropdowns = new List<UnitDropdownPair>();

    [Header("Legacy Incorrect Overlay (kept for compatibility; unused by default)")]
    public GameObject incorrectOverlay;
    public TMP_Text incorrectText;
    public Button retryButton;
    public TMP_Text retryButtonLabel;

    [Header("Order Managers (assign ONE)")]
    public NPCOrderManager npcOrderManager;                 // fallback/legacy
    public NPCOrderManager_Single npcOrderManagerSingle;    // recommended

    [Header("Behavior")]
    [Tooltip("If true: hide rows that are already correct. If false: keep visible but locked.")]
    public bool hideCorrectRows = true;

    //CONNOR ADDED THESE
    private List<string> correctValues;
    private List<string> correctUnitValues;
    private List<string> correctItemValues;

    

    [Header("Drop Handler")]
    [SerializeField] private DropHandler dropHandler;

    //For drag and drop
    private Dictionary<int, string> droppedAnswers = new Dictionary<int, string>();
    private Dictionary<int, string> droppedIngredientNames = new Dictionary<int, string>();
    
    //Claude
    private HashSet<int> lockedSlots = new HashSet<int>();

    // Claude addition *
    public bool isSlotLocked(int index)
    {
        return lockedSlots.Contains(index);
    }

    public void SetSlotLocked(int index, bool locked)
    {
        if (locked)
        {
            lockedSlots.Add(index);
        }
        else
        {
            lockedSlots.Remove(index);
        }
    }
    // *
    
    // Called by NPCOrderManager_Single with the correct answers for this round
    public void PopulateDropdowns(List<string> correct)
    {
        droppedAnswers.Clear();
        droppedIngredientNames.Clear();

        lockedSlots.Clear(); // Claude
        HideRows(); 
        correctValues = correct;
        if (dropHandler != null) dropHandler.ResetSlots();

        // Build distractors, avoid collisions only with NON-BLANK answers
        var nonBlankAnswers = new HashSet<string>();
        foreach (var v in correctValues)
            if (!string.IsNullOrEmpty(v)) nonBlankAnswers.Add(v);

        var fakeValues = new List<string>
        {
            Random.Range(1, 61) + "g",
            Random.Range(1, 61) + "ml",
            Random.Range(1, 61) + "oz"
        };
        //UNCOMMENT WHEN FINISHED
        // for (int i = 0; i < fakeValues.Count; i++)
        // {
        //     while (nonBlankAnswers.Contains(fakeValues[i]))
        //     {
        //         string unit = fakeValues[i].EndsWith("g") ? "g" :
        //                       fakeValues[i].EndsWith("ml") ? "ml" : "oz";
        //         fakeValues[i] = Random.Range(1, 61) + unit;
        //     }
        // }

        var answerOptions = new List<string>(nonBlankAnswers);
        
        //answerOptions.AddRange(fakeValues);
        ShuffleList(answerOptions);

        // ✅ Always insert a single blank at index 0 for EVERY row and select it
        for (int i = 0; i < ingredientDropdowns.Count; i++)
        {
            var pair = ingredientDropdowns[i];
            if (!pair.dropdown) continue;

            var rowOptions = new List<string> { "None" };   // single blank, first
            rowOptions.AddRange(answerOptions.Where(o => o != "None"));         // Might not need

            pair.dropdown.ClearOptions();
            pair.dropdown.AddOptions(rowOptions);
            pair.dropdown.value = 0;                    // start on blank
            pair.dropdown.RefreshShownValue();

            //SET THESE IN DragDrop
            //SetRowVisible(pair, true);
            //SetRowEditable(pair, true);
        }
        

        if (incorrectOverlay) incorrectOverlay.SetActive(false);
    }

     public void PopulateDropdownsUnits(List<string> correctUnits, List<string> correctItems)
    {
        Debug.Log($"PopulateDropdownsUnits called. Units: {correctUnits.Count}, Items: {correctItems.Count}");
        correctUnitValues = correctUnits;  
        correctItemValues = correctItems; 

        var uniqueUnits = new HashSet<string>();
        foreach (var unit in correctUnits)
        {
            if (!string.IsNullOrEmpty(unit)) uniqueUnits.Add(unit);
        }

        var unitOptions = new List<string>(uniqueUnits);
        ShuffleList(unitOptions);
        // --- Unit dropdowns ---
        // var nonBlankUnits = new List<string>();
        // foreach (var v in correctUnitValues)
        //     if (!string.IsNullOrEmpty(v)) nonBlankUnits.Add(v);

        // var unitOptions = new List<string>(nonBlankUnits);
        // ShuffleList(unitOptions);

        for (int i = 0; i < dataTypeDropdowns.Count; i++)
        {
            var pair = dataTypeDropdowns[i];
            if (!pair.dropdown) continue;

            var rowOptions = new List<string> { "None" }; //Possible placeholder
            rowOptions.AddRange(unitOptions.Where(o => o != "None"));

            pair.dropdown.ClearOptions();       // CRITICAL - clears "Option A/B/C"
            pair.dropdown.AddOptions(rowOptions);
            pair.dropdown.value = 0;
            pair.dropdown.RefreshShownValue();

            SetRowVisibleP2(pair, true);
            SetRowEditableP2(pair, true);
        }

        // --- Item dropdowns --- NEW
        var nonBlankItems = new HashSet<string>();
        foreach (var v in correctItemValues)
            if (!string.IsNullOrEmpty(v)) nonBlankItems.Add(v);

        var itemOptions = new List<string>(nonBlankItems);
        ShuffleList(itemOptions);

        for (int i = 0; i < phase2ItemDropdowns.Count; i++)
        {
            var pair = phase2ItemDropdowns[i];
            if (!pair.dropdown) continue;

            var rowOptions = new List<string> { "None" };
            rowOptions.AddRange(itemOptions.Where(o => o != "None"));

            pair.dropdown.ClearOptions();       // CRITICAL - clears "Option A/B/C"
            pair.dropdown.AddOptions(rowOptions);
            pair.dropdown.value = 0;
            pair.dropdown.RefreshShownValue();

            SetRowVisible(pair, true);
            SetRowEditable(pair, true);
        }

        if (incorrectOverlay) incorrectOverlay.SetActive(false);
    }

    // Hook this to your Submit button
    public void CheckAnswers()
    {
        Debug.Log("CHECKED");
        if (correctValues == null || correctValues.Count == 0)
        {
            Debug.LogWarning("OrderTicketUI: correctValues is empty; did NPC populate?");
            return;
        }

        var corrections = new List<string>();
        bool anyIncorrect = false;

        Debug.Log("Dropped Ingredients:");

        foreach (var kvp in droppedIngredientNames)
        {
            Debug.Log($"Slot {kvp.Key}: {kvp.Value}");
        }

        Debug.Log("Dropped Values:");

        foreach (var kvp in droppedAnswers)
        {
            Debug.Log($"Slot {kvp.Key}: {kvp.Value}");
        }

        for (int i = 0; i < ingredientDropdowns.Count; i++)
        {
            if (i >= correctValues.Count)
            {
                //anyIncorrect = true;
                continue;
            }

            var pair = ingredientDropdowns[i];
            if (!pair.dropdown)
            {
                anyIncorrect = true;
                continue;
            }

            string correct = correctValues[i]; // may be "" for omitted items
            print($"Checking slot {i}: {pair.ingredientName} (correct: {correct})");
            if (correct == "None")
            {
                //SetRowVisible(pair, false);
                // corrections.Add("I did not ask for that.");
                
                // continue;

                //Helped with Claude
                int wrongSlot = -1;
                foreach (var kvp in droppedIngredientNames)
                {
                    if (!string.IsNullOrEmpty(kvp.Value) && 
                    (kvp.Value.ToLower().Contains(pair.ingredientName.ToLower()) 
                    || pair.ingredientName.ToLower().Contains(kvp.Value.ToLower())))
                    {
                        wrongSlot = kvp.Key;
                        break;
                    }
                    
                }

                if (wrongSlot != -1)
                {
                    anyIncorrect = true;
                    var wrongSlotPair = ingredientDropdowns[wrongSlot];
                    SetRowVisible(wrongSlotPair, true);
                    SetRowEditable(wrongSlotPair, true);
                    corrections.Add($"I did not ask for <color=red><b>{pair.ingredientName}</b></color>.");
                    
                }
                continue;
            }

            int matchedSlot = -1;
            foreach (var kvp in droppedAnswers)
            {

                
                if (droppedIngredientNames.TryGetValue(kvp.Key, out string droppedName))
                {
                    Debug.Log($"Comparing '{pair.ingredientName}' to '{droppedName}'");
                    if (droppedName.ToLower().Contains(pair.ingredientName.ToLower())
                    || pair.ingredientName.ToLower().Contains(droppedName.ToLower()))
                    {
                        print(kvp.Key);
                        matchedSlot = kvp.Key;
                        
                        break;
                    }
                }
            }

            print(pair.ingredientName);
            if (matchedSlot == -1 && !pair.ingredientName.ToLower().Contains("bun"))
            {
                anyIncorrect = true;
                // SetRowVisible(pair, true);
                // SetRowVisible(pair, true);
                string missingName = string.IsNullOrEmpty(pair.ingredientName) ? "an ingredient" : pair.ingredientName;
                print("first");
                corrections.Add($"You forgot to add <color=red><b>{missingName} ({correct})</b></color=red>");
                continue;
            }

            var slotRow = ingredientDropdowns[matchedSlot];
            var slotDropdown = slotRow.dropdown;

            //var slotPair = ingredientDropdowns[matchedSlot].dropdown;
            string selected = (slotDropdown.options.Count > slotDropdown.value)
                ? slotDropdown.options[slotDropdown.value].text : "";

            
            //pair.dropdown = ingredientDropdowns[matchedSlot].dropdown;
            bool isCorrect = (selected == correct);

            if (isCorrect)
            {
                if (hideCorrectRows)
                {
                   SetRowVisible(slotRow, false);
                   SetSlotLocked(matchedSlot, true); // Claude 
                }
                else
                    SetRowEditable(slotRow, false);
            }
            else
            {
                anyIncorrect = true;

                // Keep incorrect visible & editable
                SetRowVisible(slotRow, true);
                SetRowEditable(slotRow, true);

                if (string.IsNullOrEmpty(correct) && selected != "None")
                {
                    // Omitted item but player entered something
                    corrections.Add("I did not ask for that.");
                }
                else
                {
                    string name = string.IsNullOrEmpty(pair.ingredientName) ? "that" : pair.ingredientName;
                    print (pair.ingredientName);
                    print (correct);
                    if (name.ToLower().Contains("bun"))
                    {
                        if (correct.Contains("10")) name = "Small Bun (10g)";
                        else if (correct.Contains("20")) name = "Medium Bun (20g)";
                        else if (correct.Contains("30")) name = "Large Bun (30g)";

                        corrections.Add($"No, I said a <color=red><b>{name}</b></color>.");
                    }
                    else
                    {
                        print("Second");
                        corrections.Add($"No, I said <color=red><b>{correct}</b></color> of <color=red><b>{name}</b></color>.");
                    }
                    
                }
            }
        }

        

        if (!anyIncorrect)
        {
            print("done");
            if (npcOrderManagerSingle) npcOrderManagerSingle.ShowThanksAndReset();
            else if (npcOrderManager) npcOrderManager.ShowThanksAndReset();
            else Debug.LogWarning("OrderTicketUI: No order manager assigned to receive success.");
            return;
        }

        if (npcOrderManagerSingle)
        {
            print("WHAT");
            npcOrderManagerSingle.ShowCorrectionLines(corrections);
            npcOrderManagerSingle.RegisterWrongAttempt();
        }
        else if (npcOrderManager)
        {
            Debug.Log("[OrderTicketUI] Incorrect. Consider adding ShowCorrectionLines to legacy manager.");
        }

        
    }

    public void CheckAnswersPhase2()
    {
        if ((correctUnitValues == null || correctUnitValues.Count == 0) &&
            (correctItemValues == null || correctItemValues.Count == 0))
        {
            Debug.LogWarning("OrderTicketUI: Phase 2 correctValues are empty; did NPC populate?");
            return;
        }

        var corrections = new List<string>();
        bool anyIncorrect = false;

        // Check unit dropdowns
        for (int i = 0; i < dataTypeDropdowns.Count; i++)
        {
            if (correctUnitValues == null || i >= correctUnitValues.Count) { continue; }

            var pair = dataTypeDropdowns[i];
            if (!pair.dropdown) { anyIncorrect = true; continue; }

            string selected = (pair.dropdown.options.Count > pair.dropdown.value)
                ? pair.dropdown.options[pair.dropdown.value].text : "";

            string correct = correctUnitValues[i];
            bool isCorrect = (selected == correct);

            if (isCorrect)
            {
                if (hideCorrectRows) SetRowVisibleP2(pair, false);
                else SetRowEditableP2(pair, false);
            }
            else
            {
                anyIncorrect = true;
                SetRowVisibleP2(pair, true);
                SetRowEditableP2(pair, true);

                if (string.IsNullOrEmpty(correct) && !string.IsNullOrEmpty(selected))
                    corrections.Add("I did not ask for that.");
                else
                {
                    string name = string.IsNullOrEmpty(pair.UnitName) ? "that unit" : pair.UnitName;
                    
                    print (pair.UnitName);
                    print (correct + "d"); //g, oz, none
                    if (name.ToLower().Contains("bun"))
                    {
                        // if (correct.Contains("10")) name = "Small Bun (10g)";
                        // else if (correct.Contains("20")) name = "Medium Bun (20g)";
                        // else if (correct.Contains("30")) name = "Large Bun (30g)";
                        //name = "Bun"
                       
                        corrections.Add($"The bun should be in {correct}."); //IRRELEVENT
                    }
                    else
                    {
                        corrections.Add($"No, the unit for <color=red><b>{name}</b></color> should be <color=red><b>{correct}</b></color>.");
                    }
                }
            }
        }

        // Check item dropdowns
        for (int i = 0; i < phase2ItemDropdowns.Count; i++)
        {
            if (correctItemValues == null || i >= correctItemValues.Count) { continue; }

            var pair = phase2ItemDropdowns[i];
            if (!pair.dropdown) { anyIncorrect = true; continue; }

            string selected = (pair.dropdown.options.Count > pair.dropdown.value)
                ? pair.dropdown.options[pair.dropdown.value].text : "";

            string correct = correctItemValues[i];
            bool isCorrect = (selected == correct);

            if (isCorrect)
            {
                if (hideCorrectRows) SetRowVisible(pair, false);
                else SetRowEditable(pair, false);
            }
            else
            {
                anyIncorrect = true;
                SetRowVisible(pair, true);
                SetRowEditable(pair, true);

                if (string.IsNullOrEmpty(correct) && !string.IsNullOrEmpty(selected))
                    corrections.Add("I did not ask for that.");
                else
                {
                    string name = string.IsNullOrEmpty(pair.ingredientName) ? "that" : pair.ingredientName;
                    print (pair.ingredientName);
                    print (correct);
                    if (name.ToLower().Contains("bun"))
                    {
                        if (correct.Contains("10")) name = "Small Bun (10g)";
                        else if (correct.Contains("20")) name = "Medium Bun (20g)";
                        else if (correct.Contains("30")) name = "Large Bun (30g)";

                        corrections.Add($"No, I said a <color=red><b>{name}</b></color>.");
                    }
                    else
                    {
                        corrections.Add($"No, I said <color=red><b>{correct}</b></color> of <color=red><b>{name}</b></color>.");
                    }
                }
            }
        }

        if (!anyIncorrect)
        {
            if (npcOrderManagerSingle) npcOrderManagerSingle.ShowThanksAndReset();
            else if (npcOrderManager)  npcOrderManager.ShowThanksAndReset();
            else Debug.LogWarning("OrderTicketUI: No order manager assigned to receive success.");
            return;
        }

        if (npcOrderManagerSingle)
        {
            npcOrderManagerSingle.ShowCorrectionLines(corrections);
            npcOrderManagerSingle.RegisterWrongAttempt();
        }
        else if (npcOrderManager)
        {
            Debug.Log("[OrderTicketUI] Incorrect.");
        }
    }

    public void Retry()
    {
        if (incorrectOverlay) incorrectOverlay.SetActive(false);
    }

    public void SetRowVisible(IngredientDropdownPair pair, bool visible)
    {
        if (pair.rowRoot) pair.rowRoot.SetActive(visible);
        else if (pair.dropdown) pair.dropdown.gameObject.SetActive(visible);
    }

    public void SetRowEditable(IngredientDropdownPair pair, bool editable)
    {
        if (pair.rowCanvasGroup)
        {
            pair.rowCanvasGroup.alpha = editable ? 1f : 0.4f;
            pair.rowCanvasGroup.interactable = editable;
            pair.rowCanvasGroup.blocksRaycasts = editable;
        }
        else if (pair.dropdown)
        {
            pair.dropdown.interactable = editable;
        }
    }

    //FOR PHASE 2
    private void SetRowVisibleP2(UnitDropdownPair pair, bool visible)
    {
        if (pair.rowRoot) pair.rowRoot.SetActive(visible);
        else if (pair.dropdown) pair.dropdown.gameObject.SetActive(visible);
    }

    private void SetRowEditableP2(UnitDropdownPair pair, bool editable)
    {
        if (pair.rowCanvasGroup)
        {
            pair.rowCanvasGroup.alpha = editable ? 1f : 0.4f;
            pair.rowCanvasGroup.interactable = editable;
            pair.rowCanvasGroup.blocksRaycasts = editable;
        }
        else if (pair.dropdown)
        {
            pair.dropdown.interactable = editable;
        }
    }

    private IEnumerator ShowIncorrectOverlayCountdown()
    {
        if (incorrectOverlay) incorrectOverlay.SetActive(true);
        if (retryButton) retryButton.interactable = false;

        int countdown = 3;
        while (countdown > 0)
        {
            if (retryButtonLabel) retryButtonLabel.text = $"Retry ({countdown})";
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        if (retryButtonLabel) retryButtonLabel.text = "Retry";
        if (retryButton) retryButton.interactable = true;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    public void RevealRow (int index)
    {
        if (index < 0 || index >= ingredientDropdowns.Count) return;

        var pair = ingredientDropdowns[index];
        SetRowVisible(pair, true);
        SetRowEditable(pair, true);

        if (pair.rowRoot)
        {
            var anim = pair.rowRoot.GetComponent<Animator>();
            if (anim) anim.SetTrigger("popDown");
        }
    }

    public void SetDropdownAnswer(int index, string value)
    {
        if (index < 0 || index >= ingredientDropdowns.Count) return;

        var pair = ingredientDropdowns[index];
        if (!pair.dropdown) return;

        for (int i = 0; i < pair.dropdown.options.Count; i++)
        {
            if (pair.dropdown.options[i].text == value)
            {
                pair.dropdown.value = i;
                pair.dropdown.RefreshShownValue();
                return;
            }
            
        }
    }

    
    public void RegisterDrop(int index, string value)
    {
        droppedAnswers[index] = value;
    }

    //Claude
    public void RegisterDropName(int index, string ingredientName)
    {
        droppedIngredientNames[index] = ingredientName;

        Debug.Log($"REGISTERED: slot {index} -> {ingredientName}");
    }

    //Mine
    public string getDropValue(int index)
    {
        return droppedAnswers[index];
    }

    public string getDropName(int index)
    {
        return droppedIngredientNames[index];
    }

    //Claude
    public int GetSelectedIndex(int index)
    {
        if (index < 0 || index >= ingredientDropdowns.Count) return 0;
        var pair = ingredientDropdowns[index];
        if (!pair.dropdown) return 0;
        return pair.dropdown.value;
    }

    public void SetDropdownIndex(int index, int dropdownIndex)
    {
        if (index < 0 || index >= ingredientDropdowns.Count) return;

        var pair = ingredientDropdowns[index];
        if (!pair.dropdown) return;

        if (dropdownIndex < 0 || dropdownIndex >= pair.dropdown.options.Count) return;

        pair.dropdown.value = dropdownIndex;
        pair.dropdown.RefreshShownValue();
    }

    public void HideRows()
    {
        foreach (var pair in ingredientDropdowns)
        {
            SetRowVisible(pair, false);
            SetRowEditable(pair, true);

            if (pair.dropdown)
            {
                pair.dropdown.value = 0;
                pair.dropdown.RefreshShownValue();
            }
        }
    }

}
