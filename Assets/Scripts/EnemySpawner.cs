using System;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public GameObject enemy;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (HasAuthority)
        {
            GameObject spawned = Instantiate(enemy, transform.position, transform.rotation);
            spawned.GetComponent<NetworkObject>().Spawn();
        }

        Destroy(this);
    }
}