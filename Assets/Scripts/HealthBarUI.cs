using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : NetworkBehaviour
{
    [Header("Health Bar Settings")]
    [Tooltip("The image component that represents the health bar fill")]
    public Image healthBarFill;
    [Tooltip("How fast the health bar updates")]
    public float updateSpeed = 10f;
    [Tooltip("The background image of the health bar")]
    public Image healthBarBackground;
    
    private GameObject owningPlayer;
    private PlayerController owningController;

    private float targetFill;
    private float currentFill;

    void Awake()
    {
        // Ensure the health bar is properly positioned
        if (healthBarBackground != null)
        {
            RectTransform rectTransform = healthBarBackground.rectTransform;
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(50, -50);
            rectTransform.sizeDelta = new Vector2(200, 20);
        }
    }

    void Start()
    {
        if (healthBarFill == null)
        {
            Debug.LogError("Health Bar Fill image is not assigned!");
            return;
        }

        // Initialize the health bar
        currentFill = 1f;
        targetFill = 1f;
        healthBarFill.fillAmount = 1f;
    }

    void Update()
    {
        //todo: refactor to event/delegate
        InitOwningPlayer();
        
        if(!owningController)
            return;
        
        //todo: change to event/delegate
        UpdateHealthBar(owningController.GetCurrentHealth(), owningController.GetMaxHealth());
        
        // Smoothly update the health bar fill
        if (currentFill != targetFill)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * updateSpeed);
            healthBarFill.fillAmount = currentFill;
        }
    }

    private void InitOwningPlayer()
    {
        if(owningPlayer && owningController)
            return;
        
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            if (player.GetComponent<NetworkObject>().IsLocalPlayer)
            {
                owningPlayer = player;
                owningController = player.GetComponent<PlayerController>();
                break;
            }
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        targetFill = currentHealth / maxHealth;
    }

    public void SetHealthImmediate(float currentHealth, float maxHealth)
    {
        currentFill = targetFill = currentHealth / maxHealth;
        if (healthBarFill != null)
            healthBarFill.fillAmount = currentFill;
    }
}
