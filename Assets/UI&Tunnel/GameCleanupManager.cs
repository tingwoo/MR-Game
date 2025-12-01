using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameCleanupManager : NetworkBehaviour
{
    // Static instance for easy access
    public static GameCleanupManager Instance { get; private set; }

    // List to track all active spirits
    private readonly List<NetworkObject> _spawnedGameObjects = new List<NetworkObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void RegisterObject(NetworkObject netObject)
    {
        // Only the server registers and tracks objects
        if (!IsServer) return;

        if (netObject != null && !_spawnedGameObjects.Contains(netObject))
        {
            _spawnedGameObjects.Add(netObject);
            
            // 【已刪除】原本報錯的那行 netObj.onDespawn... 我們不需要它了
            // 清理邏輯會自動處理已銷毀的物件
        }
    }
    
    // --- THE CLEANUP FUNCTION ---
    public void CleanupAllDynamicObjectsServer()
    {
        if (!IsServer) return;

        // 移除列表中的空物件 (已經在遊戲中被銷毀的)
        // RemoveAll 是 List 的內建功能，會把所有 null 的項目清掉
        _spawnedGameObjects.RemoveAll(x => x == null || !x.IsSpawned);

        Debug.Log($"SERVER: Cleaning up {_spawnedGameObjects.Count} dynamic game objects...");

        // 銷毀剩下的物件
        for (int i = _spawnedGameObjects.Count - 1; i >= 0; i--)
        {
            NetworkObject netObj = _spawnedGameObjects[i];
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true); 
            }
        }
        _spawnedGameObjects.Clear();
    }
}