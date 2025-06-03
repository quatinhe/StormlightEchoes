using System;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

public abstract class Enemy : NetworkBehaviour
{
    [Header("Base Stats")] [Tooltip("Maximum health of the enemy")]
    public int maxHealth = 3;

    [Tooltip("Base movement speed of the enemy")]
    public float moveSpeed = 3f;

    [Tooltip("Base damage dealt by the enemy")]
    public int damage = 1;

    [Header("Recoil")] [Tooltip("How far the enemy gets knocked back")]
    public float recoilForce = 5f;

    [Tooltip("How long the recoil lasts")] public float recoilDuration = 0.2f;

    [Tooltip("How much to reduce vertical recoil")] [Range(0f, 1f)]
    public float verticalRecoilMultiplier = 0.5f;

    [Header("Health")] protected NetworkVariable<float> currentHealth = new NetworkVariable<float>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    protected Rigidbody2D rb;
    protected bool isRecoiling;
    protected float recoilTimeLeft;
    protected bool isFacingRight = true;
    protected SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Initialize health for the server/owner
        if (IsServer || HasAuthority)
        {
            currentHealth.Value = maxHealth;
        }
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null)
        {
            Debug.LogError($"{gameObject.name} requires a Rigidbody2D component!");
        }

        if (spriteRenderer == null)
        {
            Debug.LogError($"{gameObject.name} requires a SpriteRenderer component!");
        }
        
        // Ensure health is properly initialized
        if (currentHealth.Value <= 0 && (IsServer || HasAuthority))
        {
            currentHealth.Value = maxHealth;
            Debug.Log($"[{gameObject.name}] Health initialized to {maxHealth}");
        }
    }
    
    protected Transform FindNearestPlayer()
    {
        Transform nearestPlayer = null;
        float minDistance = float.MaxValue;

        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPlayer = player.transform;
            }
        }

        return nearestPlayer;
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void ApplyDamage_ServerRpc(int amount)
    {
        TakeDamage(amount);
    }

    public virtual void TakeDamage(int amount)
    {
        if (HasAuthority || IsServer)
        {
            Debug.Log($"[{gameObject.name}] Taking {amount} damage. Health before: {currentHealth.Value}/{maxHealth}");

            // Prevent negative damage or excessive damage
            if (amount <= 0)
            {
                Debug.LogWarning($"[{gameObject.name}] Invalid damage amount: {amount}");
                return;
            }

            float healthBefore = currentHealth.Value;
            currentHealth.Value -= amount;
            
            Debug.Log($"[{gameObject.name}] Health after damage: {currentHealth.Value}/{maxHealth}");
            
            if (currentHealth.Value <= 0)
            {
                Debug.Log($"[{gameObject.name}] Enemy died from damage");
                Die();
            }
            else
            {
                // Apply recoil with error handling
                try
                {
                    ApplyRecoil();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[{gameObject.name}] Error applying recoil: {e.Message}");
                    // Continue without recoil if there's an error
                }
            }
        }
    }

    protected virtual void ApplyRecoil()
    {
        if (rb != null)
        {
            Vector2 playerPosition = Vector2.zero;
            bool foundPlayer = false;
            
            // get player position from the static instance first
            if (PlayerController.Instace != null && PlayerController.Instace.transform != null)
            {
                playerPosition = PlayerController.Instace.transform.position;
                foundPlayer = true;
            }
            else
            {
                // Ou find the nearest player
                Transform nearestPlayer = FindNearestPlayer();
                if (nearestPlayer != null)
                {
                    playerPosition = nearestPlayer.position;
                    foundPlayer = true;
                }
            }
            
            if (foundPlayer)
            {
                
                Vector2 direction = (transform.position - (Vector3)playerPosition).normalized;

                
                direction.y *= verticalRecoilMultiplier;

                
                rb.linearVelocity = direction * recoilForce;

                
                isRecoiling = true;
                recoilTimeLeft = recoilDuration;
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Could not find player for recoil calculation, applying default recoil");
                
                Vector2 randomDirection = new Vector2(UnityEngine.Random.Range(-1f, 1f), 0.5f).normalized;
                rb.linearVelocity = randomDirection * recoilForce;
                isRecoiling = true;
                recoilTimeLeft = recoilDuration;
            }
        }
    }

    protected virtual void Update()
    {
        if (isRecoiling)
        {
            recoilTimeLeft -= Time.deltaTime;
            if (recoilTimeLeft <= 0)
            {
                isRecoiling = false;
                
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }

    public void AddForce(Vector2 force)
    {
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    protected virtual void Die()
    {
        if (HasAuthority)
        {
            Debug.Log($"{gameObject.name} died.");
            GetComponent<NetworkObject>().Despawn();
        }
    }

    protected virtual void Flip()
    {
        isFacingRight = !isFacingRight;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !isFacingRight;
        }
    }

    protected virtual void Move(Vector2 direction)
    {
        if (HasAuthority)
        {
            if (rb != null && !isRecoiling)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }
        }
    }

    
    protected abstract void Attack();
    protected abstract void UpdateBehavior();
}