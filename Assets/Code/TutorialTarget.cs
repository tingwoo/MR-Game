using UnityEngine;
using Unity.Netcode;

public class TutorialTarget : NetworkBehaviour
{
    [Header("屬性設定")]
    public GameColor color; // 請在 Inspector 設定這隻精靈的顏色 (Red/Blue...)

    [Header("特效")]
    [SerializeField] private GameObject explosionPrefab;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 只有 Server 處理邏輯
        if (!IsServer) return;

        // 2. 【嚴格判定邏輯】跟 SpiritDestroy 完全一致
        // 條件 A: 撞到的東西 Tag 是 "FullRing"
        // 條件 B: 撞到的東西身上有 NetworkFullRing 腳本，且顏色跟精靈一樣
        if (other.CompareTag("FullRing"))
        {
            var ringScript = other.GetComponent<NetworkFullRing>();

            // 確保腳本存在，且顏色正確
            if (ringScript != null && ringScript.Color == color)
            {
                HandleCapture();
            }
        }
    }

    // 滑鼠點擊測試 (保留方便您除錯，正式版可拿掉)
    private void OnMouseDown()
    {
        if (IsServer)
        {
            Debug.Log($"【測試】滑鼠強制抓取: {color}");
            HandleCapture();
        }
    }

    private void HandleCapture()
    {
        // A. 教學進度 +1
        var status = FindObjectOfType<GameStatusController>();
        if (status != null)
        {
            status.OnTutorialTargetCaptured();
        }

        // B. 播放特效
        Color visualColor = ConvertGameColorToUnityColor(color);
        SpawnExplosionClientRpc(transform.position, visualColor);

        // C. 銷毀物件
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
        else
            Destroy(gameObject);
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position, Color impactColor)
    {
        if (explosionPrefab != null)
        {
            GameObject boom = Instantiate(explosionPrefab, position, Quaternion.identity);
            ExplosionController controller = boom.GetComponent<ExplosionController>();
            if (controller != null) controller.Initialize(impactColor);
        }
    }

    private Color ConvertGameColorToUnityColor(GameColor gameColor)
    {
        switch (gameColor)
        {
            case GameColor.Red: return Color.red;
            case GameColor.Yellow: return Color.yellow;
            case GameColor.Blue: return Color.blue;
            case GameColor.Orange: return new Color(1.0f, 0.5f, 0.0f);
            case GameColor.Green: return Color.green;
            case GameColor.Purple: return new Color(0.5f, 0.0f, 0.5f);
            default: return Color.white;
        }
    }
}