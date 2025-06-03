using UnityEngine;
using Unity.Netcode;

public class DestructibleMaterial : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        
       
        if (other.CompareTag("UpSpell") || other.gameObject.name.Contains("UpSpell"))
        {
            Debug.Log("Destructible material hit by up spell!");
            
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public void DestroySelf()
    {
        if (!IsServer) return;
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null)
            networkObject.Despawn();
        else
            Destroy(gameObject);
    }
} 