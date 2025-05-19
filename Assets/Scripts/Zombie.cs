using UnityEngine;

public class Zombie : Enemy
{
    [Header("Zombie Settings")]
    [Tooltip("How close the zombie needs to be to attack")]
    public float attackRange = 1.5f;
    [Tooltip("How often the zombie can attack")]
    public float attackCooldown = 1f;
    [Tooltip("How far the zombie can see the player")]
    public float detectionRange = 5f;

    private float attackTimer;
    private Transform playerTransform;

    protected override void Start()
    {
        base.Start();
        playerTransform = PlayerController.Instace.transform;
        attackTimer = attackCooldown;
    }

    protected override void Update()
    {
        base.Update();
        UpdateBehavior();
    }

    protected override void UpdateBehavior()
    {
        if (playerTransform == null) return;

        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Only move and attack if within detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Move towards player if not in attack range
            if (distanceToPlayer > attackRange)
            {
                Vector2 direction = (playerTransform.position - transform.position).normalized;
                Move(direction);

                // Flip sprite based on movement direction
                if (direction.x > 0 && !isFacingRight || direction.x < 0 && isFacingRight)
                {
                    Flip();
                }
            }
            else
            {
                // Stop moving when in attack range
                Move(Vector2.zero);
                
                // Attack if cooldown is ready
                if (attackTimer <= 0)
                {
                    Attack();
                    attackTimer = attackCooldown;
                }
            }
        }
        else
        {
            // Stop moving if player is too far
            Move(Vector2.zero);
        }

        // Update attack timer
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    protected override void Attack()
    {
        // Check if player is still in range
        if (Vector2.Distance(transform.position, playerTransform.position) <= attackRange)
        {
            // Try to damage the player
            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log("Zombie attacks player!");
            }
        }
    }

    // Optional: Visualize attack and detection ranges in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
