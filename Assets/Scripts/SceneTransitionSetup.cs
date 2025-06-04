using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneTransitionData
{
    public string sceneName;
    public Vector3 spawnPosition;
    public string transitionPrompt = "Press E to enter";
}

/// <summary>
/// Helper script to easily set up scene transition zones in the editor
/// This script helps you create transition zones quickly with proper configurations
/// </summary>
public class SceneTransitionSetup : MonoBehaviour
{
    [Header("Quick Setup")]
    [SerializeField] private SceneTransitionData[] transitionSetups;
    
    [Header("Prefab References")]
    [SerializeField] private GameObject transitionZonePrefab;
    [SerializeField] private GameObject transitionPromptUIPrefab;
    
    [ContextMenu("Create Transition Zones")]
    public void CreateTransitionZones()
    {
        if (transitionSetups == null || transitionSetups.Length == 0)
        {
            Debug.LogWarning("[SceneTransitionSetup] No transition setups defined!");
            return;
        }
        
        for (int i = 0; i < transitionSetups.Length; i++)
        {
            CreateTransitionZone(transitionSetups[i], i);
        }
    }
    
    private void CreateTransitionZone(SceneTransitionData data, int index)
    {
        // Create the transition zone GameObject
        GameObject zoneObj = new GameObject($"TransitionZone_To_{data.sceneName}");
        zoneObj.transform.SetParent(this.transform);
        zoneObj.transform.position = transform.position + Vector3.right * (index * 5); // Spread them out
        
        // Add collider
        BoxCollider2D collider = zoneObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2f, 3f); // Default size, adjust as needed
        
        // Add the transition zone script
        SceneTransitionZone transitionZone = zoneObj.AddComponent<SceneTransitionZone>();
        
        // Use reflection to set private fields (this is for editor use)
        var targetSceneField = typeof(SceneTransitionZone).GetField("targetSceneName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spawnPositionField = typeof(SceneTransitionZone).GetField("playerSpawnPosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var promptTextField = typeof(SceneTransitionZone).GetField("promptText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (targetSceneField != null)
            targetSceneField.SetValue(transitionZone, data.sceneName);
        if (spawnPositionField != null)
            spawnPositionField.SetValue(transitionZone, data.spawnPosition);
        if (promptTextField != null)
            promptTextField.SetValue(transitionZone, data.transitionPrompt);
        
        Debug.Log($"[SceneTransitionSetup] Created transition zone to {data.sceneName} at {zoneObj.transform.position}");
    }
    
    [ContextMenu("Create Transition Prompt UI")]
    public void CreateTransitionPromptUI()
    {
        // Check if UI already exists
        TransitionPromptUI existingUI = FindObjectOfType<TransitionPromptUI>();
        if (existingUI != null)
        {
            Debug.Log("[SceneTransitionSetup] Transition Prompt UI already exists in scene!");
            return;
        }
        
        // Create Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create prompt UI
        GameObject promptObj = new GameObject("TransitionPromptUI");
        promptObj.transform.SetParent(canvas.transform);
        
        // Set up RectTransform
        RectTransform rectTransform = promptObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(200f, 50f);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Add UI Image background
        UnityEngine.UI.Image bgImage = promptObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f);
        
        // Create text child
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Press E to enter";
        text.fontSize = 16f;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Add the TransitionPromptUI script
        TransitionPromptUI promptUI = promptObj.AddComponent<TransitionPromptUI>();
        
        // Set references using reflection
        var promptTextField = typeof(TransitionPromptUI).GetField("promptText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (promptTextField != null)
            promptTextField.SetValue(promptUI, text);
        
        Debug.Log("[SceneTransitionSetup] Created Transition Prompt UI!");
    }
}

// Custom editor script to make it easier to use
#if UNITY_EDITOR
[CustomEditor(typeof(SceneTransitionSetup))]
public class SceneTransitionSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Setup Tools", EditorStyles.boldLabel);
        
        SceneTransitionSetup setup = (SceneTransitionSetup)target;
        
        if (GUILayout.Button("Create Transition Prompt UI"))
        {
            setup.CreateTransitionPromptUI();
        }
        
        if (GUILayout.Button("Create Transition Zones"))
        {
            setup.CreateTransitionZones();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. First click 'Create Transition Prompt UI' to set up the UI system\n" +
            "2. Configure your transition setups in the inspector\n" +
            "3. Click 'Create Transition Zones' to generate the zones\n" +
            "4. Adjust zone positions and sizes as needed", 
            MessageType.Info);
    }
}
#endif 