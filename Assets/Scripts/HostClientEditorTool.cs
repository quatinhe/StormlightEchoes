using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Linq;


public class HostClientEditorTool : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        string[] args = Environment.GetCommandLineArgs();
        bool isVirtualClone = Array.Exists(args, arg => arg == "--virtual-project-clone");
        
        if (!isVirtualClone)
        {
            Debug.Log("Main Editor Instance: Starting as Host");
            
            // Check if NetworkManager is already running
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
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
#endif
    }
    
    private IEnumerator DelayedClientStart()
    {
#if UNITY_EDITOR
        yield return new WaitForSeconds(2f);
        Debug.Log("Cloned Editor Instance: Starting as Client");
        NetworkManager.Singleton.StartClient();
#endif
    }
}