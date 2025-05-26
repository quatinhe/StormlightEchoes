using System;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Spawner Settings")]
    [Tooltip("The enemy prefab to spawn")]
    public GameObject enemy;
    [Tooltip("Enable/disable enemy spawning")]
    public bool shouldSpawn = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (HasAuthority && shouldSpawn)
        {
            GameObject spawned = Instantiate(enemy, transform.position, transform.rotation);
            spawned.GetComponent<NetworkObject>().Spawn();
        }

        Destroy(this);
    }
}