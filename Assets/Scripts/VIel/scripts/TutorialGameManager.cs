using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialGameManager : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject pencilObject;
    public GameObject player;

    [Header("Main Canvases")]
    public Canvas mainInteractionCanvas;
    public Canvas taskCanvas;
    public Canvas goodJobCanvas;
    public Canvas wrongCanvas;
    public Canvas bagCanvas;
    public Canvas pencilLessonCanvas;

    [Header("UI Elements - Main Interaction")]
    public Button acquireButton;
    public Button backButton;

    [Header("UI Elements - Task Canvas")]
    public InputField codeInputField;
    public Button runButton;
    public Button taskBackButton;
    public Text outputText;

    [Header("UI Elements - Success Canvas")]
    public Text successText;
    public Text completionTimeText;

    [Header("UI Elements - Bag Canvas")]
    public Button bagButton;
    public Button bagBackButton;
    public GameObject pencilButtonInBag;
    public Transform bagItemsParent;

    [Header("UI Elements - Pencil Lesson")]
    public Button pencilLessonCloseButton;

    [Header("Game Settings")]
    public float successDisplayTime = 3f;
    public float wrongDisplayTime = 2f;

    private bool isNearPencil = false;
    private bool hasPencil = false;
    private string correctCode = "name = input(\"What is your name? \")\nprint(\"Hello, \" + name)";
    private float taskStartTime;
    private List<string> inventory = new List<string>();

    void Start()
    {
        InitializeUI();
        SetupEventListeners();

        // Ensure multiline input
        codeInputField.lineType = InputField.LineType.MultiLineNewline;
    }

    void Update()
    {
        HandlePlayerInput();
    }

    void InitializeUI()
    {
        mainInteractionCanvas.gameObject.SetActive(false);
        taskCanvas.gameObject.SetActive(false);
        goodJobCanvas.gameObject.SetActive(false);
        wrongCanvas.gameObject.SetActive(false);
        bagCanvas.gameObject.SetActive(false);
        pencilLessonCanvas.gameObject.SetActive(false);

        if (pencilButtonInBag != null)
            pencilButtonInBag.SetActive(false);
    }

    void SetupEventListeners()
    {
        acquireButton.onClick.AddListener(ShowTaskCanvas);
        backButton.onClick.AddListener(HideMainInteractionCanvas);

        runButton.onClick.AddListener(CheckCode);
        taskBackButton.onClick.AddListener(HideTaskCanvas);

        bagButton.onClick.AddListener(ShowBagCanvas);
        bagBackButton.onClick.AddListener(HideBagCanvas);

        if (pencilButtonInBag != null)
        {
            Button pencilBtn = pencilButtonInBag.GetComponent<Button>();
            if (pencilBtn != null)
                pencilBtn.onClick.AddListener(ShowPencilLesson);
        }

        pencilLessonCloseButton.onClick.AddListener(HidePencilLesson);
    }

    void HandlePlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.J) && isNearPencil && !hasPencil)
        {
            ShowMainInteractionCanvas();
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isNearPencil = true;
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isNearPencil = false;
        }
    }

    void ShowMainInteractionCanvas()
    {
        mainInteractionCanvas.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    void HideMainInteractionCanvas()
    {
        mainInteractionCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    void ShowTaskCanvas()
    {
        mainInteractionCanvas.gameObject.SetActive(false);
        taskCanvas.gameObject.SetActive(true);
        taskStartTime = Time.realtimeSinceStartup;

        codeInputField.text = "";
        outputText.text = "";
    }

    void HideTaskCanvas()
    {
        taskCanvas.gameObject.SetActive(false);
        ShowMainInteractionCanvas();
    }

    void ShowBagCanvas()
    {
        bagCanvas.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    void HideBagCanvas()
    {
        bagCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    void ShowPencilLesson()
    {
        pencilLessonCanvas.gameObject.SetActive(true);
    }

    void HidePencilLesson()
    {
        pencilLessonCanvas.gameObject.SetActive(false);
    }

    // ✅ FIXED Code Validation Method
    void CheckCode()
    {
        string userInput = codeInputField.text;

        string normalizedExpected = NormalizeCode(correctCode);
        string normalizedUserInput = NormalizeCode(userInput);

        Debug.Log("=== CODE COMPARISON DEBUG ===");
        Debug.Log($"Normalized expected:\n{normalizedExpected}");
        Debug.Log($"Normalized user:\n{normalizedUserInput}");
        Debug.Log($"Are they equal? {normalizedUserInput == normalizedExpected}");

        string[] expectedLines = normalizedExpected.Split('\n');
        string[] userLines = normalizedUserInput.Split('\n');

        for (int i = 0; i < Mathf.Max(expectedLines.Length, userLines.Length); i++)
        {
            string expected = i < expectedLines.Length ? expectedLines[i] : "<none>";
            string user = i < userLines.Length ? userLines[i] : "<none>";
            Debug.Log($"Line {i + 1} - Expected: '{expected}' | User: '{user}'");
        }

        if (normalizedUserInput == normalizedExpected)
        {
            OnTaskSuccess();
        }
        else
        {
            OnTaskFailure();
        }
    }

    // ✅ Helper to normalize line endings and trim
    string NormalizeCode(string input)
    {
        return input
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();
    }

    void OnTaskSuccess()
    {
        float completionTime = Time.realtimeSinceStartup - taskStartTime;

        hasPencil = true;
        inventory.Add("Pencil");

        outputText.text = "What is your name?\nHello, Joy";

        successText.text = "PENCIL ACQUIRED!\nITEM ADDED IN YOUR BAG.";
        completionTimeText.text = "TASK COMPLETION TIME: " + completionTime.ToString("F2") + "s";

        goodJobCanvas.gameObject.SetActive(true);

        if (pencilObject != null)
            pencilObject.SetActive(false);

        if (pencilButtonInBag != null)
            pencilButtonInBag.SetActive(true);

        StartCoroutine(HideSuccessCanvasOnly());
    }

    void OnTaskFailure()
    {
        taskCanvas.gameObject.SetActive(false);
        wrongCanvas.gameObject.SetActive(true);
        StartCoroutine(HideWrongCanvasAfterDelay());
    }

    IEnumerator HideCanvasAfterDelay(GameObject canvas, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        canvas.SetActive(false);
        Time.timeScale = 1f;
    }

    IEnumerator HideSuccessCanvasOnly()
    {
        yield return new WaitForSecondsRealtime(successDisplayTime);
        goodJobCanvas.gameObject.SetActive(false);
    }

    IEnumerator HideWrongCanvasAfterDelay()
    {
        yield return new WaitForSecondsRealtime(wrongDisplayTime);
        wrongCanvas.gameObject.SetActive(false);
        ShowTaskCanvas();
    }

    public bool HasItem(string itemName)
    {
        return inventory.Contains(itemName);
    }

    public void AddItem(string itemName)
    {
        if (!inventory.Contains(itemName))
        {
            inventory.Add(itemName);
        }
    }

    public void RemoveItem(string itemName)
    {
        inventory.Remove(itemName);
    }

    public List<string> GetInventory()
    {
        return new List<string>(inventory);
    }
}

// ✅ PencilTrigger Script (unchanged)
[System.Serializable]
public class PencilTrigger : MonoBehaviour
{
    public TutorialGameManager gameManager;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameManager.OnTriggerEnter2D(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameManager.OnTriggerExit2D(other);
        }
    }
}
