using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")] public float moveSpeed = 5f;
    [Tooltip("How strong the gravity is")] public float gravityScale = 1f;

    [Header("Jump")] public float jumpForce = 16f;

    [Tooltip("How much to reduce upward velocity when jump is released")] [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;

    [Tooltip("How long to buffer jump input")]
    public float jumpBufferTime = 0.2f;

    [Tooltip("How long to allow jumping after leaving a platform")]
    public float coyoteTime = 0.2f;

    [Tooltip("Can the player double jump?")]
    public NetworkVariable<bool> canDoubleJump = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Dash")] [Tooltip("Can the player dash?")]
    public bool canDash = true;

    [Tooltip("How fast the dash is")] public float dashSpeed = 30f;
    [Tooltip("How long the dash lasts")] public float dashTime = 0.3f;

    [Tooltip("How long until the player can dash again")]
    public float dashCooldown = 1f;

    [Header("Attack")] [Tooltip("Can the player attack?")]
    public bool canAttack = true;

    [Tooltip("Time between attacks")] public float timeBetweenAttack = 0.5f;

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

    [Header("Spellcasting")] [Tooltip("Can the player cast spells?")]
    public bool canCast = true;

    [Tooltip("Can the player cast side spells?")]
    public NetworkVariable<bool> canCastSideSpell = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Tooltip("Time between spell casts")] public float timeBetweenCast = 0.5f;

    [Tooltip("How much mana each spell costs")]
    public float manaSpellCost = 20f;

    [Tooltip("Side spell prefab")] public GameObject sideSpell;
    [Tooltip("Up spell prefab")] public GameObject upSpell;

    [Tooltip("Down spell GameObject (child of player)")]
    public GameObject downSpell;

    [Tooltip("Transform for side spell spawn point")]
    public Transform sideSpellSpawnPoint;

    [Tooltip("Transform for up spell spawn point")]
    public Transform upSpellSpawnPoint;

    [Header("Recoil")] [Tooltip("Number of steps in X direction recoil")]
    public int recoilXSteps = 3;

    [Tooltip("Number of steps in Y direction recoil")]
    public int recoilYSteps = 2;

    [Tooltip("Speed of X direction recoil")]
    public float recoilXSpeed = 10f;

    [Tooltip("Speed of Y direction recoil")]
    public float recoilYSpeed = 8f;

    [Tooltip("How long each recoil step lasts")]
    public float recoilStepDuration = 0.1f;

    [Header("Ground Check")] public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Tooltip("How far below the player to check for ground")]
    public float groundCheckDistance = 0.1f;

    [Header("Attack Damage")] public int attackDamage = 1;

    [Header("Health")] [Tooltip("Maximum health of the player")]
    public int maxHealth = 3;

    [Tooltip("How long the player is invulnerable after taking damage")]
    public float invulnerabilityDuration = 1f;

    [Tooltip("How many times the player flashes when taking damage")]
    public int damageFlashCount = 3;

    [Tooltip("How long each flash lasts")] public float damageFlashDuration = 0.1f;
    [Tooltip("Blood spurt effect prefab")] public GameObject bloodSpurt;

    [Tooltip("How long the blood spurt effect lasts")]
    public float bloodSpurtDuration = 0.5f;

    [Tooltip("How much to slow down time when hit (0.1 = 10% speed)")] [Range(0.01f, 1f)]
    public float hitTimeScale = 0.1f;

    [Tooltip("How long the time slow effect lasts")]
    public float hitTimeDuration = 0.1f;

    [Header("Healing")] [Tooltip("How long it takes to heal one health")]
    public float healTime = 1.5f;

    [Tooltip("How much health to restore per heal")]
    public int healAmount = 1;

    [Header("Mana Settings")] [Tooltip("Current mana of the player")]
    public NetworkVariable<float> currentMana = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    [Tooltip("How fast mana drains while healing (per second)")]
    public float manaDrainSpeed = 20f;

    [Tooltip("How much mana to gain when hitting an enemy")]
    public float manaGain = 10f;

    [Tooltip("Maximum mana the player can have")]
    public float maxMana = 100f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    private NetworkVariable<float> moveInput = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private bool wantJump;
    private bool wantStopJump;

    private bool isGrounded;
    private bool wasGrounded;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool canJump;
    private bool hasDoubleJumped;

    // Dash variables
    private NetworkVariable<bool> isDashing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

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

    // Spellcasting variables
    private NetworkVariable<bool> isCasting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private float timeSinceCast;

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private bool isInvulnerable;
    private float invulnerabilityTimer;
    private float flashTimer;
    private int flashCount;
    private bool isFlashing;
    private float hitTimeTimer;
    private bool isTimeSlowed;

    private bool wantHeal = false;

    private NetworkVariable<bool> isHealing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private float healTimer;

    private bool healButtonHeldLastFrame;

    public static PlayerController Instace { get; private set; }


    public void AddMovementInput(Vector2 input)
    {
        moveInput.Value = input.x;
    }

    public void SetWantJump(bool newWantJump)
    {
        wantJump = newWantJump;
    }

    public void SetWantStopJump(bool newWantStopJump)
    {
        wantStopJump = newWantStopJump;
    }

    void Awake()
    {
        // Initialize components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Set initial gravity scale
        rb.gravityScale = gravityScale;

        if (groundCheck == null)
        {
            Debug.LogError(
                "Ground Check transform is not assigned! Please create an empty GameObject as a child of the player and assign it to the groundCheck field.");
        }

        // Check for attack transforms
        if (sideAttackTransform == null || downAttackTransform == null || upAttackTransform == null)
        {
            Debug.LogError(
                "Attack transforms are not assigned! Please create empty GameObjects as children of the player and assign them to the respective transform fields.");
        }

        isDashing.OnValueChanged += (value, newValue) => animator.SetBool("Dashing", newValue);
        isHealing.OnValueChanged += (value, newValue) => animator.SetBool("Healing", newValue);
        isCasting.OnValueChanged += (value, newValue) => animator.SetBool("Casting", newValue);

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    void Start()
    {
        if (IsOwner)
        {
            currentHealth.Value = maxHealth;
        }
    }

    void Update()
    {
        HandleDamageFlash();
        HandleInvulnerability();

        if (moveInput.Value > 0f)
        {
            sr.flipX = false;
        }
        else if (moveInput.Value < 0f)
        {
            sr.flipX = true;
        }

        // 2) Ground check
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundLayer);

        bool walking = Mathf.Abs(moveInput.Value) > 0.1f && isGrounded;
        bool jumping = !isGrounded;

        // 8) Update Animator parameters
        animator.SetBool("Walking", walking);
        animator.SetBool("Jumping", jumping);

        if (!IsOwner)
        {
            return;
        }

        // Handle time slow
        if (isTimeSlowed)
        {
            hitTimeTimer -= Time.unscaledDeltaTime;
            if (hitTimeTimer <= 0)
            {
                ResumeTime();
            }
        }

        dashCooldownCounter -= Time.deltaTime;
        // Check if dash should end
        if (isDashing.Value)
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

        // Update spell cast timer
        if (timeSinceCast > 0)
        {
            timeSinceCast -= Time.deltaTime;
        }

        // 3) Jump buffer
        if (wantJump)
        {
            jumpBufferCounter = jumpBufferTime;
            wantJump = false;
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
            else if (canDoubleJump.Value && !hasDoubleJumped)
            {
                // Double jump
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                hasDoubleJumped = true;
                jumpBufferCounter = 0f;
            }
        }

        // 5) Jump cut
        if (wantStopJump && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        if (isGrounded)
        {
            hasDoubleJumped = false;
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        HandleHeal();
    }

    public void StartHeal()
    {
        wantHeal = true;
    }

    public void EndHeal()
    {
        isHealing.Value = false;
        wantHeal = false;
    }

    private void HandleHeal()
    {
        if (!IsOwner)
            return;

        if (!wantHeal)
        {
            isHealing.Value = false;
            return;
        }

        // Healing logic (Hollow Knight style: must hold for healTime, only heal once per hold)
        bool healButtonHeld = Input.GetMouseButton(1);

        if (!isHealing.Value && currentHealth.Value < maxHealth && IsIdle() && healButtonHeld &&
            !healButtonHeldLastFrame &&
            currentMana.Value > 0f)
        {
            // Start healing charge
            isHealing.Value = true;
            healTimer = healTime;
        }

        if (isHealing.Value)
        {
            // Interrupt healing if player moves, jumps, attacks, dashes, takes damage, or runs out of mana
            if (!IsIdle() || isDashing.Value || isRecoiling || isAttacking || isInvulnerable || currentMana.Value <= 0f)
            {
                isHealing.Value = false;
            }
            else
            {
                healTimer -= Time.deltaTime;
                currentMana.Value -= manaDrainSpeed * Time.deltaTime;
                currentMana.Value = Mathf.Clamp(currentMana.Value, 0, maxMana);
                if (healTimer <= 0f)
                {
                    Heal(healAmount);
                    isHealing.Value = false;
                }
            }
        }

        healButtonHeldLastFrame = healButtonHeld;
    }

    void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        if (isDashing.Value)
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
        rb.linearVelocity = new Vector2(moveInput.Value * moveSpeed, rb.linearVelocity.y);
    }

    public void TryStartDash()
    {
        if (canDash && dashCooldownCounter <= 0f)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        isDashing.Value = true;
        dashTimeCounter = dashTime;
        dashCooldownCounter = dashCooldown;

        // Set dash direction based on facing direction
        dashDirection = sr.flipX ? Vector2.left : Vector2.right;

        // Trigger dash animation
        //animator.SetBool("Dashing", true);

        // Disable gravity during dash
        rb.gravityScale = 0f;
    }

    [ServerRpc]
    private void StartDash_ServerRpc()
    {
        isDashing.Value = true;
    }

    private void EndDash()
    {
        isDashing.Value = false;
        //animator.SetBool("Dashing", false);

        rb.gravityScale = gravityScale;
    }

    [ServerRpc]
    private void EndDash_ServerRpc()
    {
        isDashing.Value = false;
    }

    //todo: move to UI
    private void HandleDamageFlash()
    {
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
    }

    private void HandleInvulnerability()
    {
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
                sr.color = Color.white;

                SpawnBloodSpurt();
            }
        }
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea, bool isUpAttack)
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
                enemy.ApplyDamage_ServerRpc(attackDamage);

                didHitEnemy = true;
            }
            // Only destroy destructible if up attack
            DestructibleMaterial destructible = objectsToHit[i].GetComponent<DestructibleMaterial>();
            if (destructible != null && isUpAttack)
            {
                destructible.DestroySelf();
            }
        }

        if (didHitEnemy)
        {
            Debug.Log("hit");

            //todo: should be encapsulated inside setmana()
            currentMana.Value = Mathf.Clamp(currentMana.Value += manaGain, 0, maxMana);

            // Apply recoil when hitting an enemy
            ApplyRecoil();
        }
    }

    [ServerRpc]
    private void PlayAttackAnimation_ServerRpc()
    {
        animator.SetTrigger("Attacking");
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

    public void TryAttack()
    {
        if (canAttack && timeSinceAttack <= 0)
        {
            Attack();
        }
    }

    private void Attack()
    {
        isAttacking = true;
        timeSinceAttack = timeBetweenAttack;

        // Y-axis directional input
        float verticalInput = Input.GetAxisRaw("Vertical");
        lastAttackDirection = new Vector2(moveInput.Value, verticalInput).normalized;

        // Trigger attack animation
        animator.SetTrigger("Attacking");
        PlayAttackAnimation_ServerRpc();

        Vector2 attackArea;
        Transform attackTransform;
        float rotation = 0f;
        bool isUpAttack = false;

        // Perform attack based on direction
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            if (verticalInput > 0)
            {
                // Up attack
                attackTransform = upAttackTransform;
                rotation = 90f;
                attackArea = upAttackArea;
                isUpAttack = true;
            }
            else
            {
                attackTransform = downAttackTransform;
                rotation = -90f;
                attackArea = downAttackArea;
            }
        }
        else
        {
            attackTransform = sideAttackTransform;
            rotation = sr.flipX ? 180f : 0f;
            attackArea = sideAttackArea;
        }

        // Pass isUpAttack to Hit
        Hit(attackTransform, attackArea, isUpAttack);
        SpawnSlashEffect(attackTransform.position, rotation);

        //todo: Remove variables from rpc to make them less
        if (!IsServer)
            SpawnSlashEffect_ServerRpc(attackTransform.position, rotation);
        else
            SpawnSlashEffect_ClientRpc(attackTransform.position, rotation);

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

    [ServerRpc]
    private void SpawnSlashEffect_ServerRpc(Vector3 position, float rotation)
    {
        SpawnSlashEffect(position, rotation);
        SpawnSlashEffect_ClientRpc(position, rotation);
    }

    [ClientRpc]
    private void SpawnSlashEffect_ClientRpc(Vector3 position, float rotation)
    {
        SpawnSlashEffect(position, rotation);
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
        if (!IsOwner)
            return;

        if (isInvulnerable)
            return;

        currentHealth.Value -= amount;

        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;

        Debug.Log($"Player took {amount} damage. Health: {currentHealth}/{maxHealth}");
        //OnHealthChanged(oldValue, currentHealth.Value);
    }

    protected void OnHealthChanged(int OldValue, int NewValue)
    {
        //
        if (NewValue > OldValue)
        {
            return;
        }
        else
        {
            if (IsLocalPlayer)
            {
                SlowTime();
            }

            // Start damage flash
            isFlashing = true;
            flashCount = 0;
            flashTimer = damageFlashDuration;
            sr.color = Color.red;

            // Trigger take damage animation
            animator.SetTrigger("TakeDamage");

            if (NewValue <= 0)
            {
                Die();
            }
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
        animator.SetTrigger("Die");
        enabled = false;

        Debug.Log("Player died!");

        // You might want to trigger game over or respawn logic here
        // For now, we'll just despawn the player with delay
        if (HasAuthority)
        {
            StartCoroutine(DestroyPlayer(1f * Time.timeScale));
        }
    }
    
    IEnumerator DestroyPlayer(float delay)
    {
        yield return new WaitForSeconds(delay);
        GetComponent<NetworkObject>().Despawn();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (HasAuthority)
        {
            // Make sure time scale is reset when the player is destroyed
            Time.timeScale = 1f;
        }
    }

    private bool IsIdle()
    {
        return Mathf.Abs(moveInput.Value) < 0.01f && isGrounded && !isDashing.Value && !isRecoiling && !isAttacking &&
               !isInvulnerable;
    }

    private void Heal(int amount)
    {
        if(!IsOwner)
            return;
        
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth);
        // Optionally trigger a heal animation or effect here
    }

    public void TryCastSpell()
    {
        if (canCast && timeSinceCast <= 0 && currentMana.Value >= manaSpellCost)
        {
            CastSpell();
        }
    }


    private void CastSpell()
    {
        // Y-axis directional input
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Cast spell based on direction
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            isCasting.Value = true;
            timeSinceCast = timeBetweenCast;

            if (verticalInput > 0)
            {
                // Up spell
                if (upSpell == null)
                {
                    Debug.LogError("Up spell prefab is not assigned!");
                    return;
                }

                if (upSpellSpawnPoint == null)
                {
                    Debug.LogError(
                        "Up spell spawn point is not assigned! Please create an empty GameObject as a child of the player and assign it to the Up Spell Spawn Point field in the PlayerController component.");
                    return;
                }
                
                // Deduct mana cost
                currentMana.Value = Mathf.Clamp(currentMana.Value - manaSpellCost, 0, maxMana);

                // Start the delayed spawn coroutine for up spell
                UpSpellSpawn_ServerRpc();
            }
            else
            {
                // Deduct mana cost
                currentMana.Value = Mathf.Clamp(currentMana.Value - manaSpellCost, 0, maxMana);
                
                // Down spell (activate child GameObject)
                if (downSpell != null)
                {
                    downSpell.SetActive(false); // Reset if needed
                    downSpell.SetActive(true); // Activate effect
                }
            }

            // Reset casting state after animation
            Invoke(nameof(ResetCasting), timeBetweenCast);
        }
        else
        {
            // Side spell
            if (!canCastSideSpell.Value)
            {
                Debug.Log("Side spell not unlocked yet!");
                return;
            }

            if (sideSpell == null)
            {
                Debug.LogError("Side spell prefab is not assigned!");
                return;
            }

            if (sideSpellSpawnPoint == null)
            {
                Debug.LogError(
                    "Side spell spawn point is not assigned! Please create an empty GameObject as a child of the player and assign it to the Side Spell Spawn Point field in the PlayerController component.");
                return;
            }
                        
            // Deduct mana cost
            currentMana.Value = Mathf.Clamp(currentMana.Value - manaSpellCost, 0, maxMana);

            isCasting.Value = true;
            timeSinceCast = timeBetweenCast;

            //todo: Add client prediction to fireball spawn
            FireballSpawn_ServerRpc();

            // Reset casting state after animation
            Invoke(nameof(ResetCasting), timeBetweenCast);
        }
    }

    [ServerRpc]
    private void UpSpellSpawn_ServerRpc()
    {
        StartCoroutine(DelayedUpSpellSpawn());
    }

    private IEnumerator DelayedUpSpellSpawn()
    {
        // Wait for 0.1 seconds
        yield return new WaitForSeconds(0.1f);

        // Use the spawn point's position
        Vector3 spawnPosition = upSpellSpawnPoint.position;
        Debug.Log($"Up spell spawn point position: {spawnPosition}");

        GameObject spell = Instantiate(upSpell, spawnPosition, Quaternion.Euler(0, 0, 0));
        spell.GetComponent<NetworkObject>().Spawn();

        if (spell == null)
        {
            Debug.LogError("Failed to instantiate up spell!");
        }
        else
        {
            Debug.Log("Up spell instantiated successfully!");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            if (Camera.main != null)
            {
                Camera.main.GetComponent<CameraFollow>().target = transform;
            }
        }
    }

    [ServerRpc]
    private void FireballSpawn_ServerRpc()
    {
        StartCoroutine(DelayedFireballSpawn());
    }

    private IEnumerator DelayedFireballSpawn()
    {
        // Wait for 0.1 seconds
        yield return new WaitForSeconds(0.1f);

        float rotation = sr.flipX ? 180f : 0f;
        // Use the spawn point's position
        Vector3 spawnPosition = sideSpellSpawnPoint.position;
        Debug.Log($"Side spell spawn point position: {spawnPosition}");
        Debug.Log($"Player position: {transform.position}");
        Debug.Log($"Side spell spawn point is child of player: {sideSpellSpawnPoint.IsChildOf(transform)}");

        GameObject fireball = Instantiate(sideSpell, spawnPosition, Quaternion.Euler(0, 0, rotation));
        fireball.GetComponent<NetworkObject>().Spawn();

        if (fireball == null)
        {
            Debug.LogError("Failed to instantiate fireball!");
        }
        else
        {
            Debug.Log("Fireball instantiated successfully!");
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth.Value;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetCurrentMana()
    {
        return currentMana.Value;
    }

    public float GetMaxMana()
    {
        return maxMana;
    }

    private void ResetCasting()
    {
        isCasting.Value = false;
    }

    public void EnableDoubleJump()
    {
        if (!IsServer) return;
        canDoubleJump.Value = true;
    }

    public void EnableSideSpell()
    {
        if (!IsServer) return;
        canCastSideSpell.Value = true;
    }

    public void EnableDash()
    {
        if (!IsServer) return;
        canDash = true;
    }
}