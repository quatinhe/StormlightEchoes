using UnityEngine;
using System.Collections;

public class Parshendi : Enemy
{
    [Header("Parshendi Settings")]
    [Tooltip("How close the Parshendi needs to be to attack")]
    public float attackRange = 2f;
    [Tooltip("How often the Parshendi can attack")]
    public float attackCooldown = 1.5f;
    [Tooltip("How far the Parshendi can see the player")]
    public float detectionRange = 7f;
    [Tooltip("Delay before dealing damage to match animation")]
    public float attackDelay = 0.2f;

    private float attackTimer;
    private Transform playerTransform;
    private Animator animator;
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    protected override void Start() 
    {
        base.Start();
        playerTransform = PlayerController.Instace.transform;
        attackTimer = attackCooldown;
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError($"{gameObject.name} requires an Animator component!");
        }
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

                // Set walking animation
                if (animator != null)
                {
                    animator.SetBool(IsWalking, true);
                }

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
            // Set idle animation
            if (animator != null)
            {
                animator.SetBool(IsWalking, false);
            }
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
            // Trigger attack animation
            if (animator != null)
            {
                animator.SetBool(IsWalking, false);
                animator.SetTrigger(AttackTrigger);
            }

            // Start the delayed damage coroutine
            StartCoroutine(DelayedDamage());
        }
    }

    private IEnumerator DelayedDamage()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(attackDelay);

        // Check if player is still in range after the delay
        if (Vector2.Distance(transform.position, playerTransform.position) <= attackRange)
        {
            // Try to damage the player
            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log("Parshendi attacks player!");
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