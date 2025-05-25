using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class PlayerSpawnManager : MonoBehaviour
{
    public List<Transform> spawnPositions = new List<Transform>() { };

    private NetworkManager networkManager;
    private int spawnIndex = 0;

    /// <summary>
    /// Get a spawn position for a spawned object based on the spawn method.
    /// </summary>
    /// <returns>?The spawn position.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public Vector3 GetNextSpawnPosition()
    {
        spawnIndex = (spawnIndex + 1) % spawnPositions.Count;
        return spawnPositions[spawnIndex].position;
    }

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        networkManager.ConnectionApprovalCallback += ConnectionApprovalWithRandomSpawnPos;
    }

    void ConnectionApprovalWithRandomSpawnPos(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Here we are only using ConnectionApproval to set the player's spawn position. Connections are always approved.
        response.CreatePlayerObject = true;
        response.Position = GetNextSpawnPosition();
        response.Rotation = Quaternion.identity;
        response.Approved = true;
    }
}