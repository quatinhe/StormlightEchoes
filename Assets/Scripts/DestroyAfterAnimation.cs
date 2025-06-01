using Unity.Netcode;
using UnityEngine;

public class DisableAfterAnimation : MonoBehaviour
{
    private Animator animator;
    private float animationLength;
    private const float EXTRA_DISPLAY_TIME = 0.5f; // Extra time in seconds

    void OnEnable()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Reset the animator to play from the beginning
            animator.Rebind();
            animator.Update(0f);
            
            // Get the length of the current animation clip
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                animationLength = clipInfo[0].clip.length;
                Invoke("DisableObject", animationLength + EXTRA_DISPLAY_TIME);
            }
            else
            {
                Debug.LogWarning("No animation clip found on " + gameObject.name);
                gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("No Animator component found on " + gameObject.name);
            gameObject.SetActive(false);
        }
    }

    private void DisableObject()
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject.HasAuthority)
        {
            networkObject.Despawn();
        }
    }
} 