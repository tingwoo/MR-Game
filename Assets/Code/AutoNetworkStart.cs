using UnityEngine;
using Unity.Netcode; // 引入 Netcode

public class AutoNetworkStart : MonoBehaviour
{
    void Start()
    {
        // 檢查：如果是在 Unity 編輯器裡面，而且目前沒有連線
        if (Application.isEditor && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("【開發模式】自動啟動 Host 模式...");
            NetworkManager.Singleton.StartHost();
        }
    }
}