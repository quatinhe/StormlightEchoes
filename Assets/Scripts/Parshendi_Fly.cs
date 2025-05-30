using System;
using UnityEngine;
using System.Collections;

public class Parshendi_Fly : Enemy
{
    [Header("Flying Parshendi Settings")]
    [Tooltip("How close the Parshendi needs to be to attack")]
    public float attackRange = 3f;
    [Tooltip("How often the Parshendi can attack")]
    public float attackCooldown = 2f;
    [Tooltip("How far the Parshendi can see the player")]
    public float detectionRange = 8f;
    [Tooltip("How fast the Parshendi charges at the player")]
    public float chargeSpeed = 15f;
    [Tooltip("Minimum charge duration")]
    public float minChargeDuration = 0.3f;
    [Tooltip("How long to wait before charging")]
    public float chargeWindup = 0.3f;
    [Tooltip("Damage dealt to player on contact during charge")]
    public int chargeDamage = 20;
    [Tooltip("How fast the Parshendi follows the player")]
    public float followSpeed = 5f;
    [Tooltip("How close the Parshendi tries to get to the player")]
    public float followDistance = 3f;
    [Header("Height Management")]
    [Tooltip("Preferred height above ground")]
    public float preferredHeight = 3f;
    [Tooltip("How fast the Parshendi returns to preferred height")]
    public float heightReturnSpeed = 3f;
    [Tooltip("How close to preferred height before considering it reached")]
    public float heightTolerance = 0.2f;
    [Tooltip("Minimum height difference to trigger attack")]
    public float minHeightDifference = 0.5f;
    [Tooltip("How far below the player to charge")]
    public float chargeOvershoot = 1f;

    private float attackTimer;
    private Transform playerTransform;
    private Animator animator;
    private bool isCharging = false;
    private Vector2 chargeDirection;
    private static readonly int IsFlying = Animator.StringToHash("IsFlying");
    private static readonly int ChargeTrigger = Animator.StringToHash("Charge");
    private bool canDealDamage = false;
    private Vector2 targetPosition;
    private bool isReturningToHeight = false;
    private float groundY;
    private bool isInAttackPosition = false;
    private float lastAttackTime;
    private bool hasDetectedPlayer = false;
    private Vector2 chargeTarget;
    private float chargeDistance;

    protected override void Start() 
    {
        base.Start();
        
        attackTimer = attackCooldown;
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError($"{gameObject.name} requires an Animator component!");
        }

        // Store initial Y position as ground level
        groundY = transform.position.y;
    }

    private void FixedUpdate()
    {
        if (HasAuthority)
        {
            if (!playerTransform)
            {
                PlayerController controller = FindFirstObjectByType<PlayerController>();
                if (controller)
                {
                    playerTransform = controller.transform;
                }
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!isCharging)
        {
            UpdateBehavior();
        }
    }

    protected override void UpdateBehavior()
    {
        if(!HasAuthority)
            return;
        
        if (playerTransform == null) return;

        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        float heightDifference = Mathf.Abs(transform.position.y - playerTransform.position.y);

        // Check if we need to return to preferred height
        float currentHeight = transform.position.y - groundY;
        if (currentHeight < preferredHeight - heightTolerance && !isCharging)
        {
            isReturningToHeight = true;
        }
        else if (currentHeight >= preferredHeight - heightTolerance)
        {
            isReturningToHeight = false;
        }

        // Return to height if needed
        if (isReturningToHeight)
        {
            float targetY = groundY + preferredHeight;
            Vector2 currentPos = transform.position;
            float newY = Mathf.MoveTowards(currentPos.y, targetY, heightReturnSpeed * Time.deltaTime);
            transform.position = new Vector2(currentPos.x, newY);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            return;
        }

        // Check if we should detect the player
        if (!hasDetectedPlayer && distanceToPlayer <= detectionRange)
        {
            hasDetectedPlayer = true;
        }

        // If we've detected the player, always follow and attack
        if (hasDetectedPlayer)
        {
            // If not in attack range or not in good position to attack, follow the player
            if (distanceToPlayer > attackRange || heightDifference < minHeightDifference)
            {
                isInAttackPosition = false;
                // Calculate direction to player
                Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                
                // Calculate target position (maintain follow distance)
                targetPosition = (Vector2)playerTransform.position - directionToPlayer * followDistance;
                
                // Ensure target position maintains preferred height
                targetPosition.y = groundY + preferredHeight;
                
                // Calculate movement direction
                Vector2 moveDirection = (targetPosition - (Vector2)transform.position).normalized;
                
                // Move towards target position
                rb.linearVelocity = moveDirection * followSpeed;

                // Set flying animation
                if (animator != null)
                {
                    animator.SetBool(IsFlying, true);
                }

                // Face the player
                if (directionToPlayer.x > 0 && !isFacingRight || directionToPlayer.x < 0 && isFacingRight)
                {
                    Flip();
                }

                // If we're close enough and at a good height, try to attack
                if (distanceToPlayer <= attackRange * 1.5f && heightDifference >= minHeightDifference)
                {
                    if (Time.time - lastAttackTime >= attackCooldown)
                    {
                        StartCharge();
                        lastAttackTime = Time.time;
                    }
                }
            }
            else
            {
                // We're in attack range and at a good height
                isInAttackPosition = true;
                
                // Stop moving when in attack range
                rb.linearVelocity = Vector2.zero;
                
                // Attack if cooldown is ready
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    StartCharge();
                    lastAttackTime = Time.time;
                }
            }
        }
        else
        {
            // Stop moving if player is too far and not detected
            rb.linearVelocity = Vector2.zero;
            // Set idle animation
            if (animator != null)
            {
                animator.SetBool(IsFlying, false);
            }
        }
    }

    private void StartCharge()
    {
        if(!HasAuthority)
            return;

        // Calculate charge target (slightly below player's position)
        chargeTarget = (Vector2)playerTransform.position;
        chargeTarget.y -= chargeOvershoot;
        
        // Calculate charge direction towards target
        chargeDirection = (chargeTarget - (Vector2)transform.position).normalized;
        
        // Calculate total distance to travel
        chargeDistance = Vector2.Distance(transform.position, chargeTarget);
        
        // Trigger charge animation
        if (animator != null)
        {
            animator.SetTrigger(ChargeTrigger);
        }

        // Start the charge coroutine
        StartCoroutine(ChargeAtPlayer());
    }

    private IEnumerator ChargeAtPlayer()
    {
        isCharging = true;
        canDealDamage = false;

        // Windup phase
        yield return new WaitForSeconds(chargeWindup);

        // Charge phase
        float distanceTraveled = 0f;
        Vector2 startPosition = transform.position;
        canDealDamage = true; // Enable damage dealing during the actual charge

        while (distanceTraveled < chargeDistance)
        {
            // Calculate how far we've moved
            distanceTraveled = Vector2.Distance(startPosition, transform.position);
            
            // Move in the charge direction at high speed
            rb.linearVelocity = chargeDirection * chargeSpeed;
            
            // Ensure minimum charge duration
            if (distanceTraveled >= chargeDistance)
            {
                float timeSpent = distanceTraveled / chargeSpeed;
                if (timeSpent < minChargeDuration)
                {
                    yield return new WaitForSeconds(minChargeDuration - timeSpent);
                }
            }
            
            yield return null;
        }

        // End charge
        rb.linearVelocity = Vector2.zero;
        canDealDamage = false;
        isCharging = false;
        
        // Start returning to height
        isReturningToHeight = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasAuthority || !canDealDamage) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            // Deal damage to the player
            player.TakeDamage(chargeDamage);
            // Optional: Add knockback or other effects here
        }
    }

    protected override void Attack()
    {
        // Attack is handled by the charge behavior
    }

    // Optional: Visualize attack and detection ranges in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw preferred height
        Gizmos.color = Color.blue;
        Vector3 heightPos = transform.position;
        heightPos.y = groundY + preferredHeight;
        Gizmos.DrawLine(transform.position, heightPos);

        // Draw charge target if charging
        if (isCharging)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(chargeTarget, 0.2f);
            Gizmos.DrawLine(transform.position, chargeTarget);
        }
    }

    protected override void Die()
    {
        base.Die();
        if (FlyingParshendiProgressionManager.Instance != null)
        {
            FlyingParshendiProgressionManager.Instance.OnFlyingParshendiKilled();
        }
    }
} 