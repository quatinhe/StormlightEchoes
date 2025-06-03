using Unity.Netcode;
using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    private Animator animator;
    private float animationLength;
    private const float EXTRA_DISPLAY_TIME = 0.5f; 

    void OnEnable()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            
            animator.Rebind();
            animator.Update(0f);
            
            
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                animationLength = clipInfo[0].clip.length;
                Invoke("DestroyObject", animationLength + EXTRA_DISPLAY_TIME);
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

    private void DestroyObject()
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject.HasAuthority)
        {
            networkObject.Despawn();
        }
    }
} 