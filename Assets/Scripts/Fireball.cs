using Unity.Netcode;
using UnityEngine;

public class Fireball : NetworkBehaviour
{
    [Header("Fireball Properties")] public float damage = 10f;
    public float hitForce = 5f;
    public int speed = 10;
    public float lifetime = 3f;

    private Rigidbody2D rb;
    private Vector2 direction;

    NetworkObject networkObject;

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

        networkObject = GetComponent<NetworkObject>();

        // Set initial velocity based on the fireball's rotation
        float angle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        rb.linearVelocity = direction * speed;
        Debug.Log($"Fireball velocity set to: {rb.linearVelocity}");
    }

    // Update is called once per frame
    void Update()
    {
        // Add debug visualization
        Debug.DrawRay(transform.position, direction * 0.5f, Color.red);

        UpdateLifetime();
    }

    void UpdateLifetime()
    {
        if (HasAuthority)
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0)
            {
                networkObject.Despawn();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Fireball hit: {other.gameObject.name}");

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy)
        {
            // Deal damage to the enemy
            enemy.TakeDamage(Mathf.RoundToInt(damage));

            // Apply knockback force
            enemy.AddForce(direction * hitForce);

            if (HasAuthority)
            {
                networkObject.Despawn();
            }
        }
    }
}