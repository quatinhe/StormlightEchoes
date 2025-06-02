using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Menu UI Elements")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Menu Music")]
    [SerializeField] private AudioSource menuMusic;
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("Menu Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private float buttonHoverScale = 1.1f;
    [SerializeField] private float buttonTransitionSpeed = 10f;

    private void Start()
    {
        // Initialize button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogError("Start Game button not assigned in MainMenu!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogError("Quit button not assigned in MainMenu!");
        }

        // Ensure the background image is visible
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Background image not assigned in MainMenu!");
        }

        // Play menu music if assigned
        if (menuMusic != null && !menuMusic.isPlaying)
        {
            menuMusic.Play();
        }
    }

    private void StartGame()
    {
        // Fade out music, then start new game
        if (menuMusic != null && menuMusic.isPlaying)
        {
            StartCoroutine(FadeOutMusicAndStartGame());
        }
        else
        {
            StartNewGame();
        }
    }

    private IEnumerator FadeOutMusicAndStartGame()
    {
        float startVolume = menuMusic.volume;
        float elapsed = 0f;
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.deltaTime;
            menuMusic.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeDuration);
            yield return null;
        }
        menuMusic.Stop();
        menuMusic.volume = startVolume; // Reset for next time
        StartNewGame();
    }

    private void QuitGame()
    {
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

    public void StartNewGame()
    {
        // Check if NetworkManager is available
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[MainMenu] NetworkManager.Singleton is null, loading scene directly without networking");
            // Load scene directly without networking
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[MainMenu] Host loading new game scene");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("[MainMenu] Client requesting new game");
            RequestNewGameServerRpc();
        }
        else
        {
            Debug.Log("[MainMenu] NetworkManager not running, starting host first");
            NetworkManager.Singleton.StartHost();
            // Wait a frame then load the scene
            StartCoroutine(LoadSceneAfterHostStart());
        }
    }

    private IEnumerator LoadSceneAfterHostStart()
    {
        yield return new WaitForEndOfFrame();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("[MainMenu] NetworkManager became null or failed to start host, loading scene directly");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNewGameServerRpc()
    {
        Debug.Log("[MainMenu] Server received new game request");
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[MainMenu] NetworkManager.Singleton is null in RequestNewGameServerRpc");
        }
    }
} 