using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Menu UI Elements")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Image backgroundImage;

    [Header("Menu Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float buttonHoverScale = 1.1f;
    [SerializeField] private float buttonTransitionSpeed = 10f;

    private bool isPaused = false;
    private PlayerController playerController;

    private void Awake()
    {
        Debug.Log("[PauseMenu] Awake called");
        // Singleton pattern
        if (Instance == null)
        {
            Debug.Log("[PauseMenu] Creating new instance");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ensure the pause menu is hidden at start
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
                Debug.Log("[PauseMenu] Pause menu panel hidden in Awake");
            }   
        }
        else
        {
            Debug.Log("[PauseMenu] Instance already exists, destroying duplicate");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Debug.Log("[PauseMenu] Start called");
        // Initialize button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
            Debug.Log("[PauseMenu] Resume button listener added");
        }
        else
        {
            Debug.LogError("[PauseMenu] Resume button not assigned!");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            Debug.Log("[PauseMenu] Main Menu button listener added");
        }
        else
        {
            Debug.LogError("[PauseMenu] Main Menu button not assigned!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("[PauseMenu] Quit button listener added");
        }
        else
        {
            Debug.LogError("[PauseMenu] Quit button not assigned!");
        }

        // Find the local player controller
        FindPlayerController();
    }

    private void OnEnable()
    {
        Debug.Log("[PauseMenu] OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        Debug.Log("[PauseMenu] OnDisable called");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PauseMenu] Scene loaded: {scene.name}");
        // Find the player controller in the new scene
        FindPlayerController();
        
        // Make sure the pause menu is hidden when loading a new scene
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Debug.Log("[PauseMenu] Pause menu panel hidden on scene load");
        }
        isPaused = false;
        Time.timeScale = 1f;
    }

    private void FindPlayerController()
    {
        playerController = FindObjectsOfType<PlayerController>().FirstOrDefault(pc => pc.IsOwner);
        Debug.Log($"[PauseMenu] Player controller found: {playerController != null}");
    }

    private void Update()
    {
        // Check for pause input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[PauseMenu] Escape key pressed");
            TogglePause();
        }
    }

    private void TogglePause()
    {
        Debug.Log($"[PauseMenu] TogglePause called. Current state: isPaused = {isPaused}");
        isPaused = !isPaused;
        
        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    private void PauseGame()
    {
        Debug.Log("[PauseMenu] Pausing game");
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Debug.Log("[PauseMenu] Pause menu panel shown");
        }

        // Pause the game
        Time.timeScale = 0f;

        // Lock player movement if we have a player controller
        if (playerController != null)
        {
            playerController.movementLocked.Value = true;
            Debug.Log("[PauseMenu] Player movement locked");
        }
        else
        {
            Debug.LogWarning("[PauseMenu] No player controller found to lock movement");
        }
    }

    private void ResumeGame()
    {
        Debug.Log("[PauseMenu] Resuming game");
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Debug.Log("[PauseMenu] Pause menu panel hidden");
        }

        // Resume the game
        Time.timeScale = 1f;

        // Unlock player movement if we have a player controller
        if (playerController != null)
        {
            playerController.movementLocked.Value = false;
            Debug.Log("[PauseMenu] Player movement unlocked");
        }
        else
        {
            Debug.LogWarning("[PauseMenu] No player controller found to unlock movement");
        }
    }

    private void ReturnToMainMenu()
    {
        Debug.Log("[PauseMenu] Returning to main menu");
        // Resume time scale before loading new scene
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void QuitGame()
    {
        Debug.Log("[PauseMenu] Quitting game");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Optional: Add hover effects for buttons
    public void OnButtonHover(Button button)
    {
        if (button != null)
        {
            button.transform.localScale = Vector3.one * buttonHoverScale;
        }
    }

    public void OnButtonExit(Button button)
    {
        if (button != null)
        {
            button.transform.localScale = Vector3.one;
        }
    }
} 