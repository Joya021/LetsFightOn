using UnityEngine;
using UnityEngine.UI;

public class LessonPopup : MonoBehaviour
{
    public GameObject popupPanel;

    // Pages
    public GameObject page1;
    public GameObject page2;
    public GameObject page3;

    // Page 1 UI
    public Image learnableObjectImage;
    public Image closeButtonImage1;
    public Image lessonImage;
    public Image sampleCodeImage;

    // Page 2 UI
    public Image learnableObjectImage1;
    public Image closeButtonImage2;
    public Image brokenCodeImage;
    public InputField playerInputField;
    public Button runButton;
    public Text feedbackText;

    // Page 3 UI
    public Image learnableObjectImage2;
    public Image closeButtonImage3;
    public Image successMessageImage;
    public Text successMessageText;

    // Navigation buttons
    public GameObject backButtonObject;
    public GameObject nextButtonObject;

    private string correctAnswer;
    private string itemID;
    private Inventory inventory;
    private PlayerMovements playerMovement;

    public GameObject paperBG;

    private LearnableObject currentObject;

    private int currentPage = 1;

    void Start()
    {
        runButton.onClick.AddListener(CheckAnswer);
        closeButtonImage1.GetComponent<Button>().onClick.AddListener(HideLesson);
        closeButtonImage2.GetComponent<Button>().onClick.AddListener(HideLesson);
        closeButtonImage3.GetComponent<Button>().onClick.AddListener(HideLesson);
        backButtonObject.GetComponent<Button>().onClick.AddListener(GoToPreviousPage);
        nextButtonObject.GetComponent<Button>().onClick.AddListener(GoToNextPage);
    }

    public void ShowLesson(Sprite learnableObjectSprite, Sprite learnableObjectSprite1, Sprite learnableObjectSprite2, Sprite closeButtonSprite1, Sprite closeButtonSprite2, Sprite closeButtonSprite3, Sprite lessonSprite, Sprite sampleSprite, Sprite brokenSprite, Sprite successMessageSprite, string expectedFix, string itemID, Inventory inventoryRef, LearnableObject sourceObject)
    {
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovements>();

        if (playerMovement != null)
            playerMovement.canMove = false;

        popupPanel.SetActive(true);
        currentPage = 1;

        // Assign lesson content
        learnableObjectImage.sprite = learnableObjectSprite;
        learnableObjectImage1.sprite = learnableObjectSprite1;
        learnableObjectImage2.sprite = learnableObjectSprite2;
        closeButtonImage1.sprite = closeButtonSprite1;
        closeButtonImage2.sprite = closeButtonSprite2;
        closeButtonImage3.sprite = closeButtonSprite3;
        lessonImage.sprite = lessonSprite;
        sampleCodeImage.sprite = sampleSprite;
        brokenCodeImage.sprite = brokenSprite;
        successMessageImage.sprite = successMessageSprite;

        correctAnswer = expectedFix;
        this.itemID = itemID;
        this.inventory = inventoryRef;
        this.currentObject = sourceObject;

        playerInputField.text = "";
        feedbackText.text = "";

        UpdatePageView();
    }

    public void HideLesson()
    {
        popupPanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.canMove = true;
    }

    private void GoToNextPage()
    {
        if (currentPage == 1)
        {
            currentPage = 2;
            UpdatePageView();
        }
    }

    private void GoToPreviousPage()
    {
        if (currentPage == 2)
        {
            currentPage = 1;
            UpdatePageView();
        }
    }

    private void UpdatePageView()
    {
        page1.SetActive(currentPage == 1);
        page2.SetActive(currentPage == 2);
        page3.SetActive(currentPage == 3);

        // 👀 Show/hide navigation buttons
        bool showNavButtons = (currentPage == 1 || currentPage == 2);
        backButtonObject.SetActive(showNavButtons);
        nextButtonObject.SetActive(showNavButtons);

        // 🛑 Disable interactivity for Back/Next if needed
        if (showNavButtons)
        {
            backButtonObject.GetComponent<Button>().interactable = (currentPage != 1);
            nextButtonObject.GetComponent<Button>().interactable = (currentPage == 1);
        }

        // Toggle PaperBG visibility
        paperBG.SetActive(currentPage != 3); // Hide on success page
    }

    private void CheckAnswer()
    {
        string playerAnswer = playerInputField.text.Trim();

        if (playerAnswer == correctAnswer.Trim())
        {
            feedbackText.text = "";
            inventory.AddItem(currentObject);
            currentPage = 3;
            successMessageText.text = "Correct! Item added to inventory.";

            // Remove the object from the map
            if (currentObject != null)
            {
                Destroy(currentObject.gameObject);
            }
            UpdatePageView();
        }
        else
        {
            feedbackText.text = "❌ Not quite. Try again!";
        }
    }
    public void ShowLessonFromInventory(Sprite lessonSprite)
    {
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovements>();

        if (playerMovement != null)
            playerMovement.canMove = false;

        popupPanel.SetActive(true);
        currentPage = 1;

        lessonImage.sprite = lessonSprite;

        // Show only Page 1
        page1.SetActive(true);
        page2.SetActive(false);
        page3.SetActive(false);

        // Hide navigation buttons
        backButtonObject.SetActive(false);
        nextButtonObject.SetActive(false);

        // Show only close button for Page 1
        closeButtonImage1.gameObject.SetActive(true);
        closeButtonImage2.gameObject.SetActive(false);
        closeButtonImage3.gameObject.SetActive(false);

        paperBG.SetActive(true);
    }
}