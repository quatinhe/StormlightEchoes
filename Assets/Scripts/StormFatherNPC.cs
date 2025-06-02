using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using System.Collections;

public class StormFatherNPC : MonoBehaviour
{
    public string npcName = "Storm Father";
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private Button submitQuestionButton;
    [SerializeField] private Button closeDialogueButton;
    public string apiEndpoint = "http://localhost:8000/ask";

    [TextArea(3, 10)]
    public List<string> dialoguePages = new List<string>
    {
        "I am the Storm Father.",
        "You have much to learn, child of Honor.",
        "Ask me what you wish to know about Roshar."
    };
    public string interactionPrompt = "Press E to talk";

    private bool playerInRange = false;
    private bool isInDialogue = false;
    private bool isAskingQuestion = false;
    private int currentDialoguePage = 0;
    private HttpClient httpClient;
    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private int questionsAsked = 0;
    private int maxQuestions = 3;
    private bool waitingForContinue = false;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (questionInput != null)
            questionInput.gameObject.SetActive(false);
        if (submitQuestionButton != null)
        {
            submitQuestionButton.gameObject.SetActive(false);
            submitQuestionButton.onClick.AddListener(SubmitQuestion);
        }
        if (closeDialogueButton != null)
        {
            closeDialogueButton.gameObject.SetActive(false);
            closeDialogueButton.onClick.AddListener(CloseDialogue);
        }
        httpClient = new HttpClient();
        if (playerController != null)
            playerRb = playerController.GetComponent<Rigidbody2D>();
    }

    private void OnDestroy()
    {
        if (httpClient != null)
            httpClient.Dispose();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            // Dynamically assign the local player's PlayerController
            var pc = collision.GetComponent<PlayerController>();
            if (pc != null && pc.IsOwner)
            {
                playerController = pc;
                Debug.Log("[StormFatherNPC] Assigned local PlayerController on trigger enter.");
            }
        }
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
            if (waitingForContinue)
            {
                waitingForContinue = false;
                if (questionsAsked < maxQuestions)
                {
                    ShowQuestionInput();
                }
                else
                {
                    // After 3 questions, Storm Father kills the player
                    KillPlayer();
                    return;
                }
                return;
            }
            if (!isInDialogue)
                StartDialogue();
            else if (!isAskingQuestion)
                AdvanceDialogue();
        }
    }

    private void StartDialogue()
    {
        isInDialogue = true;
        isAskingQuestion = false;
        currentDialoguePage = 0;
        questionsAsked = 0;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (nameText != null)
            nameText.text = npcName;
        if (questionInput != null)
            questionInput.gameObject.SetActive(false);
        if (submitQuestionButton != null)
            submitQuestionButton.gameObject.SetActive(false);
        if (closeDialogueButton != null)
            closeDialogueButton.gameObject.SetActive(false);
        ShowCurrentDialoguePage();
        if (playerController == null)
        {
            playerController = FindObjectsOfType<PlayerController>().FirstOrDefault(pc => pc.IsOwner);
            if (playerController != null)
                Debug.Log("[StormFatherNPC] Assigned local PlayerController via fallback in StartDialogue.");
        }
        if (playerController != null && playerController.IsOwner)
        {
            Debug.Log("[StormFatherNPC] Setting movementLocked to true for local player");
            playerController.movementLocked.Value = true;
        }
        else
        {
            Debug.Log("[StormFatherNPC] playerController is null or not owner: " + (playerController == null) + ", " + (playerController != null ? playerController.IsOwner.ToString() : "null"));
        }
    }

    private void AdvanceDialogue()
    {
        currentDialoguePage++;
        if (currentDialoguePage >= dialoguePages.Count)
        {
            ShowQuestionInput();
        }
        else
        {
            ShowCurrentDialoguePage();
        }
    }

    private void ShowQuestionInput()
    {
        isAskingQuestion = true;
        if (dialogueText != null)
            dialogueText.text = $"What would you like to know? ({questionsAsked + 1}/{maxQuestions})";
        if (questionInput != null)
        {
            questionInput.gameObject.SetActive(true);
            questionInput.text = "";
        }
        if (submitQuestionButton != null)
            submitQuestionButton.gameObject.SetActive(true);
        if (closeDialogueButton != null)
            closeDialogueButton.gameObject.SetActive(false);
    }

    private void ShowCurrentDialoguePage()
    {
        if (dialogueText != null && currentDialoguePage < dialoguePages.Count)
            dialogueText.text = dialoguePages[currentDialoguePage];
    }

    private void CloseDialogue()
    {
        isInDialogue = false;
        isAskingQuestion = false;
        currentDialoguePage = 0;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (questionInput != null)
            questionInput.gameObject.SetActive(false);
        if (submitQuestionButton != null)
            submitQuestionButton.gameObject.SetActive(false);
        if (closeDialogueButton != null)
            closeDialogueButton.gameObject.SetActive(false);
        if (playerController == null)
        {
            playerController = FindObjectsOfType<PlayerController>().FirstOrDefault(pc => pc.IsOwner);
        }
        if (playerController != null && playerController.IsOwner)
        {
            Debug.Log("[StormFatherNPC] Setting movementLocked to false for local player");
            playerController.movementLocked.Value = false;
        }
    }

    private async void SubmitQuestion()
    {
        if (string.IsNullOrEmpty(questionInput.text))
            return;

        string question = questionInput.text;
        questionInput.text = "";

        // Show loading message and hide input/button
        if (dialogueText != null)
            dialogueText.text = "The Storm Father is considering your question...";
        if (questionInput != null)
            questionInput.gameObject.SetActive(false);
        if (submitQuestionButton != null)
            submitQuestionButton.gameObject.SetActive(false);
        if (closeDialogueButton != null)
            closeDialogueButton.gameObject.SetActive(false);

        try
        {
            var requestData = new { question = question };
            var content = new StringContent(
                JsonConvert.SerializeObject(requestData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(apiEndpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var answer = JsonConvert.DeserializeObject<AskResponse>(responseContent);

                questionsAsked++;
                if (dialogueText != null)
                    dialogueText.text = answer.answer + "\n\n<Press E to continue...>";
                waitingForContinue = true;
            }
            else
            {
                Debug.LogError($"API request failed: {response.StatusCode}");
                dialogueText.text = "I am unable to answer at this moment.\n\n<Press E to continue...>";
                waitingForContinue = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error sending question to API: {e.Message}");
            dialogueText.text = "I am unable to answer at this moment.\n\n<Press E to continue...>";
            waitingForContinue = true;
        }
    }

    private class AskResponse
    {
        public string answer { get; set; }
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

    private void KillPlayer()
    {
        if (dialogueText != null)
            dialogueText.text = "Your time has come, child of Honor. The Storm Father's judgment is final.";
        
        // Wait a moment for the player to read the message, then kill them
        StartCoroutine(KillPlayerAfterDelay(2f));
    }

    private System.Collections.IEnumerator KillPlayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (playerController != null && playerController.IsOwner)
        {
            Debug.Log("[StormFatherNPC] Storm Father is killing the player - loading MainMenu directly");
            
            // Disable player movement and controls immediately
            playerController.movementLocked.Value = true;
            var rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            
            // Disable player components
            playerController.enabled = false;
            var collider = playerController.GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;
            
            // Load main menu directly without DeathManager fade effects
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        
        // Close the dialogue
        CloseDialogue();
    }
} 