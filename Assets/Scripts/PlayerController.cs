using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("How strong the gravity is")]
    public float gravityScale = 1f;

    [Header("Jump")]
    public float jumpForce = 16f;
    [Tooltip("How much to reduce upward velocity when jump is released")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Tooltip("How long to buffer jump input")]
    public float jumpBufferTime = 0.2f;
    [Tooltip("How long to allow jumping after leaving a platform")]
    public float coyoteTime = 0.2f;
    [Tooltip("Can the player double jump?")]
    public bool canDoubleJump = true;

    [Header("Dash")]
    [Tooltip("Can the player dash?")]
    public bool canDash = true;
    [Tooltip("How fast the dash is")]
    public float dashSpeed = 30f;
    [Tooltip("How long the dash lasts")]
    public float dashTime = 0.3f;
    [Tooltip("How long until the player can dash again")]
    public float dashCooldown = 1f;

    [Header("Attack")]
    [Tooltip("Can the player attack?")]
    public bool canAttack = true;
    [Tooltip("Time between attacks")]
    public float timeBetweenAttack = 0.5f;
    [Tooltip("Transform for side attack hitbox")]
    public Transform sideAttackTransform;
    [Tooltip("Transform for down attack hitbox")]
    public Transform downAttackTransform;
    [Tooltip("Transform for up attack hitbox")]
    public Transform upAttackTransform;
    [Tooltip("Size of the side attack hitbox")]
    public Vector2 sideAttackArea = new Vector2(1f, 0.5f);
    [Tooltip("Size of the down attack hitbox")]
    public Vector2 downAttackArea = new Vector2(0.5f, 1f);
    [Tooltip("Size of the up attack hitbox")]
    public Vector2 upAttackArea = new Vector2(0.5f, 1f);
    [Tooltip("Layer mask for attack detection")]
    public LayerMask attackLayer;
    [Tooltip("Prefab for the slash effect")]
    public GameObject slashEffect;
    [Tooltip("How long the slash effect should last")]
    public float slashEffectDuration = 0.2f;

    [Header("Recoil")]
    [Tooltip("Number of steps in X direction recoil")]
    public int recoilXSteps = 3;
    [Tooltip("Number of steps in Y direction recoil")]
    public int recoilYSteps = 2;
    [Tooltip("Speed of X direction recoil")]
    public float recoilXSpeed = 10f;
    [Tooltip("Speed of Y direction recoil")]
    public float recoilYSpeed = 8f;
    [Tooltip("How long each recoil step lasts")]
    public float recoilStepDuration = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    [Tooltip("How far below the player to check for ground")]
    public float groundCheckDistance = 0.1f;

    [Header("Attack Damage")]
    public int attackDamage = 1;

    [Header("Health")]
    [Tooltip("Maximum health of the player")]
    public int maxHealth = 3;
    [Tooltip("How long the player is invulnerable after taking damage")]
    public float invulnerabilityDuration = 1f;
    [Tooltip("How many times the player flashes when taking damage")]
    public int damageFlashCount = 3;
    [Tooltip("How long each flash lasts")]
    public float damageFlashDuration = 0.1f;
    [Tooltip("Blood spurt effect prefab")]
    public GameObject bloodSpurt;
    [Tooltip("How long the blood spurt effect lasts")]
    public float bloodSpurtDuration = 0.5f;
    [Tooltip("How much to slow down time when hit (0.1 = 10% speed)")]
    [Range(0.01f, 1f)] public float hitTimeScale = 0.1f;
    [Tooltip("How long the time slow effect lasts")]
    public float hitTimeDuration = 0.1f;
    [Tooltip("Reference to the health bar UI")]
    public HealthBarUI healthBar;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private float moveInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool canJump;
    private bool hasDoubleJumped;

    // Dash variables
    private bool isDashing;
    private float dashTimeCounter;
    private float dashCooldownCounter;
    private Vector2 dashDirection;

    // Attack variables
    private bool isAttacking;
    private float timeSinceAttack;
    private Vector2 lastAttackDirection;

    // Recoil variables
    private bool isRecoiling;
    private float recoilTimeLeft;
    private int currentRecoilStep;
    private Vector2 recoilDirection;

    private int currentHealth;
    private bool isInvulnerable;
    private float invulnerabilityTimer;
    private float flashTimer;
    private int flashCount;
    private bool isFlashing;
    private float hitTimeTimer;
    private bool isTimeSlowed;

    public static PlayerController Instace { get; private set; }

    void Awake()
    {
        // Singleton pattern implementation
        if (Instace == null)
        {
            Instace = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        
        // Set initial gravity scale
        rb.gravityScale = gravityScale;
        
        if (groundCheck == null)
        {
            Debug.LogError("Ground Check transform is not assigned! Please create an empty GameObject as a child of the player and assign it to the groundCheck field.");
        }

        // Check for attack transforms
        if (sideAttackTransform == null || downAttackTransform == null || upAttackTransform == null)
        {
            Debug.LogError("Attack transforms are not assigned! Please create empty GameObjects as children of the player and assign them to the respective transform fields.");
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetHealthImmediate(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        // Handle time slow
        if (isTimeSlowed)
        {
            hitTimeTimer -= Time.unscaledDeltaTime;
            if (hitTimeTimer <= 0)
            {
                ResumeTime();
            }
        }

        // Handle invulnerability
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
                sr.color = Color.white;
                // Spawn blood spurt when invulnerability ends
                SpawnBloodSpurt();
            }
        }

        // Handle damage flash
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                flashCount++;
                if (flashCount >= damageFlashCount * 2)
                {
                    isFlashing = false;
                    sr.color = Color.white;
                }
                else
                {
                    sr.color = flashCount % 2 == 0 ? Color.white : Color.red;
                    flashTimer = damageFlashDuration;
                }
            }
        }

        // Check if dash should end
        if (isDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter <= 0f)
            {
                EndDash();
            }
            return;
        }

        // Handle recoil
        if (isRecoiling)
        {
            recoilTimeLeft -= Time.deltaTime;
            if (recoilTimeLeft <= 0f)
            {
                currentRecoilStep++;
                if (currentRecoilStep >= recoilXSteps)
                {
                    isRecoiling = false;
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    recoilTimeLeft = recoilStepDuration;
                }
            }
            return;
        }

        // Update attack timer
        if (timeSinceAttack > 0)
        {
            timeSinceAttack -= Time.deltaTime;
        }

        // 1) Horizontal input
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2) Ground check
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundLayer);
        
        // Debug ground check
        // Debug.Log($"Ground Check: Position={groundCheck.position}, IsGrounded={isGrounded}, Layer={groundLayer.value}");

        // Reset double jump when grounded
        if (isGrounded)
        {
            hasDoubleJumped = false;
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 3) Jump buffer
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 4) Jump initiation
        if (jumpBufferCounter > 0f)
        {
            if (isGrounded || coyoteTimeCounter > 0f)
            {
                // Normal jump
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }
            else if (canDoubleJump && !hasDoubleJumped)
            {
                // Double jump
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                hasDoubleJumped = true;
                jumpBufferCounter = 0f;
            }
        }

        // 5) Jump cut
        if ((Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.Space)) 
            && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // 6) Dash
        if (canDash && dashCooldownCounter <= 0f)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                StartDash();
            }
        }
        else
        {
            dashCooldownCounter -= Time.deltaTime;
        }

        // 7) Attack
        if (canAttack && timeSinceAttack <= 0)
        {
            if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                Attack();
            }
        }

        // 8) Update Animator parameters
        bool walking = Mathf.Abs(moveInput) > 0.1f && isGrounded;
        bool jumping = !isGrounded;
        animator.SetBool("Walking", walking);
        animator.SetBool("Jumping", jumping);

        // Flip based on direction
        if (moveInput > 0f)      sr.flipX = false;
        else if (moveInput < 0f) sr.flipX = true;
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        if (isRecoiling)
        {
            // Gradually reduce recoil velocity
            float stepProgress = 1f - ((float)currentRecoilStep / recoilXSteps);
            rb.linearVelocity = recoilDirection * stepProgress;
            return;
        }

        // Apply horizontal movement
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimeCounter = dashTime;
        dashCooldownCounter = dashCooldown;
        
        // Set dash direction based on facing direction
        dashDirection = sr.flipX ? Vector2.left : Vector2.right;
        
        // Trigger dash animation
        animator.SetBool("Dashing", true);
        
        // Disable gravity during dash
        rb.gravityScale = 0f;
    }

    private void EndDash()
    {
        isDashing = false;
        animator.SetBool("Dashing", false);
        rb.gravityScale = gravityScale;
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea)
    {
        // Calculate the position based on facing direction
        Vector2 hitPosition = _attackTransform.position;
        if (sr.flipX)
        {
            // If facing left, mirror the x position relative to the player
            hitPosition.x = transform.position.x - (_attackTransform.position.x - transform.position.x);
        }

        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(
            hitPosition,
            _attackArea,
            0f,
            attackLayer
        );
        bool didHitEnemy = false;
        for (int i = 0; i < objectsToHit.Length; i++)
        {
            Enemy enemy = objectsToHit[i].GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
                didHitEnemy = true;
            }
        }
        if (didHitEnemy)
        {
            Debug.Log("hit");
            // Apply recoil when hitting an enemy
            ApplyRecoil();
        }
    }

    private void ApplyRecoil()
    {
        isRecoiling = true;
        currentRecoilStep = 0;
        recoilTimeLeft = recoilStepDuration;
        
        // Calculate recoil direction based on facing direction
        recoilDirection = new Vector2(
            sr.flipX ? recoilXSpeed : -recoilXSpeed,
            recoilYSpeed
        );
        
        // Apply initial recoil velocity
        rb.linearVelocity = recoilDirection;
    }

    private void Attack()
    {
        isAttacking = true;
        timeSinceAttack = timeBetweenAttack;
        
        // Y-axis directional input
        float verticalInput = Input.GetAxisRaw("Vertical");
        lastAttackDirection = new Vector2(moveInput, verticalInput).normalized;
        
        // Trigger attack animation
        animator.SetTrigger("Attacking");
        
        // Perform attack based on direction
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            if (verticalInput > 0)
            {
                // Up attack
                Hit(upAttackTransform, upAttackArea);
                SpawnSlashEffect(upAttackTransform.position, 90f);
            }
            else
            {
                // Down attack
                Hit(downAttackTransform, downAttackArea);
                SpawnSlashEffect(downAttackTransform.position, -90f);
            }
        }
        else
        {
            // Side attack
            Hit(sideAttackTransform, sideAttackArea);
            float rotation = sr.flipX ? 180f : 0f;
            SpawnSlashEffect(sideAttackTransform.position, rotation);
        }
        
        // Reset attack state after animation
        Invoke(nameof(ResetAttack), timeBetweenAttack);
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    private void SpawnSlashEffect(Vector3 position, float rotation)
    {
        if (slashEffect != null)
        {
            // Calculate the position based on facing direction
            Vector3 slashPosition = position;
            if (sr.flipX)
            {
                // If facing left, mirror the x position relative to the player
                slashPosition.x = transform.position.x - (position.x - transform.position.x);
                // Flip the rotation for side attacks
                if (rotation == 0f)
                {
                    rotation = 180f;
                }
            }

            GameObject slash = Instantiate(slashEffect, slashPosition, Quaternion.Euler(0, 0, rotation));
            Destroy(slash, slashEffectDuration);
        }
    }

    private void SpawnBloodSpurt()
    {
        if (bloodSpurt != null)
        {
            // Spawn the blood spurt at the player's position
            GameObject spurt = Instantiate(bloodSpurt, transform.position, Quaternion.identity);
            // Destroy it after the specified duration
            Destroy(spurt, bloodSpurtDuration);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Draw attack areas
        if (sideAttackTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(sideAttackTransform.position, sideAttackArea);
        }
        
        if (downAttackTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(downAttackTransform.position, downAttackArea);
        }
        
        if (upAttackTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(upAttackTransform.position, upAttackArea);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isInvulnerable) return;

        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Health: {currentHealth}/{maxHealth}");

        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }

        // Slow down time
        SlowTime();

        // Start invulnerability
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;

        // Start damage flash
        isFlashing = true;
        flashCount = 0;
        flashTimer = damageFlashDuration;
        sr.color = Color.red;

        // Trigger take damage animation
        animator.SetTrigger("TakeDamage");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void SlowTime()
    {
        Time.timeScale = hitTimeScale;
        isTimeSlowed = true;
        hitTimeTimer = hitTimeDuration;
    }

    private void ResumeTime()
    {
        Time.timeScale = 1f;
        isTimeSlowed = false;
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Trigger death animation if you have one
        animator.SetTrigger("Die");
        
        // Disable player controls
        enabled = false;
        
        // You might want to trigger game over or respawn logic here
        // For now, we'll just destroy the player
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Make sure time scale is reset when the player is destroyed
        Time.timeScale = 1f;
    }
}
