using UnityEngine;
using Unity.Netcode;

public class SpiritDestroy : NetworkBehaviour
{
    public GameColor color;

    [Header("VFX References")]
    [SerializeField] private GameObject explosionPrefab;

    // 分數設定現在由 GameStatusController 統一管理，這裡只是為了相容舊設定
    // [SerializeField] private float scoreAmount = 20f; 

    [Header("Audio Settings")]
    [SerializeField] private AudioClip destroySound;
    [Range(0f, 1f)][SerializeField] private float soundVolume = 1.0f;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 只有 Server 處理碰撞邏輯
        if (!IsServer) return;

        // 檢查是否撞到 FullRing 且顏色正確
        if (other.CompareTag("FullRing") && other.gameObject.GetComponent<FullRing>().color == color)
        {
            Color visualColor = ConvertGameColorToUnityColor(color);

            // 2. 視覺與音效同步
            SpawnExplosionClientRpc(transform.position, visualColor);

            // 3. 手把震動
            other.gameObject.GetComponent<FullRing>().PlayHaptics();

            // 4. 🔥【關鍵修正】呼叫 GameStatusController 加分
            var status = FindObjectOfType<GameStatusController>();
            if (status != null)
            {
                status.OnEnemyCaptured();
            }
            else
            {
                Debug.LogWarning("找不到 GameStatusController，無法加分！");
            }

            // 5. 銷毀物件
            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn();
            else
                Destroy(gameObject);
        }
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

        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, position, soundVolume);
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
            case GameColor.Purple: return Color.magenta;
            default: return Color.white;
        }
    }
}