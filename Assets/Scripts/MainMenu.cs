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
    }

    private void StartGame()
    {
        // Load the first level
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
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[MainMenu] Host loading new game scene");
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
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

    private System.Collections.IEnumerator LoadSceneAfterHostStart()
    {
        yield return new WaitForEndOfFrame();
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNewGameServerRpc()
    {
        Debug.Log("[MainMenu] Server received new game request");
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }
} 