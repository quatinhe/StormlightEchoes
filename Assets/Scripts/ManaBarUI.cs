using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [Header("Mana Bar Settings")] [Tooltip("The image component that represents the mana bar fill")]
    public Image manaBarFill;

    [Tooltip("How fast the mana bar updates")]
    public float updateSpeed = 10f;

    private GameObject owningPlayer;
    private PlayerController owningController;

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
        //todo: refactor to event/delegate
        InitOwningPlayer();

        if (!owningController)
            return;

        //todo: change to event/delegate
        UpdateManaBar(owningController.GetCurrentMana(), owningController.GetMaxMana());

        // Smoothly update the mana bar fill
        if (currentFill != targetFill)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * updateSpeed);
            manaBarFill.fillAmount = currentFill;
        }
    }

    private void InitOwningPlayer()
    {
        if (owningPlayer && owningController)
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