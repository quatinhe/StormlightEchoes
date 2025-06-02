using UnityEngine;

public class PauseMenuInitializer : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[PauseMenuInitializer] Awake called");
        // Check if PauseMenu instance already exists
        if (PauseMenu.Instance == null)
        {
            Debug.LogError("[PauseMenuInitializer] No PauseMenu found in scene! Please add the PauseMenu GameObject to your first scene.");
        }
        else
        {
            Debug.Log("[PauseMenuInitializer] PauseMenu instance found");
        }
    }
} 