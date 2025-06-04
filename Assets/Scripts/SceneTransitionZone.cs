using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class SceneTransitionZone : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private string targetSceneName = "Level2";
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
    [SerializeField] private bool useSpawnPosition = true;
    
    [Header("Transition Behavior")]
    [SerializeField] private bool instantTransition = true; // New option for instant teleportation
    [SerializeField] private float transitionDelay = 0.0f; // Small delay before instant transition
    
    [Header("Visual Feedback")]
    [SerializeField] private TransitionPromptUI transitionPromptUI;
    [SerializeField] private string promptText = "Press E to enter";
    
    [Header("Transition Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;
    
    private bool playerInZone = false;
    private bool isTransitioning = false;
    private PlayerController playerController;
    private static SceneTransitionManager transitionManager;
    private Coroutine instantTransitionCoroutine;

    private void Start()
    {
        // Ensure the collider is set as trigger
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
        
        // Create transition manager if it doesn't exist
        if (transitionManager == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            transitionManager = managerObj.AddComponent<SceneTransitionManager>();
            DontDestroyOnLoad(managerObj);
        }
        
        // Only set up UI if not using instant transition
        if (!instantTransition)
        {
            // Find transition prompt UI if not assigned
            if (transitionPromptUI == null)
            {
                transitionPromptUI = FindObjectOfType<TransitionPromptUI>();
            }
            
            // Set up the prompt text
            if (transitionPromptUI != null)
            {
                transitionPromptUI.SetPromptText(promptText);
                transitionPromptUI.SetVisible(false, true);
            }
        }
    }

    private void Update()
    {
        // Only check for input if not using instant transition
        if (!instantTransition && playerInZone && !isTransitioning && playerController != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player entered the zone
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.IsOwner)
        {
            playerInZone = true;
            playerController = player;
            
            Debug.Log($"[SceneTransitionZone] Player entered transition zone to {targetSceneName}");
            
            if (instantTransition)
            {
                // Start instant transition with small delay
                if (instantTransitionCoroutine != null)
                {
                    StopCoroutine(instantTransitionCoroutine);
                }
                instantTransitionCoroutine = StartCoroutine(InstantTransitionWithDelay());
            }
            else
            {
                // Show transition prompt for manual transition
                if (transitionPromptUI != null)
                {
                    transitionPromptUI.SetPromptText(promptText);
                    transitionPromptUI.SetVisible(true);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the player left the zone
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.IsOwner)
        {
            playerInZone = false;
            playerController = null;
            
            Debug.Log($"[SceneTransitionZone] Player left transition zone");
            
            // Cancel instant transition if player leaves quickly
            if (instantTransitionCoroutine != null)
            {
                StopCoroutine(instantTransitionCoroutine);
                instantTransitionCoroutine = null;
            }
            
            // Hide transition prompt if using manual transition
            if (!instantTransition && transitionPromptUI != null)
            {
                transitionPromptUI.SetVisible(false);
            }
        }
    }

    private IEnumerator InstantTransitionWithDelay()
    {
        // Small delay to prevent accidental triggers when just passing through
        yield return new WaitForSeconds(transitionDelay);
        
        // Check if player is still in zone
        if (playerInZone && !isTransitioning)
        {
            StartTransition();
        }
    }

    private void StartTransition()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        Debug.Log($"[SceneTransitionZone] Starting transition to {targetSceneName}");
        
        // Cancel any pending instant transition
        if (instantTransitionCoroutine != null)
        {
            StopCoroutine(instantTransitionCoroutine);
            instantTransitionCoroutine = null;
        }
        
        // Hide the prompt if using manual transition
        if (!instantTransition && transitionPromptUI != null)
        {
            transitionPromptUI.SetVisible(false);
        }
        
        // Lock player movement
        if (playerController != null)
        {
            playerController.movementLocked.Value = true;
        }
        
        // Start the transition
        transitionManager.TransitionToScene(targetSceneName, playerSpawnPosition, useSpawnPosition);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the transition zone in the editor
        Gizmos.color = instantTransition ? Color.cyan : Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Draw spawn position if using it
        if (useSpawnPosition)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerSpawnPosition, 0.5f);
        }
        
        #if UNITY_EDITOR
        // Draw label for transition type
        Handles.Label(transform.position + Vector3.up * 2, 
            instantTransition ? "INSTANT TRANSITION" : "MANUAL TRANSITION (Press E)");
        #endif
    }
}

// Scene transition manager that handles the actual scene loading
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance => instance;
    
    [Header("Fade Settings")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public Color fadeColor = Color.black;
    
    private CanvasGroup fadeCanvasGroup;
    private GameObject fadeCanvas;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void CreateFadeCanvas()
    {
        // Create fade canvas
        fadeCanvas = new GameObject("FadeCanvas");
        fadeCanvas.transform.SetParent(transform);
        
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Ensure it's on top
        
        fadeCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        fadeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create fade panel
        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(fadeCanvas.transform);
        
        UnityEngine.UI.Image fadeImage = fadePanel.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        
        RectTransform rect = fadePanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        fadeCanvasGroup = fadePanel.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
    
    public void TransitionToScene(string sceneName, Vector3 spawnPosition, bool useSpawnPosition)
    {
        StartCoroutine(TransitionCoroutine(sceneName, spawnPosition, useSpawnPosition));
    }
    
    private IEnumerator TransitionCoroutine(string sceneName, Vector3 spawnPosition, bool useSpawnPosition)
    {
        Debug.Log($"[SceneTransitionManager] Starting transition to {sceneName}");
        
        // Fade in
        yield return StartCoroutine(FadeIn());
        
        // Load the new scene
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[SceneTransitionManager] Loading scene via NetworkManager: {sceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log($"[SceneTransitionManager] Loading scene directly: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        
        // Wait longer for scene to fully load
        yield return new WaitForSeconds(1.0f);
        
        // Move player to spawn position if specified
        if (useSpawnPosition)
        {
            // Try multiple times to find the player (in case of networking delays)
            for (int attempts = 0; attempts < 5; attempts++)
            {
                if (MovePlayerToSpawnPosition(spawnPosition))
                {
                    break; // Success!
                }
                yield return new WaitForSeconds(0.2f); // Wait a bit and try again
            }
        }
        
        // Additional wait to ensure everything is set up
        yield return new WaitForSeconds(0.3f);
        
        // Fade out
        yield return StartCoroutine(FadeOut());
        
        Debug.Log($"[SceneTransitionManager] Transition to {sceneName} completed");
    }
    
    private bool MovePlayerToSpawnPosition(Vector3 spawnPosition)
    {
        // Find the local player and move them to the spawn position
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.IsOwner)
            {
                player.transform.position = spawnPosition;
                player.movementLocked.Value = false; // Unlock movement after transition
                
                // Set up camera follow after transition
                SetupCameraFollow(player.transform);
                
                Debug.Log($"[SceneTransitionManager] Moved player to spawn position: {spawnPosition}");
                return true; // Success
            }
        }
        
        Debug.LogWarning("[SceneTransitionManager] Could not find local player to move to spawn position");
        return false; // Failed to find player
    }
    
    private void SetupCameraFollow(Transform playerTransform)
    {
        // Find the main camera in the new scene
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // If Camera.main doesn't work, find any camera
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera != null)
        {
            // Try to get the CameraFollow component
            CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
            
            if (cameraFollow != null)
            {
                // Set the camera to follow the player
                cameraFollow.target = playerTransform;
                Debug.Log($"[SceneTransitionManager] Camera follow set to player: {playerTransform.name}");
            }
            else
            {
                Debug.LogWarning("[SceneTransitionManager] CameraFollow component not found on main camera! Adding one...");
                
                // Add CameraFollow component if it doesn't exist
                cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
                cameraFollow.target = playerTransform;
                cameraFollow.smoothTime = 0.3f; // Default smooth time
                cameraFollow.offset = new Vector3(0f, 1f, -10f); // Default offset for 2D
                
                Debug.Log("[SceneTransitionManager] Added CameraFollow component and set target");
            }
        }
        else
        {
            Debug.LogError("[SceneTransitionManager] No camera found in the scene!");
        }
    }
    
    private IEnumerator FadeIn()
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
} 