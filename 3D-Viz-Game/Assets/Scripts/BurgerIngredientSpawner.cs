using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class BurgerIngredientSpawner : MonoBehaviour
{
    [Header("Burger Play Mode")]
    [SerializeField]
    private BurgerPlayMode burgerPlayMode =
    BurgerPlayMode.Guided;

    public BurgerPlayMode BurgerPlayMode => burgerPlayMode;
    [Header("Ingredient Prefabs (Order Matters)")]
    public GameObject[] foodPrefabs;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Full Build Spawn Points")]
    [SerializeField] private Transform[] fullBuildSpawnPoints;

    [Header("UI")]
    public Button nextButton;
    public TextMeshProUGUI[] orderTextSlots;

    [Header("Build Sequence (Index into foodPrefabs)")]
    public int[] ingredientSequence = { 0, 1, 2, 3, 4, 5 };

    [SerializeField] private BurgerStackManager stackManager;
    [Header("StationCamera")]
    [SerializeField] private Camera stationCamera;

    [Header("Plate System")]
    [SerializeField] private GameObject plateGroup;          // Current plate + bottom bun in scene
    [SerializeField] private GameObject newPlateGroupPrefab; // Prefab to spawn
    [SerializeField] private Transform spawnPointLeft;
    [SerializeField] private Transform finalPlatePosition;

    private int currentStep = 0;
    private List<GameObject> activeFoodItems = new List<GameObject>();
    private List<int> correctNumbers = new List<int>();
    private bool hasPlacedItem = false;
    private bool waitingForResultContinue = false;

    private string[] ingredientNames = { "Lettuce", "Meat", "Cheese", "Tomato", "Onions", "Bun" };

    public void BeginChefGame()
    {
        currentStep = 0;

        GenerateFullOrder();
        DisplayFullOrder();

        if (burgerPlayMode == BurgerPlayMode.Guided)
        {
            SpawnRound();
        }
        else
        {
            SpawnFullBuild();
        }
    }

    public void SpawnRound()
    {
        // Remove leftovers from last step
        ClearUnusedItems();

        if (currentStep >= ingredientSequence.Length)
        {
            Submit();
            return;
        }

        // ✅ Start per-burger timer only at the first step of a new burger
        if (currentStep == 0 && GameSession.Instance != null)
        {
            if (!GameSession.Instance.SessionRunning) GameSession.Instance.StartSession(150);
            GameSession.Instance.SetMode(GameMode.Burger);
            GameSession.Instance.StartTask();   // enables time bonus window
        }

        int prefabIndex = ingredientSequence[currentStep];
        GameObject prefabToUse = foodPrefabs[prefabIndex];

        int correctNumber = correctNumbers[currentStep];
        List<int> numberOptions = GenerateUniqueRandomNumbersExcluding(2, 1, 99, correctNumber);
        numberOptions.Insert(Random.Range(0, numberOptions.Count + 1), correctNumber);

        // Spawn choices for this step
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform sp = spawnPoints[i];

            GameObject clone = Instantiate(prefabToUse, sp.position, sp.rotation);

            // Snap to spawnPoint
            clone.transform.SetParent(sp, worldPositionStays: false);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = prefabToUse.transform.localScale;

            var drag = clone.GetComponent<DragHandler3D>();
            if (drag != null)
            {
                drag.mode = DragHandler3D.DragMode.BurgerBuild;
                drag.SetSpawnPoint(sp);
                drag.SetDragCamera(stationCamera); // ensure the DragHandler3D knows which camera to use for raycasting
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
            correctNumbers.Add(Random.Range(1, 100));
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
            if (num != exclude) numbers.Add(num);
        }
        return new List<int>(numbers);
    }

    private void SetNumberOnPrefab(GameObject obj, int number)
    {
        TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = number.ToString();
    }

    // --- Cleanup of previous options ---
    public void ClearUnusedItems()
    {
        foreach (var item in activeFoodItems)
            if (item != null) Destroy(item);
        activeFoodItems.Clear();
    }

    public void RemoveFromActiveItems(GameObject item)
    {
        if (activeFoodItems.Contains(item))
            activeFoodItems.Remove(item);
    }

    public void ReAddToActiveItems(GameObject item)
    {
        if (item == null) return;
        if (!activeFoodItems.Contains(item))
            activeFoodItems.Add(item);
    }

    public void Next()
    {

        // FULL BUILD:
        // The button checks the entire burger.
        if (burgerPlayMode == BurgerPlayMode.FullBuild)
        {
            Submit();
            return;
        }

        // GUIDED
        //if we're looking at the results of a wrong burger
        //Next means "continue" to the next burger, so we need to reset the state

        if (waitingForResultContinue)
        {
            waitingForResultContinue = false;

            if (nextButton != null)
                nextButton.interactable = false;

            StartCoroutine(ExplodeBurgerAndReset());
            return;
        }


        if (!hasPlacedItem)
        {
            Debug.Log("Must place an item before continuing.");
            return;
        }

        StrikeThroughCurrentIngredient();

        currentStep++;
        SpawnRound();
    }

    public void NotifyIngredientPlaced()
    {
        hasPlacedItem = true;
        if (nextButton != null) nextButton.interactable = true;
    }

    public void Submit()
    {
        GameObject[] items = stackManager.GetStackedItems();
        if (items.Any(i => i == null))
        {
            return;
        }

        bool isCorrect = true;

        for (int i = 0; i < items.Length; i++)
        {
            GameObject item = items[i];

            TextMeshProUGUI label =
                item.GetComponentInChildren<TextMeshProUGUI>(true);

            int stackNumber = -1;

            if (label != null)
                int.TryParse(label.text, out stackNumber);

            int correctNumber = correctNumbers[i];

            // Which prefab SHOULD be in this stack position?
            int expectedPrefabIndex = ingredientSequence[i];

            GameObject expectedPrefab =
                foodPrefabs[expectedPrefabIndex];

            string placedType =
                CleanIngredientName(item.name);

            string expectedType =
                CleanIngredientName(expectedPrefab.name);

            bool correctType =
                placedType == expectedType;

            bool correctValue =
                stackNumber == correctNumber;

            bool slotCorrect =
                correctType && correctValue;

            if (!slotCorrect)
                isCorrect = false;

            if (i < orderTextSlots.Length)
            {
                string name = ingredientNames[i];

                if (slotCorrect)
                {
                    orderTextSlots[i].text =
                        $"<color=green><s>{name}: {correctNumber}</s></color>";
                }
                else
                {
                    orderTextSlots[i].text =
                        $"<color=red><s>{name}: {correctNumber}</s></color>";
                }
            }
        }

        // Score here using your existing correctness result
        if (GameSession.Instance != null)
        {
            GameSession.Instance.SetMode(GameMode.Burger);   // safety
            GameSession.Instance.CompleteTask(isCorrect);    // +100 or −10 (+bonus if fast)
        }

        if (isCorrect)
        {
            if (nextButton != null) nextButton.interactable = false;

            StartCoroutine(AssembleCorrectBurger(items));
        }
        else
        {
            if (burgerPlayMode == BurgerPlayMode.FullBuild)
            {
                waitingForResultContinue = false;

                if (nextButton != null)
                    nextButton.interactable = true;

                return;
            }

            // Existing Guided behavior
            waitingForResultContinue = true;

            if (nextButton != null)
            {
                nextButton.interactable = true;

                TextMeshProUGUI buttonText =
                    nextButton.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                    buttonText.text = "Next";
            }
        }
    }

    private IEnumerator AssembleCorrectBurger(GameObject[] items)
    {
        Transform[] finalPositions = stackManager.GetFinalBurgerPositions();

        if (finalPositions == null ||
            finalPositions.Length < items.Length)
        {
            Debug.LogError("Final burger positions are not assigned.");
            yield break;
        }

        float duration = 0.5f;

        Vector3[] startPositions = new Vector3[items.Length];
        Quaternion[] startRotations = new Quaternion[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            startPositions[i] = items[i].transform.position;
            startRotations[i] = items[i].transform.rotation;

            DragHandler3D drag =
                items[i].GetComponent<DragHandler3D>();

            if (drag != null)
                drag.enabled = false;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;

                Transform target = finalPositions[i];

                items[i].transform.position = Vector3.Lerp(
                    startPositions[i],
                    target.position,
                    t
                );

                items[i].transform.rotation = Quaternion.Slerp(
                    startRotations[i],
                    target.rotation,
                    t
                );
            }

            yield return null;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            items[i].transform.position =
                finalPositions[i].position;

            items[i].transform.rotation =
                finalPositions[i].rotation;
        }
        yield return new WaitForSeconds(0.75f);

        yield return StartCoroutine(SlideBurgerOffScreen(Vector3.right));
    }
    private string CleanIngredientName(string raw)
    {
        return raw.Replace("(Clone)", "").Trim();
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
                if (items[i] != null)
                    items[i].transform.position = Vector3.Lerp(startPositions[i], startPositions[i] + direction * distance, t);

            plateGroup.transform.position = Vector3.Lerp(plateStart, plateStart + direction * distance, t);
            yield return null;
        }

        foreach (GameObject item in items)
            if (item != null) Destroy(item);

        Destroy(plateGroup);

        yield return StartCoroutine(SlideNewPlateOn());

        currentStep = 0;
        GenerateFullOrder();
        DisplayFullOrder();

        if (burgerPlayMode == BurgerPlayMode.FullBuild)
        {
            SpawnFullBuild();
        }
        else
        {
            SpawnRound();
        }
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
            Rigidbody rb = item.GetComponent<Rigidbody>() ?? item.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, 0.5f, ForceMode.Impulse);
            rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }

        if (plateGroup != null)
        {
            Rigidbody rb = plateGroup.GetComponent<Rigidbody>() ?? plateGroup.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.AddExplosionForce(explosionForce * 0.8f, explosionCenter, explosionRadius, 0.5f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(1.2f);

        foreach (GameObject item in items)
            if (item != null) Destroy(item);

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

    private void StrikeThroughCurrentIngredient()
    {
        if (orderTextSlots == null)
            return;

        if (currentStep < 0 || currentStep >= orderTextSlots.Length)
            return;

        TextMeshProUGUI orderText = orderTextSlots[currentStep];

        if (orderText == null)
            return;

        orderText.text = $"<s>{orderText.text}</s>";
    }

    private void SpawnFullBuild()
    {
        ClearUnusedItems();

        List<GameObject> prefabsToSpawn = new List<GameObject>();
        List<int> numbersToSpawn = new List<int>();

        for (int i = 0; i < ingredientSequence.Length; i++)
        {
            int prefabIndex = ingredientSequence[i];
            GameObject prefab = foodPrefabs[prefabIndex];

            int correctNumber = correctNumbers[i];

            // Add correct version
            prefabsToSpawn.Add(prefab);
            numbersToSpawn.Add(correctNumber);

            // Add two wrong versions
            List<int> wrongNumbers =
                GenerateUniqueRandomNumbersExcluding(
                    2,
                    1,
                    99,
                    correctNumber
                );

            prefabsToSpawn.Add(prefab);
            numbersToSpawn.Add(wrongNumbers[0]);

            prefabsToSpawn.Add(prefab);
            numbersToSpawn.Add(wrongNumbers[1]);
        }

        // Spawn them into the 18 FullBuild points
        for (int i = 0; i < fullBuildSpawnPoints.Length; i++)
        {
            if (i >= prefabsToSpawn.Count)
                break;

            Transform spawnPoint = fullBuildSpawnPoints[i];

            GameObject prefab = prefabsToSpawn[i];
            int number = numbersToSpawn[i];

            GameObject clone = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            clone.transform.SetParent(spawnPoint, false);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = prefab.transform.localRotation;

            clone.transform.localScale =
                prefab.transform.localScale;

            DragHandler3D drag =
                clone.GetComponent<DragHandler3D>();

            if (drag != null)
            {
                drag.mode = DragHandler3D.DragMode.BurgerBuild;
                drag.SetSpawnPoint(spawnPoint);
                drag.SetDragCamera(stationCamera);
                drag.MarkInStack(false);
            }

            SetNumberOnPrefab(clone, number);

            activeFoodItems.Add(clone);
        }
    }
}
