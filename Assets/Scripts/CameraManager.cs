using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Ensures the camera is always properly following the local player
/// This script helps fix camera issues after scene transitions
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool autoFindCamera = true;
    [SerializeField] private float checkInterval = 1.0f;
    
    [Header("Camera Follow Settings")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float defaultSmoothTime = 0.3f;
    
    private CameraFollow cameraFollow;
    private float lastCheckTime;
    
    private void Start()
    {
        SetupCamera();
    }
    
    private void Update()
    {
        // Periodically check if camera needs to be reconnected
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            CheckCameraConnection();
        }
    }
    
    private void SetupCamera()
    {
        // Find camera if not assigned
        if (targetCamera == null && autoFindCamera)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }
        
        if (targetCamera == null)
        {
            Debug.LogError("[CameraManager] No camera found in scene!");
            return;
        }
        
        // Get or add CameraFollow component
        cameraFollow = targetCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = targetCamera.gameObject.AddComponent<CameraFollow>();
            cameraFollow.offset = defaultOffset;
            cameraFollow.smoothTime = defaultSmoothTime;
            Debug.Log("[CameraManager] Added CameraFollow component to camera");
        }
        
        // Try to connect to local player
        ConnectToLocalPlayer();
    }
    
    private void CheckCameraConnection()
    {
        if (cameraFollow == null || cameraFollow.target != null)
            return; // Camera is fine or doesn't exist
        
        // Camera lost its target, try to reconnect
        ConnectToLocalPlayer();
    }
    
    private void ConnectToLocalPlayer()
    {
        if (cameraFollow == null)
            return;
        
        // Find the local player
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.IsOwner)
            {
                cameraFollow.target = player.transform;
                Debug.Log($"[CameraManager] Connected camera to local player: {player.name}");
                return;
            }
        }
        
        // If no local player found yet, that's okay - they might spawn later
        if (players.Length == 0)
        {
            Debug.Log("[CameraManager] No players found yet, will retry...");
        }
        else
        {
            Debug.LogWarning("[CameraManager] Found players but none are local owner");
        }
    }
    
    /// <summary>
    /// Force reconnect the camera to the local player (useful for scene transitions)
    /// </summary>
    public void ForceReconnect()
    {
        SetupCamera();
    }
    
    /// <summary>
    /// Manually set the camera target
    /// </summary>
    public void SetCameraTarget(Transform target)
    {
        if (cameraFollow != null)
        {
            cameraFollow.target = target;
            Debug.Log($"[CameraManager] Camera target manually set to: {target.name}");
        }
    }
} 