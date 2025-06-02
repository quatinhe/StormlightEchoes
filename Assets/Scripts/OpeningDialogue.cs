using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class OpeningDialogue : NetworkBehaviour
{
    [Header("Dialogue UI References")]
    [Tooltip("Reference to the same dialogue panel used by SpellTeacher")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    
    [Header("Opening Dialogue Content")]
    public string speakerName = "Unknown Voice";
    
    [Tooltip("Portrait image to display during the opening dialogue")]
    public Sprite portraitSprite;
    
    [TextArea(3, 10)]
    public List<string> openingDialoguePages = new List<string>
    {
        "Welcome to the Shattered Plains...",
        "You have awakened in a strange and dangerous land.",
        "The storms have changed everything...",
        "You must learn to survive, and perhaps... discover the truth.",
        "Press E to continue through each message.",
        "Use WASD to move, Space to jump, and Left Click to attack.",
        "Your journey begins now..."
    };
    
    [Header("Settings")]
    [Tooltip("Delay before opening dialogue starts (in seconds)")]
    public float startDelay = 1f;
    
    [Tooltip("Should this dialogue only show once per game session?")]
    public bool showOnlyOnce = true;
    
    private bool isInDialogue = false;
    private int currentDialoguePage = 0;
    private PlayerController playerController;
    private bool dialogueCompleted = false;
    private static bool hasShownOpening = false; // Static to persist across scene loads

#if UNITY_EDITOR
    // In editor, reset the static variable when the script is loaded
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticVariables()
    {
        hasShownOpening = false;
        Debug.Log("[OpeningDialogue] Static variables reset for editor play session");
    }
#endif

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Only the server/host should handle the opening dialogue logic
        if (IsServer)
        {
            StartCoroutine(InitializeOpeningDialogue());
        }
    }
    
    private IEnumerator InitializeOpeningDialogue()
    {
        // Wait for start delay
        yield return new WaitForSeconds(startDelay);
        
        // Check if we should show the opening dialogue
        if (showOnlyOnce && hasShownOpening)
        {
            Debug.Log("[OpeningDialogue] Opening dialogue already shown this session, skipping.");
            yield break;
        }
        
        // Find the local player
        yield return new WaitUntil(() => FindLocalPlayer());
        
        if (playerController != null)
        {
            StartOpeningDialogue();
        }
    }
    
    private bool FindLocalPlayer()
    {
        // Find the local player controller
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            if (player.IsOwner)
            {
                playerController = player;
                return true;
            }
        }
        return false;
    }
    
    private void Update()
    {
        // Only handle input if we're in dialogue
        if (isInDialogue)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                AdvanceDialogue();
            }
        }
    }
    
    private void StartOpeningDialogue()
    {
        Debug.Log("[OpeningDialogue] Starting opening dialogue");
        
        isInDialogue = true;
        currentDialoguePage = 0;
        
        // Lock player movement
        if (playerController != null)
        {
            playerController.movementLocked.Value = true;
            Debug.Log("[OpeningDialogue] Player movement locked");
        }
        
        // Show dialogue panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
        
        // Set speaker name
        if (nameText != null)
        {
            nameText.text = speakerName;
        }
        
        // Set portrait image
        if (portraitImage != null && portraitSprite != null)
        {
            portraitImage.sprite = portraitSprite;
            portraitImage.gameObject.SetActive(true);
        }
        else if (portraitImage != null)
        {
            // Hide portrait if no sprite is assigned
            portraitImage.gameObject.SetActive(false);
        }
        
        // Show first dialogue page
        ShowCurrentDialoguePage();
        
        // Mark as shown if set to show only once
        if (showOnlyOnce)
        {
            hasShownOpening = true;
        }
    }
    
    private void AdvanceDialogue()
    {
        currentDialoguePage++;
        
        if (currentDialoguePage >= openingDialoguePages.Count)
        {
            CloseDialogue();
        }
        else
        {
            ShowCurrentDialoguePage();
        }
    }
    
    private void ShowCurrentDialoguePage()
    {
        if (dialogueText != null && currentDialoguePage < openingDialoguePages.Count)
        {
            dialogueText.text = openingDialoguePages[currentDialoguePage];
        }
    }
    
    private void CloseDialogue()
    {
        Debug.Log("[OpeningDialogue] Closing opening dialogue");
        
        isInDialogue = false;
        dialogueCompleted = true;
        currentDialoguePage = 0;
        
        // Hide dialogue panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        // Unlock player movement
        if (playerController != null)
        {
            playerController.movementLocked.Value = false;
            Debug.Log("[OpeningDialogue] Player movement unlocked");
        }
        
        // Call completion event
        OnDialogueCompleted();
    }
    
    private void OnDialogueCompleted()
    {
        Debug.Log("[OpeningDialogue] Opening dialogue completed");
        
        // You can add additional logic here that should happen after the opening dialogue
        // For example: enabling certain game systems, starting background music, etc.
    }
    
    // Public methods for external control
    public void TriggerOpeningDialogue()
    {
        if (!isInDialogue && !dialogueCompleted)
        {
            StartCoroutine(InitializeOpeningDialogue());
        }
    }
    
    public bool IsDialogueActive()
    {
        return isInDialogue;
    }
    
    public bool IsDialogueCompleted()
    {
        return dialogueCompleted;
    }
    
    public void ResetDialogue()
    {
        hasShownOpening = false;
        dialogueCompleted = false;
        isInDialogue = false;
        currentDialoguePage = 0;
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        if (playerController != null)
        {
            playerController.movementLocked.Value = false;
        }
        
        Debug.Log("[OpeningDialogue] Dialogue system manually reset");
    }
    
    // Method to manually set dialogue content
    public void SetDialogueContent(string speaker, List<string> pages)
    {
        speakerName = speaker;
        openingDialoguePages = pages;
    }
    
    // Editor helper method to force reset static variables
    public void ForceResetStatic()
    {
        hasShownOpening = false;
        Debug.Log("[OpeningDialogue] Static variables force reset");
    }
    
    // Method to check static state (for debugging)
    public bool HasShownOpeningStatic()
    {
        return hasShownOpening;
    }
    
    // Method to force trigger dialogue (ignores showOnlyOnce)
    public void ForceTriggerDialogue()
    {
        Debug.Log("[OpeningDialogue] Force triggering dialogue");
        hasShownOpening = false;
        dialogueCompleted = false;
        StartCoroutine(InitializeOpeningDialogue());
    }
} 