using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CodeCheckGame : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Overlay Panel (assign inside prefab)")]
    [Tooltip("Drag the overlay panel (child GameObject) of this intercom prefab here.")]
    public GameObject overlayPanel;

    [Header("UI Elements - will be fetched from overlayPanel")]
    private InputField codeInputField;
    private Text taskText;
    private Button checkButton;

    [Header("Answer Feedback Images - Individual per Intercom")]
    public GameObject[] correctAnswerImages;
    public GameObject wrongAnswerImage;
    public float answerImageDisplayTime = 2f;

    [Header("Cooldown UI - Individual")]
    private GameObject interactCooldownCanvas;
    private Text interactCooldownText;

    [Header("Target Object")]
    public GameObject triggeredObject;

    [Header("Interaction Settings")]
    public float interactionDistance = 2f;

    [Header("Cooldown Settings")]
    public float interactCooldown = 5f;
    public float cooldownTimer = 0f;
    [HideInInspector] public bool isOnCooldown = false;

    public bool IsBeingInteractedWith { get; private set; } = false;

    private static List<int> usedTaskIndices = new List<int>();
    private static bool isTaskAssignmentInitialized = false;

    private static CodeCheckGame currentActiveIntercom = null;

    private GameManager gameManager;
    private GameObject playerObject;
    private PlayerMovement playerMovement;
    private HunterController hunterController;
    private InterCom linkedIntercom;
    private PhotonView photonView;

    private int intercomID = -1;

    private TaskManager.CodeTask assignedTask;
    private bool isTriggered = false;
    private string savedInput = "";
    private bool hasBeenSolved = false;
    private bool lastAnswerWasHunterTampered = false;
    private bool hasRegistered = false;

    private string networkSavedInput = "";
    private bool networkHasBeenSolved = false;
    private Color networkColor = Color.white;

    [Header("Minimap Integration")]
    public GameObject minimapIcon;
    private MiniMap miniMap;
    private bool minimapRevealed = false; // NEW: Track if minimap icon has been revealed

    void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if (photonView != null && photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
        {
            intercomID = (int)photonView.InstantiationData[0];
        }
        else if (intercomID == -1)
        {
            Vector3 pos = transform.position;
            intercomID = Mathf.RoundToInt(pos.x * 1000 + pos.y * 1000 + pos.z * 1000);
        }

        if (triggeredObject == null)
            triggeredObject = gameObject;
    }

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        miniMap = FindObjectOfType<MiniMap>();
        linkedIntercom = GetComponent<InterCom>();
        if (gameManager != null && gameManager.allCodeGames != null && !hasRegistered)
        {
            gameManager.RegisterCodeCheckGame(this);
            hasRegistered = true;
            Debug.Log($"[CodeCheckGame] ✓ {gameObject.name} registered immediately in Start");
        }
        // NEW: Hide minimap icon initially for survivors
        if (minimapIcon != null && gameManager != null)
        {
            if (gameManager.localPlayerIsSurvivor)
            {
                minimapIcon.SetActive(false);
            }
            else if (gameManager.localPlayerIsHunter)
            {
                // Hunter can see all intercom icons from the start
                minimapIcon.SetActive(true);
                minimapRevealed = true;
            }
        }
        if (!hasRegistered)
        {
            StartCoroutine(RegisterWithGameManagerRoutine());
        }
        if (overlayPanel == null)
        {
            Transform child = transform.Find("OverlayPanel");
            if (child != null)
            {
                overlayPanel = child.gameObject;
            }
        }

        if (overlayPanel != null)
        {
            FindUIComponentsInPanel();
            SetupUIEvents();
            overlayPanel.SetActive(false);
        }

        HideFeedbackImages();

        

        StartCoroutine(FindLocalPlayerRoutine());

        AssignDeterministicTask();
    }

    void HideFeedbackImages()
    {
        if (wrongAnswerImage != null)
            wrongAnswerImage.SetActive(false);

        if (correctAnswerImages != null)
        {
            foreach (var img in correctAnswerImages)
            {
                if (img != null) img.SetActive(false);
            }
        }
    }

    void FindUIComponentsInPanel()
    {
        if (overlayPanel == null) return;

        codeInputField = overlayPanel.GetComponentInChildren<InputField>();

        Transform tTask = overlayPanel.transform.Find("TaskText");
        if (tTask != null) taskText = tTask.GetComponent<Text>();
        if (taskText == null)
        {
            Text[] texts = overlayPanel.GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                if (t.name.ToLower().Contains("task") || t.name.ToLower().Contains("instruction"))
                {
                    taskText = t;
                    break;
                }
            }
            if (taskText == null && texts.Length > 0) taskText = texts[0];
        }

        Transform tBtn = overlayPanel.transform.Find("CheckButton");
        if (tBtn != null) checkButton = tBtn.GetComponent<Button>();
        if (checkButton == null)
        {
            Button[] buttons = overlayPanel.GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0) checkButton = buttons[0];
        }

        Transform correctParent = overlayPanel.transform.Find("CorrectAnswerImages");
        if (correctParent != null)
        {
            correctAnswerImages = new GameObject[correctParent.childCount];
            for (int i = 0; i < correctParent.childCount; i++)
            {
                correctAnswerImages[i] = correctParent.GetChild(i).gameObject;
            }
        }

        Transform wrongT = overlayPanel.transform.Find("WrongAnswerImage");
        if (wrongT != null) wrongAnswerImage = wrongT.gameObject;

        Transform cooldownT = overlayPanel.transform.Find("CooldownCanvas");
        if (cooldownT != null)
        {
            interactCooldownCanvas = cooldownT.gameObject;
            Transform ct = cooldownT.Find("CooldownText");
            if (ct != null) interactCooldownText = ct.GetComponent<Text>();
        }
    }

    void SetupUIEvents()
    {
        if (checkButton != null)
        {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(CheckAnswer);
        }

        if (codeInputField != null)
        {
            codeInputField.onValueChanged.RemoveAllListeners();
            codeInputField.onValueChanged.AddListener(OnInputChanged);
        }

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        if (interactCooldownCanvas != null)
            interactCooldownCanvas.SetActive(false);

        HideFeedbackImages();
    }

    IEnumerator RegisterWithGameManagerRoutine()
    {
        // CRITICAL FIX: Wait a bit before starting registration attempts
        yield return new WaitForSeconds(0.5f);

        int attempts = 0;
        int maxAttempts = 30; // Increased from 20

        while (!hasRegistered && attempts < maxAttempts)
        {
            attempts++;

            // Try to find GameManager
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            if (gameManager != null)
            {
                // CRITICAL: Verify GameManager's allCodeGames list exists
                if (gameManager.allCodeGames != null)
                {
                    gameManager.RegisterCodeCheckGame(this);
                    hasRegistered = true;
                    Debug.Log($"[CodeCheckGame] ✓ {gameObject.name} successfully registered on attempt {attempts}");
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[CodeCheckGame] {gameObject.name}: GameManager found but allCodeGames is null (attempt {attempts})");
                }
            }
            else
            {
                Debug.LogWarning($"[CodeCheckGame] {gameObject.name}: GameManager not found (attempt {attempts})");
            }

            // Wait longer between attempts
            yield return new WaitForSeconds(0.3f);
        }

        if (!hasRegistered)
        {
            
            Debug.LogError($"[CodeCheckGame] ❌ {gameObject.name} FAILED to register after {maxAttempts} attempts!");

            yield return new WaitForSeconds(1f);

            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();

            if (gameManager != null && gameManager.allCodeGames != null)
            {
                gameManager.RegisterCodeCheckGame(this);
                hasRegistered = true;
                Debug.Log($"[CodeCheckGame] ✓ {gameObject.name} registered on FINAL attempt!");
            }
        }
    }

    void OnEnable()
    {
        if (!hasRegistered)
        {
            // Try immediate registration first
            if (gameManager != null && gameManager.allCodeGames != null)
            {
                gameManager.RegisterCodeCheckGame(this);
                hasRegistered = true;
                Debug.Log($"[CodeCheckGame] ✓ {gameObject.name} registered immediately in OnEnable");
            }
            else
            {
                // Fall back to coroutine
                StartCoroutine(RegisterWithGameManagerRoutine());
            }
        }
    }

    void OnDestroy()
    {
        if (gameManager != null)
            gameManager.UnregisterCodeCheckGame(this);

        if (currentActiveIntercom == this) currentActiveIntercom = null;
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected && photonView != null && !photonView.IsMine)
        {
            savedInput = networkSavedInput;
            if (hasBeenSolved != networkHasBeenSolved)
            {
                hasBeenSolved = networkHasBeenSolved;
                if (hasBeenSolved && gameManager != null)
                    gameManager.RegisterCorrectObject(this);
            }
            SetObjectColor(networkColor);
        }

        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                if (interactCooldownCanvas != null) interactCooldownCanvas.SetActive(false);
            }
            else if (interactCooldownText != null)
            {
                interactCooldownText.text = $"{Mathf.CeilToInt(cooldownTimer)}";
            }
        }

        IsBeingInteractedWith = overlayPanel != null && overlayPanel.activeSelf && currentActiveIntercom == this;

        if (IsBeingInteractedWith)
        {
            if (!IsPlayerInRange())
            {
                Debug.Log($"[CodeCheckGame] Player moved away from {name}, closing panel");
                CloseCodePanel();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                CloseCodePanel();
        }

        if (playerObject != null && !hasBeenSolved && !isOnCooldown)
        {
            if (IsPlayerInRange())
            {
                if (Input.GetKeyDown(KeyCode.E) && !IsBeingInteractedWith)
                    OpenCodePanel();
            }
        }

        bool playerStunned = (playerMovement != null && playerMovement.isStunned);
        if (checkButton != null)
            checkButton.interactable = !playerStunned;
    }

    IEnumerator FindLocalPlayerRoutine()
    {
        while (playerObject == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in players)
            {
                PhotonView view = p.GetComponent<PhotonView>();
                if (PhotonNetwork.OfflineMode || view == null || view.IsMine)
                {
                    playerObject = p;
                    playerMovement = p.GetComponent<PlayerMovement>();
                    hunterController = p.GetComponent<HunterController>();
                    Debug.Log($"[CodeCheckGame] Local player found: {playerObject.name}");
                    break;
                }
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    private void OnInputChanged(string value)
    {
        if (currentActiveIntercom == this)
        {
            savedInput = value;
            if (PhotonNetwork.IsConnected && photonView != null)
                photonView.RPC("RPC_SyncInputField", RpcTarget.OthersBuffered, intercomID, value);
        }
    }

    [PunRPC]
    void RPC_SyncInputField(int id, string inputText)
    {
        if (id == intercomID)
        {
            savedInput = inputText;
            networkSavedInput = inputText;
        }
    }

    private bool IsPlayerInRange()
    {
        if (playerObject == null || triggeredObject == null) return false;
        float distance = Vector2.Distance(playerObject.transform.position, triggeredObject.transform.position);
        return distance <= interactionDistance;
    }

    public void OpenCodePanel()
    {
        Debug.Log($"[CodeCheckGame] OpenCodePanel called on {name}");

        if (currentActiveIntercom != null && currentActiveIntercom != this)
        {
            Debug.Log($"[CodeCheckGame] Another intercom is active: {currentActiveIntercom.name}");
            return;
        }

        if (isOnCooldown)
        {
            Debug.Log($"[CodeCheckGame] {name} is on cooldown");
            return;
        }

        if (!IsPlayerInRange())
        {
            Debug.Log($"[CodeCheckGame] Player not in range of {name}");
            return;
        }

        currentActiveIntercom = this;

        if (assignedTask == null)
            AssignDeterministicTask();

        if (codeInputField != null)
            codeInputField.text = string.IsNullOrEmpty(savedInput) ? assignedTask.startingInput : savedInput;

        if (taskText != null)
            taskText.text = assignedTask != null ? $"Task:\n{assignedTask.instruction}" : "No tasks available.";

        if (overlayPanel != null)
            overlayPanel.SetActive(true);

        isTriggered = true;

        if (playerMovement != null)
            playerMovement.LockMovement();

        if (gameManager != null)
            gameManager.StartCodeDebugging();

        // NEW: Reveal minimap icon when ANY survivor interacts (shared across all survivors)
        if (!hasBeenSolved && !minimapRevealed)
        {
            RevealOnMinimap();
        }

        Debug.Log($"[CodeCheckGame] Panel opened for {name}");
    }

    public void CloseCodePanel()
    {
        if (currentActiveIntercom != this) return;

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        isTriggered = false;

        if (playerMovement != null)
            playerMovement.UnlockMovement();

        currentActiveIntercom = null;

        Debug.Log($"[CodeCheckGame] Panel closed for {name}");
    }

    public void CheckAnswer()
    {
        if (currentActiveIntercom != this) return;
        if (!isTriggered || assignedTask == null) return;

        if (!IsPlayerInRange())
        {
            CloseCodePanel();
            return;
        }

        string userInput = (codeInputField != null) ? codeInputField.text.Trim() : savedInput;
        savedInput = userInput;

        bool isHunter = (hunterController != null);

        if (PhotonNetwork.IsConnected && photonView != null)
        {
            if (userInput == assignedTask.correctAnswer)
                photonView.RPC("RPC_MarkAsCorrect", RpcTarget.AllBuffered, intercomID, isHunter);
            else
                photonView.RPC("RPC_MarkAsWrong", RpcTarget.AllBuffered, intercomID, lastAnswerWasHunterTampered);
        }
        else
        {
            if (userInput == assignedTask.correctAnswer)
                MarkAsCorrect(isHunter);
            else
                MarkAsWrong(lastAnswerWasHunterTampered);
        }

        if (userInput == assignedTask.correctAnswer)
        {
            ShowCorrectAnswerImageLocal();
        }
        else
        {
            ShowWrongAnswerImageLocal();
        }

        CloseCodePanel();

        if (!string.IsNullOrEmpty(userInput) && !isOnCooldown)
            StartCoroutine(InteractCooldownRoutine());
    }

    [PunRPC]
    void RPC_MarkAsCorrect(int id, bool isHunter)
    {
        if (id == intercomID) MarkAsCorrect(isHunter);
    }

    void MarkAsCorrect(bool isHunter = false)
    {
        SetObjectColor(Color.green);
        networkColor = Color.green;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCorrectAnswer();

        if (gameManager != null)
        {
            if (isHunter && hasBeenSolved)
            {
                gameManager.UnregisterCorrectObject(this);
                hasBeenSolved = false;
                networkHasBeenSolved = false;
            }
            else if (!isHunter && !hasBeenSolved)
            {
                gameManager.RegisterCorrectObject(this);
                hasBeenSolved = true;
                networkHasBeenSolved = true;
            }

            gameManager.FinishCodeDebugging(true);
        }

        lastAnswerWasHunterTampered = false;
    }

    [PunRPC]
    void RPC_MarkAsWrong(int id, bool wasTampered)
    {
        if (id == intercomID) MarkAsWrong(wasTampered);
    }

    void MarkAsWrong(bool wasTampered)
    {
        SetObjectColor(Color.red);
        networkColor = Color.red;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWrongAnswer();

        if (gameManager != null)
        {
            gameManager.TakeDamage(wasTampered);
            gameManager.FinishCodeDebugging(false);
            gameManager.ApplyWrongCodePenalty();
        }

        lastAnswerWasHunterTampered = false;
    }

    private void ShowCorrectAnswerImageLocal()
    {
        if (correctAnswerImages != null && correctAnswerImages.Length > 0)
        {
            StartCoroutine(DisplayCorrectImageCoroutine());
        }
    }

    private void ShowWrongAnswerImageLocal()
    {
        if (wrongAnswerImage != null)
        {
            StartCoroutine(DisplayWrongImageCoroutine());
        }
    }

    private IEnumerator DisplayCorrectImageCoroutine()
    {
        int randomIndex = Random.Range(0, correctAnswerImages.Length);
        GameObject selectedImage = correctAnswerImages[randomIndex];

        if (selectedImage != null)
        {
            selectedImage.SetActive(true);
            yield return new WaitForSeconds(answerImageDisplayTime);
            selectedImage.SetActive(false);
        }
    }

    private IEnumerator DisplayWrongImageCoroutine()
    {
        if (wrongAnswerImage != null)
        {
            wrongAnswerImage.SetActive(true);
            yield return new WaitForSeconds(answerImageDisplayTime);
            wrongAnswerImage.SetActive(false);
        }
    }

    private IEnumerator InteractCooldownRoutine()
    {
        isOnCooldown = true;
        cooldownTimer = interactCooldown;

        if (interactCooldownCanvas != null)
            interactCooldownCanvas.SetActive(true);

        while (cooldownTimer > 0f)
        {
            if (interactCooldownText != null)
                interactCooldownText.text = $"{Mathf.CeilToInt(cooldownTimer)}";
            cooldownTimer -= Time.deltaTime;
            yield return null;
        }

        if (interactCooldownText != null)
            interactCooldownText.text = "";

        if (interactCooldownCanvas != null)
            interactCooldownCanvas.SetActive(false);

        isOnCooldown = false;
    }

    public void SetObjectColor(Color color)
    {
        if (triggeredObject == null) return;
        Renderer rend = triggeredObject.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = color;
    }

    public void TamperCode()
    {
        if (PhotonNetwork.IsConnected && photonView != null)
            photonView.RPC("RPC_TamperCode", RpcTarget.AllBuffered, intercomID);
        else
            ApplyTamper();
    }

    [PunRPC]
    void RPC_TamperCode(int id)
    {
        if (id == intercomID) ApplyTamper();
    }

    void ApplyTamper()
    {
        if (currentActiveIntercom == this)
        {
            if (codeInputField != null)
            {
                string currentText = codeInputField.text;
                if (!string.IsNullOrEmpty(currentText))
                {
                    System.Text.StringBuilder newText = new System.Text.StringBuilder(currentText);
                    int changes = Random.Range(1, 3);
                    for (int i = 0; i < changes; i++)
                    {
                        int action = Random.Range(0, 3);
                        int index = Random.Range(0, Mathf.Max(1, newText.Length));

                        if (action == 0)
                            newText[index % newText.Length] = GetRandomChar();
                        else if (action == 1)
                            newText.Insert(index, GetRandomChar());
                        else if (action == 2 && newText.Length > 1)
                            newText.Remove(index % newText.Length, 1);
                    }
                    codeInputField.text = newText.ToString();
                    savedInput = codeInputField.text;
                    networkSavedInput = savedInput;
                }
            }
        }

        SetObjectColor(Color.red);
        networkColor = Color.red;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCodeInterrupted();

        lastAnswerWasHunterTampered = true;

        if (gameManager != null)
            gameManager.RecordCodeInterrupted();

        if (hasBeenSolved)
        {
            hasBeenSolved = false;
            networkHasBeenSolved = false;
            if (gameManager != null)
                gameManager.UnregisterCorrectObject(this);
        }
    }

    private char GetRandomChar()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return chars[Random.Range(0, chars.Length)];
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(savedInput);
            stream.SendNext(hasBeenSolved);
            stream.SendNext(networkColor.r);
            stream.SendNext(networkColor.g);
            stream.SendNext(networkColor.b);
            stream.SendNext(minimapRevealed); // NEW: Sync minimap reveal state
        }
        else
        {
            networkSavedInput = (string)stream.ReceiveNext();
            networkHasBeenSolved = (bool)stream.ReceiveNext();
            float r = (float)stream.ReceiveNext();
            float g = (float)stream.ReceiveNext();
            float b = (float)stream.ReceiveNext();
            networkColor = new Color(r, g, b);
            bool wasRevealed = (bool)stream.ReceiveNext();

            // NEW: Update minimap icon if reveal state changed
            if (wasRevealed && !minimapRevealed)
            {
                RevealOnMinimap();
            }
        }
    }

    public int GetIntercomID() => intercomID;
    public bool IsSolved() => hasBeenSolved;

    private void AssignDeterministicTask()
    {
        if (TaskManager.Instance == null)
        {
            Debug.LogWarning($"[CodeCheckGame] {gameObject.name}: TaskManager not found! Using fallback task.");
            CreateFallbackTask();
            return;
        }

        var allTasks = TaskManager.Instance.allTasks;
        if (allTasks == null || allTasks.Count == 0)
        {
            Debug.LogWarning($"[CodeCheckGame] {gameObject.name}: No tasks available in TaskManager! Using fallback.");
            CreateFallbackTask();
            return;
        }

        if (!isTaskAssignmentInitialized)
        {
            usedTaskIndices.Clear();
            isTaskAssignmentInitialized = true;
            Debug.Log("[CodeCheckGame] Task assignment system initialized - ensuring NO REPEATING tasks");
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < allTasks.Count; i++)
            if (!usedTaskIndices.Contains(i)) availableIndices.Add(i);

        if (availableIndices.Count == 0)
        {
            usedTaskIndices.Clear();
            for (int i = 0; i < allTasks.Count; i++) availableIndices.Add(i);
        }

        UnityEngine.Random.State original = UnityEngine.Random.state;
        UnityEngine.Random.InitState(intercomID);
        int randomIndex = UnityEngine.Random.Range(0, availableIndices.Count);
        int selectedIndex = availableIndices[randomIndex];
        UnityEngine.Random.state = original;

        usedTaskIndices.Add(selectedIndex);
        assignedTask = allTasks[selectedIndex];

        Debug.Log($"[CodeCheckGame] {gameObject.name}: Assigned task #{selectedIndex} - '{assignedTask.instruction}'");
    }

    private void CreateFallbackTask()
    {
        assignedTask = new TaskManager.CodeTask();
        assignedTask.instruction = "Type 'run' to execute the program";
        assignedTask.correctAnswer = "run";
        assignedTask.startingInput = "";
    }

    public static void ResetTaskAssignment()
    {
        usedTaskIndices.Clear();
        isTaskAssignmentInitialized = false;
        currentActiveIntercom = null;
        Debug.Log("[CodeCheckGame] Task assignment system reset for new game");
    }

    // NEW: Reveal minimap icon for ALL survivors when ANY survivor interacts
    public void RevealOnMinimap()
    {
        if (minimapRevealed) return; // Already revealed

        minimapRevealed = true;

        // For survivors: reveal the icon
        if (gameManager != null && gameManager.localPlayerIsSurvivor)
        {
            if (minimapIcon != null)
            {
                minimapIcon.SetActive(true);
                Debug.Log($"[CodeCheckGame] {name}: Minimap icon revealed for survivor.");
            }
        }

        // Sync reveal state across network
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("RPC_RevealMinimapIcon", RpcTarget.AllBuffered, intercomID);
        }
    }

    [PunRPC]
    void RPC_RevealMinimapIcon(int id)
    {
        if (id == intercomID && !minimapRevealed)
        {
            minimapRevealed = true;

            // Only reveal for survivors
            if (gameManager != null && gameManager.localPlayerIsSurvivor)
            {
                if (minimapIcon != null)
                {
                    minimapIcon.SetActive(true);
                    Debug.Log($"[CodeCheckGame] {name}: Minimap icon revealed via RPC for survivor.");
                }
            }
        }
    }

    public void SetInteractionState(bool active)
    {
        IsBeingInteractedWith = active;
        if (!active) triggeredObject = null;
    }

    public void SetTriggeredObject(GameObject obj)
    {
        triggeredObject = obj;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}