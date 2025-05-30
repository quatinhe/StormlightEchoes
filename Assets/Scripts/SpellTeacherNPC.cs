using UnityEngine;
using Unity.Netcode;

public class SpellTeacherNPC : NetworkBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Spell Teacher";
    public string interactionPrompt = "Press E to learn Side Spell";
    
    private bool playerInRange = false;
    private PlayerController nearbyPlayer;

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
                TeachSideSpell();
            }
        }
    }

    private void TeachSideSpell()
    {
        if (nearbyPlayer != null)
        {
            nearbyPlayer.EnableSideSpell();
        }
    }

    private void OnGUI()
    {
        if (playerInRange)
        {
            // Display interaction prompt
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Screen.height - screenPos.y; // Convert to screen coordinates
            
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 50, 200, 20), interactionPrompt);
        }
    }
} 