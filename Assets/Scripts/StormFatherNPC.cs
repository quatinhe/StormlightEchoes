using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class StormFatherNPC : MonoBehaviour
{
    public string npcName = "Storm Father";
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    [TextArea(3, 10)]
    public List<string> dialoguePages = new List<string>
    {
        "I am the Storm Father.",
        "You have much to learn, child of Honor."
    };
    public string interactionPrompt = "Press E to talk";

    private bool playerInRange = false;
    private bool isInDialogue = false;
    private int currentDialoguePage = 0;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            if (isInDialogue)
                CloseDialogue();
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!isInDialogue)
                StartDialogue();
            else
                AdvanceDialogue();
        }
    }

    private void StartDialogue()
    {
        isInDialogue = true;
        currentDialoguePage = 0;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (nameText != null)
            nameText.text = npcName;
        ShowCurrentDialoguePage();
    }

    private void AdvanceDialogue()
    {
        currentDialoguePage++;
        if (currentDialoguePage >= dialoguePages.Count)
            CloseDialogue();
        else
            ShowCurrentDialoguePage();
    }

    private void ShowCurrentDialoguePage()
    {
        if (dialogueText != null && currentDialoguePage < dialoguePages.Count)
            dialogueText.text = dialoguePages[currentDialoguePage];
    }

    private void CloseDialogue()
    {
        isInDialogue = false;
        currentDialoguePage = 0;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnGUI()
    {
        if (playerInRange && !isInDialogue)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Screen.height - screenPos.y;
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 50, 200, 20), interactionPrompt);
        }
    }
} 