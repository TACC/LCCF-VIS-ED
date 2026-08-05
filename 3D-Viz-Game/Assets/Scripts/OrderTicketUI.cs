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

    //Part 2 Dictionaries
    private Dictionary<int, string> droppedUnitAnswers = new Dictionary<int, string>();
    private Dictionary<int, string> droppedUnitNames = new Dictionary<int, string>();
    private Dictionary<int, string> droppedItemAnswersP2 = new Dictionary<int, string>();
    private Dictionary<int, string> droppedItemNamesP2 = new Dictionary<int, string>();
    
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

        }
        

        if (incorrectOverlay) incorrectOverlay.SetActive(false);
    }

     public void PopulateDropdownsUnits(List<string> correctUnits, List<string> correctItems)
    {
        Debug.Log($"PopulateDropdownsUnits called. Units: {correctUnits.Count}, Items: {correctItems.Count}");

        droppedUnitAnswers.Clear();
        droppedUnitNames.Clear();
        droppedItemAnswersP2.Clear();
        droppedItemNamesP2.Clear();

        correctUnitValues = correctUnits;  
        correctItemValues = correctItems; 

        var uniqueUnits = new HashSet<string>();
        foreach (var unit in correctUnits)
        {
            if (!string.IsNullOrEmpty(unit)) uniqueUnits.Add(unit);
        }

        var unitOptions = new List<string>(uniqueUnits);
        ShuffleList(unitOptions);

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
                string missingName = string.IsNullOrEmpty(pair.ingredientName) ? "an ingredient" : pair.ingredientName;
                print("first");
                if (pair.ingredientName.ToLower().Contains("cheese"))
                {
                    print("WTH");
                }
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
        Debug.Log("checked phase 2");
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
        { //Claude version
            if (i >= correctUnitValues.Count) continue;

                var pair = dataTypeDropdowns[i];
                if (!pair.dropdown)
                {
                    anyIncorrect = true;
                    continue;
                }

                string correct = correctUnitValues[i];
                print(correct);
                if (correct == "None")
                {
                    int wrongSlot = -1;
                    foreach (var kvp in droppedUnitNames)
                    {
                        if (!string.IsNullOrEmpty(kvp.Value) &&
                            (kvp.Value.ToLower().Contains(pair.UnitName.ToLower())
                            || pair.UnitName.ToLower().Contains(kvp.Value.ToLower())))
                        {
                            wrongSlot = kvp.Key;
                            break;
                        }
                    }

                    if (wrongSlot != -1)
                    {
                        anyIncorrect = true;
                        var wrongSlotPair = dataTypeDropdowns[wrongSlot];
                        SetRowVisibleP2(wrongSlotPair, true);
                        SetRowEditableP2(wrongSlotPair, true);
                        corrections.Add($"I did not ask for <color=red><b>{pair.UnitName}</b></color>.");
                    }
                    continue;
                }

                int matchedSlot = -1;
                foreach (var kvp in droppedUnitAnswers)
                {
                    if (droppedUnitNames.TryGetValue(kvp.Key, out string droppedName))
                    {
                        if (droppedName.ToLower().Contains(pair.UnitName.ToLower())
                            || pair.UnitName.ToLower().Contains(droppedName.ToLower()))
                        {
                            matchedSlot = kvp.Key;
                            break;
                        }
                    }
                }
                
                if (matchedSlot == -1)
                {
                    anyIncorrect = true;
                    string missingName = string.IsNullOrEmpty(pair.UnitName) ? "a unit" : pair.UnitName;
                    corrections.Add($"You forgot to add <color=red><b>{missingName} ({correct})</b></color>");
                    continue;
                }

                var slotRow = dataTypeDropdowns[matchedSlot];
                var slotDropdown = slotRow.dropdown;

                string selected = (slotDropdown.options.Count > slotDropdown.value)
                    ? slotDropdown.options[slotDropdown.value].text : "";

                bool isCorrect = (selected == correct);

                if (isCorrect)
                {
                    if (hideCorrectRows)
                    {
                        SetRowVisibleP2(slotRow, false);
                        SetSlotLocked(matchedSlot, true);
                    }
                    else
                        SetRowEditableP2(slotRow, false);
                }
                else
                {
                    anyIncorrect = true;
                    SetRowVisibleP2(slotRow, true);
                    SetRowEditableP2(slotRow, true);

                    if (string.IsNullOrEmpty(correct) && selected != "None")
                    {
                        corrections.Add("I did not ask for that.");
                    }
                    else
                    {
                        string name = string.IsNullOrEmpty(pair.UnitName) ? "that unit" : pair.UnitName;
                        if (name.ToLower().Contains("bun"))
                        {
                            corrections.Add($"The bun should be in <color=red><b>{correct}</b></color>."); 
                        }
                        else
                        {
                            corrections.Add($"No, the unit for <color=red><b>{name}</b></color> should be <color=red><b>{correct}</b></color>.");
                        }
                    }
                }
            } //Claude version

            if (correctItemValues != null)
            { //Help from claude
                for (int i = 0; i < phase2ItemDropdowns.Count; i++)
                {
                    if (i >= correctItemValues.Count) continue;

                    var pair = phase2ItemDropdowns[i];
                    if (!pair.dropdown)
                    {
                        anyIncorrect = true;
                        continue;
                    }

                    string correct = correctItemValues[i];

                    if (correct == "None")
                    {
                        int wrongSlot = -1;
                        foreach (var kvp in droppedItemNamesP2)
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
                            var wrongSlotPair = phase2ItemDropdowns[wrongSlot];
                            SetRowVisible(wrongSlotPair, true);
                            SetRowEditable(wrongSlotPair, true);
                            corrections.Add($"I did not ask for <color=red><b>{pair.ingredientName}</b></color>.");
                        }
                        continue;
                    }

                    int matchedSlot = -1;
                    foreach (var kvp in droppedItemAnswersP2)
                    {
                        if (droppedItemNamesP2.TryGetValue(kvp.Key, out string droppedName))
                        {
                            if (droppedName.ToLower().Contains(pair.ingredientName.ToLower())
                                || pair.ingredientName.ToLower().Contains(droppedName.ToLower()))
                            {
                                matchedSlot = kvp.Key;
                                break;
                            }
                        }
                    }

                    if (matchedSlot == -1)
                    {
                        if (!pair.ingredientName.ToLower().Contains("bun"))
                        {
                            anyIncorrect = true;
                            string missingName = string.IsNullOrEmpty(pair.ingredientName) ? "an ingredient" : pair.ingredientName;
                            corrections.Add($"You forgot to add <color=red><b>{missingName} ({correct})</b></color>");
                            
                        }
                        continue;
                        
                    }

                    var slotRow = phase2ItemDropdowns[matchedSlot];
                    var slotDropdown = slotRow.dropdown;

                    string selected = (slotDropdown.options.Count > slotDropdown.value)
                        ? slotDropdown.options[slotDropdown.value].text : "";

                    bool isCorrect = (selected == correct);

                    if (isCorrect)
                    {
                        if (hideCorrectRows)
                        {
                            SetRowVisible(slotRow, false);
                            SetSlotLocked(matchedSlot, true);
                        }
                        else
                            SetRowEditable(slotRow, false);
                    }
                    else
                    {
                        anyIncorrect = true;
                        SetRowVisible(slotRow, true);
                        SetRowEditable(slotRow, true);

                        if (string.IsNullOrEmpty(correct) && selected != "None")
                        {
                            corrections.Add("I did not ask for that.");
                        }
                        else
                        {
                            string name = string.IsNullOrEmpty(pair.ingredientName) ? "that" : pair.ingredientName;
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
    public void SetRowVisibleP2(UnitDropdownPair pair, bool visible)
    {
        if (pair.rowRoot) pair.rowRoot.SetActive(visible);
        else if (pair.dropdown) pair.dropdown.gameObject.SetActive(visible);
    }

    public void SetRowEditableP2(UnitDropdownPair pair, bool editable)
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

    //Part 2 (Help from claude) NOT HAPPY WITH THIS APPROACH! TRY AND CHANGE TO BE MORE ABSTRACTED
    public void RegisterDropUnit(int index, string value)
    {
        droppedUnitAnswers[index] = value;
    }

    public void RegisterDropUnitName(int index, string value)
    {
        droppedUnitNames[index] = value;
    }

    public void RegisterDropItemP2(int index, string value)
    {
        droppedItemAnswersP2[index] = value;
    }

    public void RegisterDropItemNameP2(int index, string value)
    {
        droppedItemNamesP2[index] = value;
    }

    //Claude
    public int GetSelectedIndex(int index)
    {
        if (index < 0 || index >= ingredientDropdowns.Count) return 0;
        var pair = ingredientDropdowns[index];
        if (!pair.dropdown) return 0;
        return pair.dropdown.value;
    }

    //Claude
    public void SetDropdownIndex(int index, int dropdownIndex)
    {
        if (index < 0 || index >= ingredientDropdowns.Count) return;

        var pair = ingredientDropdowns[index];
        if (!pair.dropdown) return;

        if (dropdownIndex < 0 || dropdownIndex >= pair.dropdown.options.Count) return;

        pair.dropdown.value = dropdownIndex;
        pair.dropdown.RefreshShownValue();
    }


    //Mine
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

    //TESTING WITH CLAUDE
    public int GetSelectedIndexDataType(int index)
    {
        if (index < 0 || index >= dataTypeDropdowns.Count) return 0;
        var pair = dataTypeDropdowns[index];
        if (!pair.dropdown) return 0;
        return pair.dropdown.value;
    }

    public void SetDropdownIndexDataType(int index, int dropdownIndex)
    {
        if (index < 0 || index >= dataTypeDropdowns.Count) return;
        var pair = dataTypeDropdowns[index];
        if (!pair.dropdown) return;
        if (dropdownIndex < 0 || dropdownIndex >= pair.dropdown.options.Count) return;
        pair.dropdown.value = dropdownIndex;
        pair.dropdown.RefreshShownValue();
    }

    public int GetSelectedIndexP2Item(int index)
    {
        if (index < 0 || index >= phase2ItemDropdowns.Count) return 0;
        var pair = phase2ItemDropdowns[index];
        if (!pair.dropdown) return 0;
        return pair.dropdown.value;
    }

    public void SetDropdownIndexP2Item(int index, int dropdownIndex)
    {
        if (index < 0 || index >= phase2ItemDropdowns.Count) return;
        var pair = phase2ItemDropdowns[index];
        if (!pair.dropdown) return;
        if (dropdownIndex < 0 || dropdownIndex >= pair.dropdown.options.Count) return;
        pair.dropdown.value = dropdownIndex;
        pair.dropdown.RefreshShownValue();
    }

    public string getDropUnitValue(int index) => droppedUnitAnswers.TryGetValue(index, out var v) ? v : null;
    public string getDropUnitName(int index) => droppedUnitNames.TryGetValue(index, out var v) ? v : null;
    public string getDropItemValueP2(int index) => droppedItemAnswersP2.TryGetValue(index, out var v) ? v : null;
    public string getDropItemNameP2(int index) => droppedItemNamesP2.TryGetValue(index, out var v) ? v : null;

    

}
