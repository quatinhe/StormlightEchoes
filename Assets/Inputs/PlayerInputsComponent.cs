using Unity.Netcode;
using UnityEngine;

public class PlayerInputsComponent : NetworkBehaviour
{
    private PlayerInputs _playerInputs;
    private PlayerInputs.PlayerActions _playerActions;

    private PlayerController _playerController;

    private void Awake()
    {
        _playerInputs = new PlayerInputs();
        _playerActions = _playerInputs.Player;

        _playerController = GetComponent<PlayerController>();

        //_playerActions.Jump.performed += ctx => _playerController.StartJump();
        _playerActions.Attack.performed += ctx => _playerController.TryAttack();
        _playerActions.Dash.performed += ctx => _playerController.TryStartDash();
        _playerActions.Spell.performed += ctx => _playerController.TryCastSpell();
        _playerActions.Heal.performed += ctx => _playerController.StartHeal();
        _playerActions.Heal.canceled += ctx => _playerController.EndHeal();
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsLocalPlayer;
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        enabled = false;
        base.OnNetworkDespawn();
    }

    void Update()
    {
        _playerController.AddMovementInput(_playerActions.Move.ReadValue<Vector2>());
        
        _playerActions.Move.ReadValue<Vector2>();
        
        _playerController.SetWantJump(_playerActions.Jump.WasPressedThisFrame());
        _playerController.SetWantStopJump(_playerActions.Jump.WasReleasedThisFrame());
    }

    private void OnEnable()
    {
        _playerActions.Enable();
    }

    private void OnDisable()
    {
        _playerActions.Disable();
    }
}