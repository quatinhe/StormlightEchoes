using System;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;


public class SessionManager : MonoBehaviour
{
    private string profileName = Guid.NewGuid().ToString()[..30];
    private string sessionName = "session1";
    private int maxPlayers = 20;
    private ConnectionState state = ConnectionState.Disconnected;
    private ISession session;
    private NetworkManager networkManager;
    
    private enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    private async void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        networkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        await UnityServices.InitializeAsync();
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (networkManager.LocalClient.IsSessionOwner)
        {
            Debug.Log($"Client-{networkManager.LocalClientId} is the session owner!");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (networkManager.LocalClientId == clientId)
        {
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");
        }
    }
    
    private void OnDestroy()
    {
        session?.LeaveAsync();
    }

    async void Start()
    {
        try
        {
            state = ConnectionState.Connecting;
            
            AuthenticationService.Instance.SwitchProfile(profileName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var options = new SessionOptions()
            {
                Name = sessionName,
                MaxPlayers = maxPlayers
            }.WithDistributedAuthorityNetwork();

            session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);

            state = ConnectionState.Connected;
        }
        catch (Exception e)
        {
            state = ConnectionState.Disconnected;
            Debug.LogException(e);
        }
    }
}