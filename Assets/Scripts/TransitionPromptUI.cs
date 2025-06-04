using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransitionPromptUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string defaultPromptText = "Press E to enter";
    
    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    private CanvasGroup canvasGroup;
    private bool isVisible = false;
    private float baseAlpha = 1f;
    
    private void Awake()
    {
        // Initialize CanvasGroup early
        InitializeCanvasGroup();
    }
    
    private void Start()
    {
        // Set default text if not set
        if (promptText != null && string.IsNullOrEmpty(promptText.text))
        {
            promptText.text = defaultPromptText;
        }
        
        // Hide initially
        SetVisible(false, true);
    }
    
    private void InitializeCanvasGroup()
    {
        // Get or add CanvasGroup for smooth fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    private void Update()
    {
        if (canvasGroup == null)
        {
            InitializeCanvasGroup();
            return;
        }
        
        if (isVisible)
        {
            // Animate the prompt with a subtle pulse effect
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = Vector3.one * pulse;
        }
        
        // Smooth fade in/out
        float targetAlpha = isVisible ? baseAlpha : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }
    
    public void SetVisible(bool visible, bool immediate = false)
    {
        isVisible = visible;
        
        // Make sure CanvasGroup exists
        if (canvasGroup == null)
        {
            InitializeCanvasGroup();
        }
        
        if (canvasGroup != null)
        {
            if (immediate)
            {
                canvasGroup.alpha = visible ? baseAlpha : 0f;
                transform.localScale = Vector3.one;
            }
            
            // Enable/disable interaction
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        
        // Hide/show the panel if it exists
        if (promptPanel != null)
        {
            promptPanel.SetActive(visible);
        }
    }
    
    public void SetPromptText(string text)
    {
        if (promptText != null)
        {
            promptText.text = text;
        }
    }
} 