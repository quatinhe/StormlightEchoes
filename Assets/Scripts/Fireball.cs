using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Fireball Properties")]
    public float damage = 10f;
    public float hitForce = 5f;
    public int speed = 10;
    public float lifetime = 3f;

    private Rigidbody2D rb;
    private Vector2 direction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Fireball Start called");
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Fireball requires a Rigidbody2D component!");
            return;
        }

        // Set initial velocity based on the fireball's rotation
        float angle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        rb.linearVelocity = direction * speed;
        Debug.Log($"Fireball velocity set to: {rb.linearVelocity}");

        // Destroy the fireball after lifetime seconds
        Destroy(gameObject, lifetime);
    }

    // Update is called once per frame
    void Update()
    {
        // Add debug visualization
        Debug.DrawRay(transform.position, direction * 0.5f, Color.red);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Fireball hit: {other.gameObject.name}");
        // Check if we hit an enemy
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Deal damage to the enemy
            enemy.TakeDamage(Mathf.RoundToInt(damage));
            Debug.Log($"Dealt {damage} damage to enemy");

            // Apply knockback force if the enemy has a Rigidbody2D
            Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.AddForce(direction * hitForce, ForceMode2D.Impulse);
            }

            // Destroy the fireball after hitting
            Destroy(gameObject);
        }
    }
}
