using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 16f;
    [Tooltip("How much to reduce upward velocity when jump is released")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private float moveInput;
    private bool isGrounded;

    public static PlayerController Instace;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr       = GetComponent<SpriteRenderer>();
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
        // 1) Horizontal input
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2) Ground check
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundLayer);

        // 3) Jump initiation
        if (isGrounded && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        // 4) Jump cut
        if ((Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.Space)) 
            && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // 5) Update Animator parameters
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
        // 6) Apply horizontal movement
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }


    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
