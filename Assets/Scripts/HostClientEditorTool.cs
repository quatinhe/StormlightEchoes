using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Linq;
using Unity.Netcode.Transports.UTP;


public class HostClientEditorTool : MonoBehaviour
{
    [SerializeField] private bool auto = true;
    [SerializeField] private bool client = false;
    [SerializeField] private String serverUrl = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7777;
    
    void Start()
    {
/*#if UNITY_EDITOR*/
        if (auto)
        {
            string[] args = Environment.GetCommandLineArgs();
            bool isVirtualClone = Array.Exists(args, arg => arg == "--virtual-project-clone");

            if (!isVirtualClone)
            {
                Debug.Log("Main Editor Instance: Starting as Host");

                // Check if NetworkManager is already running
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer ||
                    NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("[HostClientEditorTool] NetworkManager already running, skipping host start");
                    return;
                }

                NetworkManager.Singleton.StartHost();
            }
            else
            {
                Debug.Log("Cloned Editor Instance: Starting as Client");
                StartCoroutine(DelayedClientStart());
            }
        }
/*#endif*/
        else if (client)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(serverUrl, serverPort);
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(serverUrl, serverPort);
            NetworkManager.Singleton.StartHost();
        }
    }
    
    private IEnumerator DelayedClientStart()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("Cloned Editor Instance: Starting as Client");
        NetworkManager.Singleton.StartClient();
    }
}