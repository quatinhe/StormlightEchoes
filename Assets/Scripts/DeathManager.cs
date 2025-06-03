using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("Death UI Elements")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI deathText;
    [SerializeField] private Image fadeImage;

    [Header("Death Settings")]
    [SerializeField] private float deathDisplayDuration = 4f;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Color fadeColor = Color.black;

    private bool isDying = false;

    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetUI();
    }

    public void ResetUI()
    {
        Debug.Log("[DeathManager] Resetting UI");
        
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
            Debug.Log("[DeathManager] Death panel deactivated");
        }
        
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.gameObject.SetActive(false); // Also disable the fadeImage GameObject
            Debug.Log("[DeathManager] Fade image reset and deactivated");
        }
        
        // Reset the entire Canvas if it exists
        Canvas deathCanvas = GetComponentInParent<Canvas>();
        if (deathCanvas == null && deathPanel != null)
        {
            deathCanvas = deathPanel.GetComponentInParent<Canvas>();
        }
        if (deathCanvas != null)
        {
            deathCanvas.gameObject.SetActive(false);
            Debug.Log("[DeathManager] Death canvas deactivated");
            // Re-enable it but make sure it's not blocking
            deathCanvas.gameObject.SetActive(true);
            deathCanvas.sortingOrder = -100; // Send it to the back
        }
        
        isDying = false;
        Time.timeScale = 1f; // Ensure time scale is reset
        Debug.Log("[DeathManager] UI reset complete");
    }

    private void InitializeUI()
    {
        // Initialize UI
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[DeathManager] Death Panel not assigned!");
        }

        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }
        else
        {
            Debug.LogError("[DeathManager] Fade Image not assigned!");
        }
    }

    public void StartDeathSequence()
    {
        if (!isDying)
        {
            isDying = true;
            // Immediately set time scale to 0 to freeze the game
            Time.timeScale = 0f;
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log("[DeathManager] Starting death sequence");

        // Show death panel
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        // Fade in
        if (fadeImage != null)
        {
            float elapsedTime = 0f;
            Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }
        }

        // Wait for the specified duration (using unscaled time)
        float waitTime = 0f;
        while (waitTime < deathDisplayDuration)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        if (fadeImage != null)
        {
            float elapsedTime = 0f;
            Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
            Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }
        }

        // Reset time scale
        Time.timeScale = 1f;

        // Load main menu
        Debug.Log($"[DeathManager] Loading scene: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
        isDying = false;
    }
} 