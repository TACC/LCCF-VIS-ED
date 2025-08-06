using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class BurgerIngredientSpawner : MonoBehaviour
{
    [Header("Ingredient Prefabs (Order Matters)")]
    public GameObject[] foodPrefabs;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("UI")]
    public Button nextButton;
    public TextMeshProUGUI[] orderTextSlots;

    [Header("Build Sequence (Index into foodPrefabs)")]
    public int[] ingredientSequence = { 0, 1, 2, 3, 4, 5 };

    [SerializeField] private BurgerStackManager stackManager;

    [Header("Plate System")]
    [SerializeField] private GameObject plateGroup; // Current plate + bottom bun in scene
    [SerializeField] private GameObject newPlateGroupPrefab; // Prefab to spawn
    [SerializeField] private Transform spawnPointLeft;
    [SerializeField] private Transform finalPlatePosition;

    private int currentStep = 0;
    private List<GameObject> activeFoodItems = new List<GameObject>();
    private List<int> correctNumbers = new List<int>();
    private bool hasPlacedItem = false;

    private string[] ingredientNames = { "Lettuce", "Meat", "Cheese", "Tomato", "Onions", "Bun" };

    void Start()
    {
        GenerateFullOrder();
        DisplayFullOrder();
        SpawnRound();
    }

    public void SpawnRound()
    {
        ClearUnusedItems();

        if (currentStep >= ingredientSequence.Length)
        {
            Submit();
            return;
        }

        int prefabIndex = ingredientSequence[currentStep];
        GameObject prefabToUse = foodPrefabs[prefabIndex];

        int correctNumber = correctNumbers[currentStep];
        List<int> numberOptions = GenerateUniqueRandomNumbersExcluding(2, 1, 99, correctNumber);
        numberOptions.Insert(Random.Range(0, numberOptions.Count + 1), correctNumber);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject clone = Instantiate(prefabToUse, spawnPoints[i].position, prefabToUse.transform.rotation);

            var drag = clone.GetComponent<DragHandler3D>();
            if (drag != null)
            {
                drag.SetSpawnPoint(spawnPoints[i]);
                drag.MarkInStack(false);
            }

            SetNumberOnPrefab(clone, numberOptions[i]);
            activeFoodItems.Add(clone);
        }

        hasPlacedItem = false;

        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
                currentStep == ingredientSequence.Length - 1 ? "Submit" : "Next";
        }
    }

    private void GenerateFullOrder()
    {
        correctNumbers.Clear();
        for (int i = 0; i < ingredientSequence.Length; i++)
        {
            correctNumbers.Add(Random.Range(1, 100));
        }
    }

    private void DisplayFullOrder()
    {
        for (int i = 0; i < ingredientSequence.Length && i < orderTextSlots.Length; i++)
        {
            string name = i < ingredientNames.Length ? ingredientNames[i] : "???";
            orderTextSlots[i].text = $"{name}: {correctNumbers[i]}";
            orderTextSlots[i].color = Color.black;
        }
    }

    private List<int> GenerateUniqueRandomNumbersExcluding(int count, int min, int max, int exclude)
    {
        HashSet<int> numbers = new HashSet<int>();
        while (numbers.Count < count)
        {
            int num = Random.Range(min, max);
            if (num != exclude)
                numbers.Add(num);
        }
        return new List<int>(numbers);
    }

    private void SetNumberOnPrefab(GameObject obj, int number)
    {
        TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = number.ToString();
        }
    }

    public void ClearUnusedItems()
    {
        GameObject[] leftovers = GameObject.FindGameObjectsWithTag("InSpawn");
        foreach (GameObject item in leftovers)
        {
            Destroy(item);
        }
        activeFoodItems.Clear();
    }

    public void RemoveFromActiveItems(GameObject item)
    {
        if (activeFoodItems.Contains(item))
        {
            activeFoodItems.Remove(item);
        }
    }

    public void Next()
    {
        if (!hasPlacedItem)
        {
            Debug.Log("Must place an item before continuing.");
            return;
        }

        currentStep++;
        SpawnRound();
    }

    public void NotifyIngredientPlaced(GameObject placedItem)
    {
        hasPlacedItem = true;
        if (nextButton != null)
            nextButton.interactable = true;
    }

    public void Submit()
    {
        GameObject[] items = stackManager.GetStackedItems();

        if (items.Any(i => i == null))
        {
            Debug.LogWarning("Stack incomplete!");
            return;
        }

        bool isCorrect = true;

        for (int i = 0; i < items.Length; i++)
        {
            TextMeshProUGUI label = items[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) continue;

            int stackNumber = int.Parse(label.text);
            int correctNumber = correctNumbers[i];

            if (orderTextSlots.Length > i)
            {
                orderTextSlots[i].color = stackNumber == correctNumber ? Color.green : Color.red;
                if (stackNumber != correctNumber) isCorrect = false;
            }
        }

        if (isCorrect)
        {
            StartCoroutine(SlideBurgerOffScreen(Vector3.right));
        }
        else
        {
            StartCoroutine(ExplodeBurgerAndReset());
        }

        if (nextButton != null)
            nextButton.interactable = false;
    }

    private IEnumerator SlideBurgerOffScreen(Vector3 direction, float distance = 10f, float duration = 0.5f)
    {
        GameObject[] items = stackManager.GetStackedItems();
        Vector3[] startPositions = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
            startPositions[i] = items[i].transform.position;

        Vector3 plateStart = plateGroup.transform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    items[i].transform.position = Vector3.Lerp(startPositions[i], startPositions[i] + direction * distance, t);
            }

            plateGroup.transform.position = Vector3.Lerp(plateStart, plateStart + direction * distance, t);
            yield return null;
        }

        foreach (GameObject item in items)
        {
            if (item != null)
                Destroy(item);
        }

        Destroy(plateGroup);

        yield return StartCoroutine(SlideNewPlateOn());

        currentStep = 0;
        GenerateFullOrder();
        DisplayFullOrder();
        SpawnRound();
    }

    private IEnumerator ExplodeBurgerAndReset()
    {
        GameObject[] items = stackManager.GetStackedItems();
        float explosionForce = 12f;
        float explosionRadius = 3f;
        Vector3 explosionCenter = finalPlatePosition.position + Vector3.up;

        foreach (GameObject item in items)
        {
            if (item == null) continue;

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb == null) rb = item.AddComponent<Rigidbody>();

            rb.isKinematic = false; // 💥 this allows it to be affected by physics
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            rb.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, 0.5f, ForceMode.Impulse);
            rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse); // 🔁 adds spin
        }

        if (plateGroup != null)
        {
            Rigidbody rb = plateGroup.GetComponent<Rigidbody>();
            if (rb == null) rb = plateGroup.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            rb.AddExplosionForce(explosionForce * 0.8f, explosionCenter, explosionRadius, 0.5f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(1.2f);

        foreach (GameObject item in items)
        {
            if (item != null) Destroy(item);
        }

        if (plateGroup != null) Destroy(plateGroup);

        currentStep = 0;
        GenerateFullOrder();
        DisplayFullOrder();
        yield return StartCoroutine(SlideNewPlateOn());
        SpawnRound();
    }

    private IEnumerator SlideNewPlateOn()
    {
        Quaternion spawnRotation = Quaternion.Euler(-12.806f, 0f, 0f);
        GameObject newPlate = Instantiate(newPlateGroupPrefab, spawnPointLeft.position, spawnRotation);
        plateGroup = newPlate;

        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 start = newPlate.transform.position;
        Vector3 end = finalPlatePosition.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            newPlate.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        newPlate.transform.position = end;
    }
}
