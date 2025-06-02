using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class ProgressionManager : NetworkBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Progression Settings")]
    public int parshendiKillsRequired = 1;
    
    [Header("Unlock Message UI")]
    public GameObject unlockMessagePanel;
    public TextMeshProUGUI unlockMessageText;
    public float messageDisplayTime = 4f;
    
    private NetworkVariable<int> parshendiKills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> doubleJumpUnlocked = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Hide unlock message panel at start
        if (unlockMessagePanel != null)
            unlockMessagePanel.SetActive(false);
    }

    public void OnParshendiKilled()
    {
        if (!IsServer) return;

        parshendiKills.Value++;
        Debug.Log($"Parshendi killed! Current kills: {parshendiKills.Value}/{parshendiKillsRequired}");
        
        if (parshendiKills.Value >= parshendiKillsRequired && !doubleJumpUnlocked.Value)
        {
            Debug.Log($"{parshendiKillsRequired} Parshendi killed! Unlocking double jump and showing message.");
            UnlockDoubleJump();
            ShowUnlockMessageClientRpc();
        }
    }

    private void UnlockDoubleJump()
    {
        if (!IsServer) return;
        
        doubleJumpUnlocked.Value = true;
        
        // Find all player controllers and enable double jump
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            player.EnableDoubleJump();
        }
    }
    
    [ClientRpc]
    private void ShowUnlockMessageClientRpc()
    {
        Debug.Log("ShowUnlockMessageClientRpc called on client for double jump unlock!");
        if (unlockMessagePanel != null && unlockMessageText != null)
        {
            unlockMessagePanel.SetActive(true);
            unlockMessageText.text = "You've unlocked the double jump ability! Press W twice to double jump.";
            StartCoroutine(HideMessageAfterDelay());
        }
        else
        {
            Debug.LogWarning("Unlock message panel or text is not assigned in ProgressionManager!");
        }
    }
    
    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        if (unlockMessagePanel != null)
            unlockMessagePanel.SetActive(false);
    }

    public bool IsDoubleJumpUnlocked()
    {
        return doubleJumpUnlocked.Value;
    }

    public int GetParshendiKills()
    {
        return parshendiKills.Value;
    }
} 