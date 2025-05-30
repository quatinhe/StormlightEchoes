using UnityEngine;
using Unity.Netcode;

public class FlyingParshendiSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public GameObject flyingParshendiPrefab;
    public int totalSpawns = 10;
    public float spawnInterval = 2f;
    public float spawnRadius = 10f;
    
    private int currentSpawns = 0;
    private float spawnTimer = 0f;
    private bool isSpawning = false;

    public void StartSpawning()
    {
        if (!IsServer) return;
        
        isSpawning = true;
        currentSpawns = 0;
        spawnTimer = 0f;
    }

    private void Update()
    {
        if (!IsServer || !isSpawning) return;

        if (currentSpawns >= totalSpawns)
        {
            isSpawning = false;
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnFlyingParshendi();
            spawnTimer = 0f;
        }
    }

    private void SpawnFlyingParshendi()
    {
        // Calculate random position within spawn radius
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);

        // Spawn the flying Parshendi
        GameObject flyingParshendi = Instantiate(flyingParshendiPrefab, spawnPosition, Quaternion.identity);
        flyingParshendi.GetComponent<NetworkObject>().Spawn();
        
        currentSpawns++;
    }
} 