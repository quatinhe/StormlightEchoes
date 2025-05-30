using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class FlyingParshendiProgressionManager : NetworkBehaviour
{
    public static FlyingParshendiProgressionManager Instance { get; private set; }

    public int killsRequired = 10;
    private int currentKills = 0;

    [Header("Unlock Message UI")]
    public GameObject unlockMessagePanel;
    public TextMeshProUGUI unlockMessageText;
    public float messageDisplayTime = 4f;

    private void Awake()
    {
        Instance = this;
        if (unlockMessagePanel != null)
            unlockMessagePanel.SetActive(false);
    }

    public void OnFlyingParshendiKilled()
    {
        if (!IsServer) return;
        currentKills++;
        Debug.Log($"Flying Parshendi killed! Current kills: {currentKills}/{killsRequired}");
        if (currentKills == killsRequired)
        {
            Debug.Log("10 flying Parshendi killed! Unlocking dash and showing message.");
            UnlockDashForAllPlayers();
            ShowUnlockMessageClientRpc();
        }
    }

    private void UnlockDashForAllPlayers()
    {
        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            player.EnableDash();
        }
    }

    [ClientRpc]
    private void ShowUnlockMessageClientRpc()
    {
        Debug.Log("ShowUnlockMessageClientRpc called on client!");
        if (unlockMessagePanel != null && unlockMessageText != null)
        {
            unlockMessagePanel.SetActive(true);
            unlockMessageText.text = "You've unlocked the dash ability (shift). Go back to Dalinar Kholin";
            StartCoroutine(HideMessageAfterDelay());
        }
        else
        {
            Debug.LogWarning("Unlock message panel or text is not assigned!");
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        if (unlockMessagePanel != null)
            unlockMessagePanel.SetActive(false);
    }

    public bool IsDashUnlocked()
    {
        return currentKills >= killsRequired;
    }
} 