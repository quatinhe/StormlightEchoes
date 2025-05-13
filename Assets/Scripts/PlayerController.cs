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

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    [Tooltip("How far below the player to check for ground")]
    public float groundCheckDistance = 0.1f;

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

    public static PlayerController Instace;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        
        // Set initial gravity scale
        rb.gravityScale = gravityScale;
        
        if (groundCheck == null)
        {
            Debug.LogError("Ground Check transform is not assigned! Please create an empty GameObject as a child of the player and assign it to the groundCheck field.");
        }

        if (Instace == null)
        {
            Instace = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
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

        // 1) Horizontal input
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2) Ground check
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundLayer);
            
        // Debug ground check
        Debug.Log($"Ground Check: Position={groundCheck.position}, IsGrounded={isGrounded}, Layer={groundLayer.value}");

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

        // 7) Update Animator parameters
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

        // 8) Apply horizontal movement
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

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
