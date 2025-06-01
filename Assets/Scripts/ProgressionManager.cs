using UnityEngine;
using Unity.Netcode;

public class ProgressionManager : NetworkBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Progression Settings")]
    public int parshendiKillsRequired = 1;
    
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
    }

    public void OnParshendiKilled()
    {
        if (!IsServer) return;

        parshendiKills.Value++;
        
        if (parshendiKills.Value >= parshendiKillsRequired && !doubleJumpUnlocked.Value)
        {
            UnlockDoubleJump();
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

    public bool IsDoubleJumpUnlocked()
    {
        return doubleJumpUnlocked.Value;
    }

    public int GetParshendiKills()
    {
        return parshendiKills.Value;
    }
} 