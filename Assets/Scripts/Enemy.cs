using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    [Tooltip("Maximum health of the enemy")]
    public int maxHealth = 3;
    [Tooltip("Base movement speed of the enemy")]
    public float moveSpeed = 3f;
    [Tooltip("Base damage dealt by the enemy")]
    public int damage = 1;

    [Header("Recoil")]
    [Tooltip("How far the enemy gets knocked back")]
    public float recoilForce = 5f;
    [Tooltip("How long the recoil lasts")]
    public float recoilDuration = 0.2f;
    [Tooltip("How much to reduce vertical recoil")]
    [Range(0f, 1f)] public float verticalRecoilMultiplier = 0.5f;

    protected int currentHealth;
    protected Rigidbody2D rb;
    protected bool isRecoiling;
    protected float recoilTimeLeft;
    protected bool isFacingRight = true;
    protected SpriteRenderer spriteRenderer;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
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
    }

    public virtual void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Apply recoil
            ApplyRecoil();
        }
    }

    protected virtual void ApplyRecoil()
    {
        if (rb != null)
        {
            // Get the direction from the player to the enemy
            Vector2 playerPosition = PlayerController.Instace.transform.position;
            Vector2 direction = (transform.position - (Vector3)playerPosition).normalized;
            
            // Apply more horizontal recoil than vertical
            direction.y *= verticalRecoilMultiplier;
            
            // Apply the recoil force
            rb.linearVelocity = direction * recoilForce;
            
            // Start recoil state
            isRecoiling = true;
            recoilTimeLeft = recoilDuration;
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
                // Reset velocity when recoil ends
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
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
        if (rb != null && !isRecoiling)
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    // Abstract methods that derived classes must implement
    protected abstract void Attack();
    protected abstract void UpdateBehavior();
}
