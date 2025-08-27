using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Dropdown Mapping (order must match 'correctValues' from NPC manager)")]
    public List<IngredientDropdownPair> ingredientDropdowns = new List<IngredientDropdownPair>();

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

    private List<string> correctValues;

    // Called by NPCOrderManager_Single with the correct answers for this round
    public void PopulateDropdowns(List<string> correct)
    {
        correctValues = correct;

        // Build distractors, avoid collisions only with NON-BLANK answers
        var nonBlankAnswers = new List<string>();
        foreach (var v in correctValues)
            if (!string.IsNullOrEmpty(v)) nonBlankAnswers.Add(v);

        var fakeValues = new List<string>
        {
            Random.Range(1, 61) + "g",
            Random.Range(1, 61) + "ml",
            Random.Range(1, 61) + "oz"
        };
        for (int i = 0; i < fakeValues.Count; i++)
        {
            while (nonBlankAnswers.Contains(fakeValues[i]))
            {
                string unit = fakeValues[i].EndsWith("g") ? "g" :
                              fakeValues[i].EndsWith("ml") ? "ml" : "oz";
                fakeValues[i] = Random.Range(1, 61) + unit;
            }
        }

        var answerOptions = new List<string>(nonBlankAnswers);
        answerOptions.AddRange(fakeValues);
        ShuffleList(answerOptions);

        // ✅ Always insert a single blank at index 0 for EVERY row and select it
        for (int i = 0; i < ingredientDropdowns.Count; i++)
        {
            var pair = ingredientDropdowns[i];
            if (!pair.dropdown) continue;

            var rowOptions = new List<string> { "" };   // single blank, first
            rowOptions.AddRange(answerOptions);         // then the real options

            pair.dropdown.ClearOptions();
            pair.dropdown.AddOptions(rowOptions);
            pair.dropdown.value = 0;                    // start on blank
            pair.dropdown.RefreshShownValue();

            SetRowVisible(pair, true);
            SetRowEditable(pair, true);
        }

        if (incorrectOverlay) incorrectOverlay.SetActive(false);
    }

    // Hook this to your Submit button
    public void CheckAnswers()
    {
        if (correctValues == null || correctValues.Count == 0)
        {
            Debug.LogWarning("OrderTicketUI: correctValues is empty; did NPC populate?");
            return;
        }

        var corrections = new List<string>();
        bool anyIncorrect = false;

        for (int i = 0; i < ingredientDropdowns.Count; i++)
        {
            if (i >= correctValues.Count)
            {
                anyIncorrect = true;
                continue;
            }

            var pair = ingredientDropdowns[i];
            if (!pair.dropdown)
            {
                anyIncorrect = true;
                continue;
            }

            string selected = (pair.dropdown.options.Count > pair.dropdown.value)
                ? pair.dropdown.options[pair.dropdown.value].text
                : "";

            string correct = correctValues[i]; // may be "" for omitted items
            bool isCorrect = (selected == correct);

            if (isCorrect)
            {
                if (hideCorrectRows)
                    SetRowVisible(pair, false);
                else
                    SetRowEditable(pair, false);
            }
            else
            {
                anyIncorrect = true;

                // Keep incorrect visible & editable
                SetRowVisible(pair, true);
                SetRowEditable(pair, true);

                if (string.IsNullOrEmpty(correct) && !string.IsNullOrEmpty(selected))
                {
                    // Omitted item but player entered something
                    corrections.Add("I did not ask for that.");
                }
                else
                {
                    string name = string.IsNullOrEmpty(pair.ingredientName) ? "that" : pair.ingredientName;
                    corrections.Add($"No, I said {correct} of {name}.");
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
            Debug.Log("[OrderTicketUI] Incorrect. Consider adding ShowCorrectionLines to legacy manager.");
        }
    }

    public void Retry()
    {
        if (incorrectOverlay) incorrectOverlay.SetActive(false);
    }

    private void SetRowVisible(IngredientDropdownPair pair, bool visible)
    {
        if (pair.rowRoot) pair.rowRoot.SetActive(visible);
        else if (pair.dropdown) pair.dropdown.gameObject.SetActive(visible);
    }

    private void SetRowEditable(IngredientDropdownPair pair, bool editable)
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
            retryButtonLabel.text = $"Retry ({countdown})";
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        retryButtonLabel.text = "Retry";
        retryButton.interactable = true;
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
