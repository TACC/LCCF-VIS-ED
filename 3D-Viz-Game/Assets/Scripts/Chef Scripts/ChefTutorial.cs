using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ChefTutorial : MonoBehaviour
{
    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialCanvas;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private GameObject tutorialPanel;

    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;

    [Header("Button Sprites")]
    [SerializeField] private Sprite nextButtonSprite;
    [SerializeField] private Sprite startButtonSprite;

    [Header("Burger Game")]
    [SerializeField] private BurgerIngredientSpawner burgerSpawner;

    [SerializeField] private GameObject chefRoot;

    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI countdownText;


    private int currentPage = 0;

    private readonly string[] tutorialPages =
    {
        "Hey there, I'm Chef Jojo! Welcome to your first day as a chef! See the ingredients on the order ticket? Each one has a spedific number for how much of it goes onto the burger.",

        "To build the burgers, you will need to drag the correct amount for each ingredient onto the plate, making sure it matches the order ticket.",

        "Once you place an ingredient on the plate and press Next, it will be locked in place.",

        "Once every ingredient is placed, hit Submit! Red means that an ingredient didn't match the ticket amount, and it will explode. Green means you got it right and the burger will slide away!"

    };

    void Start()
    {
        StartTutorial();
    }

    private void StartTutorial()
    {
        currentPage = 0;

        tutorialCanvas.SetActive(true);

        UpdatePage();
    }

    public void NextPage()
    {
        if (currentPage < tutorialPages.Length - 1)
        {
            currentPage++;
            UpdatePage();
        }
        else
        {
            FinishTutorial();
        }
    }

    public void PreviousPage()
    {
        if (currentPage <= 0)
            return;

        currentPage--;

        UpdatePage();
    }

    private void UpdatePage()
    {
        tutorialText.text = tutorialPages[currentPage];

        backButton.interactable = currentPage > 0;

        bool isLastPage =
            currentPage == tutorialPages.Length - 1;

        Image buttonImage = nextButton.GetComponent<Image>();

        if (buttonImage != null)
        {
            buttonImage.sprite = isLastPage
                ? startButtonSprite
                : nextButtonSprite;
        }
    }

    public void FinishTutorial()
    {
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        // Hide tutorial instructions
        tutorialPanel.SetActive(false);

        // Show countdown
        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.75f);

        // Hide countdown
        countdownText.gameObject.SetActive(false);

        // Start the actual burger game
        burgerSpawner.BeginChefGame();
    }
}
