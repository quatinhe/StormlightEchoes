using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("The target Transform to follow (your player)")]
    public Transform target;

    [Tooltip("How quickly the camera catches up to the target")]
    public float smoothTime = 0.2f;

    [Tooltip("Offset from the target's position")]
    public Vector3 offset = new Vector3(0f, 1f, -5f);

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position = target position + offset
        Vector3 targetPosition = target.position + offset;

        // Smoothly move the camera towards that position
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );
    }
}
