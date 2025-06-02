using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Header("FPS Counter Settings")]
    [Tooltip("Show FPS counter on start")]
    public bool showOnStart = false;
    
    [Tooltip("Update frequency for FPS calculation (lower = more responsive)")]
    public float updateInterval = 0.5f;
    
    [Tooltip("Text color for the FPS display")]
    public Color textColor = Color.yellow;
    
    [Tooltip("Text size for the FPS display")]
    public int fontSize = 24;
    
    [Tooltip("Position offset from top-left corner")]
    public Vector2 screenOffset = new Vector2(10f, 10f);

    private bool showFPS = false;
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private float timer = 0.0f;
    private GUIStyle style;

    void Start()
    {
        showFPS = showOnStart;
        
        // Initialize GUI style
        style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        
        // Add a black outline for better readability
        style.fontStyle = FontStyle.Bold;
    }

    void Update()
    {
        // Toggle FPS display with "I" key
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleFPS();
        }
        
        if (showFPS)
        {
            // Calculate delta time
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            
            // Update FPS at specified interval
            timer += Time.unscaledDeltaTime;
            if (timer >= updateInterval)
            {
                fps = 1.0f / deltaTime;
                timer = 0.0f;
            }
        }
    }

    void OnGUI()
    {
        if (!showFPS) return;
        
        // Calculate position
        Rect rect = new Rect(screenOffset.x, screenOffset.y, 200, 50);
        
        // Create FPS text with additional info
        string fpsText = string.Format("FPS: {0:F1}\nMS: {1:F2}", fps, deltaTime * 1000.0f);
        
        // Draw background for better readability
        Color originalColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
        GUI.DrawTexture(new Rect(rect.x - 5, rect.y - 5, rect.width, rect.height), Texture2D.whiteTexture);
        GUI.color = originalColor;
        
        // Draw the FPS text
        GUI.Label(rect, fpsText, style);
        
        // Add performance indicators
        string performanceText = GetPerformanceIndicator(fps);
        if (!string.IsNullOrEmpty(performanceText))
        {
            Rect perfRect = new Rect(screenOffset.x, screenOffset.y + 40, 200, 20);
            GUIStyle perfStyle = new GUIStyle(style);
            perfStyle.fontSize = fontSize - 4;
            perfStyle.normal.textColor = GetPerformanceColor(fps);
            GUI.Label(perfRect, performanceText, perfStyle);
        }
    }

    private void ToggleFPS()
    {
        showFPS = !showFPS;
        Debug.Log($"[FPSCounter] FPS display {(showFPS ? "enabled" : "disabled")}");
    }

    private string GetPerformanceIndicator(float currentFPS)
    {
        if (currentFPS >= 60f)
            return "EXCELLENT";
        else if (currentFPS >= 45f)
            return "GOOD";
        else if (currentFPS >= 30f)
            return "FAIR";
        else if (currentFPS >= 15f)
            return "POOR";
        else
            return "CRITICAL";
    }

    private Color GetPerformanceColor(float currentFPS)
    {
        if (currentFPS >= 60f)
            return Color.green;
        else if (currentFPS >= 45f)
            return Color.green;
        else if (currentFPS >= 30f)
            return Color.yellow;
        else
            return Color.red;
    }

    // Public methods for external access
    public void ShowFPS(bool show)
    {
        showFPS = show;
    }

    public bool IsFPSVisible()
    {
        return showFPS;
    }

    public float GetCurrentFPS()
    {
        return fps;
    }
} 