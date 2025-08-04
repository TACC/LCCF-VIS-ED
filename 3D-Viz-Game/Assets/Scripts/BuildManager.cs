using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BuildManager : MonoBehaviour
{
    [System.Serializable]
    public class IngredientStep
    {
        public string ingredientType;
        public int correctNumber;
    }

    public static BuildManager Instance { get; private set; }

    [Header("Setup References")]
    public RectTransform dropZone;
    public Transform stackParent;
    public Transform plateStartPoint;
    public Transform plateEndPoint;
    public GameObject platePrefab;
    public GameObject bottomBunPrefab;

    [Header("Ingredient Prefabs")]
    public GameObject meatPrefab;
    public GameObject cheesePrefab;
    public GameObject ketchupPrefab;
    public GameObject mustardPrefab;
    public GameObject lettucePrefab;
    public GameObject tomatoPrefab;
    public GameObject topbunPrefab;

    [Header("UI")]
    public Transform ingredientSpawnPointsParent;
    public GameObject nextButton;

    public List<TextMeshProUGUI> ticketLines; // Assign in Inspector


    private int currentStepIndex = -1;
    private List<IngredientStep> orderSteps = new();
    private List<GameObject> chosenIngredients = new();
    private Dictionary<string, GameObject> stackedIngredients = new();

    private GameObject spawnedPlate;

    private Vector3 initialStackPosition;


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        initialStackPosition = stackParent.position;


        GenerateRandomOrder();
        UpdateOrderTicketUI();
        StartCoroutine(SlideInPlate());
    }

    void GenerateRandomOrder()
    {
        orderSteps.Clear();

        string[] ingredientOrder = {
            "meat", "cheese", "ketchup", "mustard", "lettuce", "tomato", "topbun"
        };

        foreach (string type in ingredientOrder)
        {
            orderSteps.Add(new IngredientStep
            {
                ingredientType = type,
                correctNumber = Random.Range(1, 6)
            });
        }
    }

    IEnumerator SlideInPlate()
    {
        spawnedPlate = Instantiate(platePrefab, plateStartPoint.position, Quaternion.identity, stackParent.parent);
        spawnedPlate.SetActive(true);
        spawnedPlate.transform.SetSiblingIndex(2);

        float t = 0;
        float duration = 0.5f;

        while (t < duration)
        {
            t += Time.deltaTime;
            spawnedPlate.transform.position = Vector3.Lerp(plateStartPoint.position, plateEndPoint.position, t / duration);
            yield return null;
        }

        // ✅ Bottom bun is parented to the stack
        GameObject bun = Instantiate(bottomBunPrefab, stackParent.position, Quaternion.identity, stackParent);
        bun.transform.localPosition = Vector3.zero;
        chosenIngredients.Add(bun);

        NextStep();
    }

    IEnumerator SlideBurgerOffScreen()
    {
        float duration = 2f;
        float t = 0;

        Vector3 plateStart = spawnedPlate.transform.position;
        Vector3 stackStart = stackParent.position;

        Vector3 plateEnd = plateStart + Vector3.right * 1000f;
        Vector3 stackEnd = stackStart + Vector3.right * 1000f;

        while (t < duration)
        {
            t += Time.deltaTime;
            spawnedPlate.transform.position = Vector3.Lerp(plateStart, plateEnd, t / duration);
            stackParent.position = Vector3.Lerp(stackStart, stackEnd, t / duration);
            yield return null;
        }

        Debug.Log("✅ Burger delivered!");

        yield return new WaitForSeconds(0.5f);
        ResetGame();
    }





    void UpdateOrderTicketUI()
    {
        for (int i = 0; i < orderSteps.Count; i++)
        {
            string label = orderSteps[i].ingredientType;

            // Format the label
            label = label.ToLower() == "topbun" ? "Bun" : char.ToUpper(label[0]) + label.Substring(1);

            int number = orderSteps[i].correctNumber;
            ticketLines[i].text = $"{label}: {number}";
            ticketLines[i].color = Color.black;
            ticketLines[i].fontSize = 35; // ✅ Adjust font size here
        }
    }


    public void NextStep()
    {
        currentStepIndex++;

        if (currentStepIndex >= orderSteps.Count)
        {
            GradeBurger();
            return;
        }

        ClearOldOptions();

        string type = orderSteps[currentStepIndex].ingredientType;
        int correctNumber = orderSteps[currentStepIndex].correctNumber;

        SpawnOptions(type, correctNumber);

        if (currentStepIndex == orderSteps.Count - 1)
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text = "Submit";
    }

    void SpawnOptions(string type, int correctNumber)
    {
        GameObject prefab = GetOptionPrefab(type);
        int[] numbers = GenerateThreeOptions(correctNumber);

        for (int i = 0; i < 3; i++)
        {
            Transform slot = ingredientSpawnPointsParent.GetChild(i);
            GameObject option = Instantiate(prefab, slot.position, Quaternion.identity, slot);


            var io = option.GetComponent<IngredientOption>();
            io.ingredientType = type;
            io.number = numbers[i];

            var label = option.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = numbers[i].ToString();
                label.gameObject.SetActive(true);
            }

            // Store spawn pos for return
            var drag = option.GetComponent<IngredientDragAndDrop>();
            drag.originalSpawnPos = option.GetComponent<RectTransform>().anchoredPosition;
        }
    }

    int[] GenerateThreeOptions(int correctNumber)
    {
        List<int> numbers = new() { correctNumber };

        while (numbers.Count < 3)
        {
            int n = Random.Range(1, 6);
            if (!numbers.Contains(n))
                numbers.Add(n);
        }

        for (int i = 0; i < numbers.Count; i++)
        {
            int randIndex = Random.Range(0, numbers.Count);
            (numbers[i], numbers[randIndex]) = (numbers[randIndex], numbers[i]);
        }

        return numbers.ToArray();
    }

    GameObject GetOptionPrefab(string type)
    {
        return type switch
        {
            "meat" => meatPrefab,
            "cheese" => cheesePrefab,
            "ketchup" => ketchupPrefab,
            "mustard" => mustardPrefab,
            "lettuce" => lettucePrefab,
            "tomato" => tomatoPrefab,
            "topbun" => topbunPrefab,
            _ => null
        };
    }

    public void IngredientChosen(GameObject ingredient)
    {
        var option = ingredient.GetComponent<IngredientOption>();
        string type = option.ingredientType;

        // Replace if already stacked
        if (stackedIngredients.ContainsKey(type))
        {
            GameObject old = stackedIngredients[type];
            ReturnIngredientToSpawn(old);
            stackedIngredients.Remove(type);
            chosenIngredients.Remove(old);
        }

        // Stack the new ingredient
        ingredient.transform.SetParent(stackParent, false);
        var rt = ingredient.GetComponent<RectTransform>();
        float offset = ingredient.GetComponent<IngredientOption>().stackHeightOffset;
        rt.anchoredPosition = new Vector2(0, offset * CurrentStackCount());


        stackedIngredients[type] = ingredient;
        chosenIngredients.Add(ingredient);

        var label = ingredient.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.gameObject.SetActive(false);
    }

    private void ReturnIngredientToSpawn(GameObject ingredient)
    {
        ingredient.transform.SetParent(ingredientSpawnPointsParent, false);
        var drag = ingredient.GetComponent<IngredientDragAndDrop>();
        ingredient.GetComponent<RectTransform>().anchoredPosition = drag.originalSpawnPos;
        drag.ResetPlacement();

        // ✅ Make number label visible again
        var label = ingredient.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.gameObject.SetActive(true);
    }



    public int CurrentStackCount()
    {
        return chosenIngredients.Count;
    }

    void ClearOldOptions()
    {
        foreach (Transform slot in ingredientSpawnPointsParent)
        {
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                Destroy(slot.GetChild(i).gameObject);
            }
        }
    }


    public void OnNextOrSubmit()
    {
        if (currentStepIndex < orderSteps.Count - 1)
        {
            NextStep(); // Still building
        }
        else
        {
            GradeBurger(); // Final step = submit
        }

    }


    void GradeBurger()
    {
        bool allCorrect = true;

        for (int i = 0; i < orderSteps.Count; i++)
        {
            var step = orderSteps[i];

            bool hasIngredient = stackedIngredients.ContainsKey(step.ingredientType);
            bool correct = false;

            if (hasIngredient)
            {
                var placed = stackedIngredients[step.ingredientType].GetComponent<IngredientOption>();
                correct = placed.number == step.correctNumber;
            }

            // Color the ticket line
            ticketLines[i].color = correct ? Color.green : Color.red;

            // Optional: DEBUG tag to confirm it’s the right line
            string label = step.ingredientType == "topbun" ? "Bun" : char.ToUpper(step.ingredientType[0]) + step.ingredientType.Substring(1);
            ticketLines[i].text = $"{label}: {step.correctNumber} {(correct ? "[OK]" : "[X]")}";

            if (!correct)
                allCorrect = false;
        }

        if (allCorrect)
        {
            StartCoroutine(SlideBurgerOffScreen());
        }
        else
        {
            StartCoroutine(DropBurgerOffScreen());
        }
    }

    IEnumerator DropBurgerOffScreen()
    {
        float duration = 1.5f;
        float t = 0;

        Vector3 start = stackParent.position;
        Vector3 end = start + Vector3.down * 1000f;

        while (t < duration)
        {
            t += Time.deltaTime;
            stackParent.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        ResetGame();

    }

    void ResetGame()
    {
        if (spawnedPlate != null)
        {
            Destroy(spawnedPlate);
            spawnedPlate = null;
        }

        foreach (Transform child in stackParent)
        {
            Destroy(child.gameObject);
        }

        chosenIngredients.Clear();
        stackedIngredients.Clear();

        stackParent.position = initialStackPosition; // ✅ Reset stack location

        foreach (var line in ticketLines)
        {
            line.text = "";
            line.color = Color.black;
        }

        currentStepIndex = -1;

        GenerateRandomOrder();
        UpdateOrderTicketUI();

        nextButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next";

        StartCoroutine(SlideInPlate());
    }

}
