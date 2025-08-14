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
        public string ingredientName;   // unused by logic; kept for your UI
        public TMP_Dropdown dropdown;
    }

    [Header("Dropdown Mapping")]
    public List<IngredientDropdownPair> ingredientDropdowns = new List<IngredientDropdownPair>();

    [Header("UI References")]
    public GameObject incorrectOverlay;   // grey overlay panel
    public TMP_Text incorrectText;        // "Incorrect!" label
    public Button retryButton;            // retry button
    public TMP_Text retryButtonLabel;     // retry button text

    [Header("Order Managers (assign ONE)")]
    public NPCOrderManager npcOrderManager;                 // original 2D manager
    public NPCOrderManager_Single npcOrderManagerSingle;    // new single-NPC manager

    private List<string> correctValues;

    public void PopulateDropdowns(List<string> correct)
    {
        correctValues = correct;

        // build 3 fake values with different units (same as before)
        List<string> fakeValues = new List<string>
        {
            Random.Range(1, 61) + "g",
            Random.Range(1, 61) + "ml",
            Random.Range(1, 61) + "oz"
        };

        // avoid collisions with real answers
        for (int i = 0; i < fakeValues.Count; i++)
        {
            while (correctValues.Contains(fakeValues[i]))
            {
                string unit = fakeValues[i].EndsWith("g") ? "g" :
                              fakeValues[i].EndsWith("ml") ? "ml" : "oz";
                fakeValues[i] = Random.Range(1, 61) + unit;
            }
        }

        // shuffle all options (blank first)
        List<string> answerOptions = new List<string>(correctValues);
        answerOptions.AddRange(fakeValues);
        ShuffleList(answerOptions);

        List<string> allOptions = new List<string> { "" }; // Option A = blank
        allOptions.AddRange(answerOptions);

        // apply to every dropdown row
        foreach (var pair in ingredientDropdowns)
        {
            if (pair.dropdown == null) continue;
            pair.dropdown.ClearOptions();
            pair.dropdown.AddOptions(allOptions);
            pair.dropdown.value = 0;
            pair.dropdown.RefreshShownValue();
        }
    }

    public void CheckAnswers()
    {
        // compare selections to correctValues by index (same as before)
        for (int i = 0; i < ingredientDropdowns.Count; i++)
        {
            var dd = ingredientDropdowns[i].dropdown;
            if (dd == null || dd.options.Count == 0) { StartCoroutine(ShowIncorrectOverlay()); return; }

            var selected = dd.options[dd.value].text;
            if (i >= correctValues.Count || selected != correctValues[i])
            {
                StartCoroutine(ShowIncorrectOverlay());
                return;
            }
        }

        // ✅ Correct — call whichever manager is assigned
        if (npcOrderManagerSingle != null) npcOrderManagerSingle.ShowThanksAndReset();
        else if (npcOrderManager != null)  npcOrderManager.ShowThanksAndReset();
        else Debug.LogWarning("OrderTicketUI: No order manager assigned to receive success.");
    }

    private IEnumerator ShowIncorrectOverlay()
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

    public void Retry()
    {
        if (incorrectOverlay) incorrectOverlay.SetActive(false);
        // keep current dropdown selections
    }

    private void ShuffleList(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
