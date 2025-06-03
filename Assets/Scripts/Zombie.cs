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

        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

       
        if (distanceToPlayer <= detectionRange)
        {
            
            if (distanceToPlayer > attackRange)
            {
                Vector2 direction = (playerTransform.position - transform.position).normalized;
                Move(direction);

               
                if (direction.x > 0 && !isFacingRight || direction.x < 0 && isFacingRight)
                {
                    Flip();
                }
            }
            else
            {
                
                Move(Vector2.zero);
                
                
                if (attackTimer <= 0)
                {
                    Attack();
                    attackTimer = attackCooldown;
                }
            }
        }
        else
        {
           
            Move(Vector2.zero);
        }

       
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    protected override void Attack()
    {
        
        if (Vector2.Distance(transform.position, playerTransform.position) <= attackRange)
        {
            
            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log("Zombie attacks player!");
            }
        }
    }

    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
