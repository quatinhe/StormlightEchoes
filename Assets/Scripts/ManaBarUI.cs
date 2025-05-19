using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [Header("Mana Bar Settings")]
    [Tooltip("The image component that represents the mana bar fill")]
    public Image manaBarFill;
    [Tooltip("How fast the mana bar updates")]
    public float updateSpeed = 10f;

    private float targetFill;
    private float currentFill;

    void Start()
    {
        if (manaBarFill == null)
        {
            Debug.LogError("Mana Bar Fill image is not assigned!");
            return;
        }

        // Initialize the mana bar
        currentFill = 1f;
        targetFill = 1f;
        manaBarFill.fillAmount = 1f;
    }

    void Update()
    {
        // Smoothly update the mana bar fill
        if (currentFill != targetFill)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * updateSpeed);
            manaBarFill.fillAmount = currentFill;
        }
    }

    public void UpdateManaBar(float currentMana, float maxMana)
    {
        targetFill = Mathf.Clamp01(currentMana / maxMana);
    }

    public void SetManaImmediate(float currentMana, float maxMana)
    {
        currentFill = targetFill = Mathf.Clamp01(currentMana / maxMana);
        if (manaBarFill != null)
            manaBarFill.fillAmount = currentFill;
    }
}
