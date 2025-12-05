using UnityEngine;
using Unity.Netcode;

public class TutorialTarget : NetworkBehaviour
{
    [Header("顏色設定")]
    public Color impactColor = Color.red; // 請在 Inspector 設定對應顏色
    
    [Header("特效")]
    public GameObject explosionPrefab; // 請拖入 Explosion Prefab

    // 1. 手把碰撞偵測
    private void OnTriggerEnter(Collider other)
    {
        // 只有 Server 有權力處理邏輯
        if (!IsServer) return;

        // 檢查是否為手把 (或是 FullRing)
        if (other.CompareTag("PlayerHand") || other.CompareTag("FullRing")) 
        {
            HandleCapture();
        }
    }

    // 2. 滑鼠點擊偵測 (測試用)
    private void OnMouseDown()
    {
        // 滑鼠點擊也必須在 Server (Host) 端執行才有效
        if (!IsServer) return;

        Debug.Log("【測試】滑鼠點擊了教學靶子！");
        HandleCapture();
    }

    // 3. 核心邏輯 (把重複的程式碼抽出來)
    private void HandleCapture()
    {
        // A. 通知管理器 (教學進度 +1)
        var status = FindObjectOfType<GameStatusController>();
        if(status != null)
        {
            status.OnTutorialTargetCaptured();
        }

        // B. 播放特效 (通知所有客戶端)
        if (explosionPrefab != null)
        {
            SpawnExplosionClientRpc(transform.position, impactColor);
        }

        // C. 銷毀自己 (同步銷毀)
        if (NetworkObject != null && NetworkObject.IsSpawned) 
            NetworkObject.Despawn();
        else
            Destroy(gameObject);
    }

    // 4. RPC 廣播特效
    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position, Color color)
    {
        // 生成特效物件
        GameObject boom = Instantiate(explosionPrefab, position, Quaternion.identity);
        
        // 設定顏色 (呼叫你們原本的 ExplosionController)
        ExplosionController controller = boom.GetComponent<ExplosionController>();
        if (controller != null)
        {
            controller.Initialize(color);
        }
    }
}