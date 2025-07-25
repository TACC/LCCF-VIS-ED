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
        public string ingredientName;
        public TMP_Dropdown dropdown;
    }

    [Header("Dropdown Mapping")]
    public List<IngredientDropdownPair> ingredientDropdowns = new List<IngredientDropdownPair>();

    [Header("UI References")]
    public GameObject incorrectOverlay;         // Assign the grey overlay panel
    public TMP_Text incorrectText;              // Assign the "Incorrect!" label
    public Button retryButton;                  // Assign the retry button
    public TMP_Text retryButtonLabel;           // Assign the retry button's label
    public NPCOrderManager npcOrderManager;     // Reference to regenerate new orders

    private List<string> correctValues;

    public void PopulateDropdowns(List<string> correct)
    {
        correctValues = correct;

        List<string> fakeValues = new List<string>
        {
            Random.Range(1, 61) + "g",
            Random.Range(1, 61) + "ml",
            Random.Range(1, 61) + "oz"
        };

        for (int i = 0; i < fakeValues.Count; i++)
        {
            while (correctValues.Contains(fakeValues[i]))
            {
                string unit = fakeValues[i].EndsWith("g") ? "g" :
                              fakeValues[i].EndsWith("ml") ? "ml" : "oz";
                fakeValues[i] = Random.Range(1, 61) + unit;
            }
        }

        List<string> answerOptions = new List<string>(correctValues);
        answerOptions.AddRange(fakeValues);
        ShuffleList(answerOptions);

        List<string> allOptions = new List<string> { "" }; // Option A = blank
        allOptions.AddRange(answerOptions);

        foreach (var pair in ingredientDropdowns)
        {
            pair.dropdown.ClearOptions();
            pair.dropdown.AddOptions(allOptions);
            pair.dropdown.value = 0;
            pair.dropdown.RefreshShownValue();
        }
    }

    public void CheckAnswers()
    {
        for (int i = 0; i < ingredientDropdowns.Count; i++)
        {
            var selected = ingredientDropdowns[i].dropdown.options[ingredientDropdowns[i].dropdown.value].text;
            if (selected != correctValues[i])
            {
                StartCoroutine(ShowIncorrectOverlay());
                return;
            }
        }

        // ✅ Player got everything right
        npcOrderManager.ShowThanksAndReset();
    }

    private IEnumerator ShowIncorrectOverlay()
    {
        incorrectOverlay.SetActive(true);
        retryButton.interactable = false;

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

    public void Retry()
    {
        incorrectOverlay.SetActive(false);
        // dropdown values stay the same
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
