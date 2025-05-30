using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class SpellTeacherNPC : NetworkBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Dhalinar Kholin";
    public string interactionPrompt = "Press E to learn Side Spell";
    
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    
    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    public List<string> dialoguePages = new List<string>
    {
        "Hello Stranger..",
        "It has been a long time since I've seen someone here in Uthiru"
    };

    [Header("Tutorial Message")]
    public GameObject tutorialMessagePanel;
    public TextMeshProUGUI tutorialText;
    public float messageDisplayTime = 3f;
    public float messageFadeTime = 0.5f;

    [Header("Flying Parshendi Spawner")]
    public FlyingParshendiSpawner flyingParshendiSpawner;
    
    private bool playerInRange = false;
    private PlayerController nearbyPlayer;
    private bool isInDialogue = false;
    private int currentDialoguePage = 0;

    private void Start()
    {
        // Hide dialogue panel at start
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Hide tutorial message at start
        if (tutorialMessagePanel != null)
        {
            tutorialMessagePanel.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            playerInRange = true;
            nearbyPlayer = player;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsServer) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null && player == nearbyPlayer)
        {
            playerInRange = false;
            nearbyPlayer = null;
            
            // Close dialogue if player leaves
            if (isInDialogue)
            {
                CloseDialogue();
            }
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (playerInRange && nearbyPlayer != null)
        {
            // Check for interaction input (E key)
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!isInDialogue)
                {
                    StartDialogue();
                }
                else
                {
                    AdvanceDialogue();
                }
            }
        }
    }

    private void StartDialogue()
    {
        isInDialogue = true;
        currentDialoguePage = 0;
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
        
        if (nameText != null)
        {
            nameText.text = npcName;
        }
        
        ShowCurrentDialoguePage();
    }

    private void AdvanceDialogue()
    {
        currentDialoguePage++;
        
        if (currentDialoguePage >= dialoguePages.Count)
        {
            // End of dialogue
            CloseDialogue();
            TeachSideSpell();
        }
        else
        {
            ShowCurrentDialoguePage();
        }
    }

    private void ShowCurrentDialoguePage()
    {
        if (dialogueText != null && currentDialoguePage < dialoguePages.Count)
        {
            dialogueText.text = dialoguePages[currentDialoguePage];
        }
    }

    private void CloseDialogue()
    {
        isInDialogue = false;
        currentDialoguePage = 0;
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void TeachSideSpell()
    {
        if (nearbyPlayer != null)
        {
            nearbyPlayer.EnableSideSpell();
            ShowTutorialMessage();
            
            // Start spawning flying Parshendi
            if (flyingParshendiSpawner != null)
            {
                flyingParshendiSpawner.StartSpawning();
            }
        }
    }

    private void ShowTutorialMessage()
    {
        if (tutorialMessagePanel != null && tutorialText != null)
        {
            tutorialMessagePanel.SetActive(true);
            tutorialText.text = "You can now press space to cast a spell";
            tutorialText.alpha = 1f;
            StartCoroutine(FadeOutTutorialMessage());
        }
    }

    private IEnumerator FadeOutTutorialMessage()
    {
        // Wait for the display time
        yield return new WaitForSeconds(messageDisplayTime);

        // Fade out the message
        float elapsedTime = 0f;
        while (elapsedTime < messageFadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / messageFadeTime);
            tutorialText.alpha = alpha;
            yield return null;
        }

        // Hide the panel
        tutorialMessagePanel.SetActive(false);
    }

    private void OnGUI()
    {
        if (playerInRange && !isInDialogue)
        {
            // Display interaction prompt
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Screen.height - screenPos.y; // Convert to screen coordinates
            
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 50, 200, 20), interactionPrompt);
        }
    }
} 