using UnityEngine;
using Unity.Netcode;

public class SpiritDestroy : NetworkBehaviour
{
    [Header("Base Settings")]
    public GameColor color;

    [Header("VFX References")]
    [SerializeField] protected GameObject explosionPrefab;

    // 這個變數其實用不到了，因為分數改由 GameStatusController 決定
    // 但為了不破壞 Inspector 設定，您可以留著，或者加個 [Obsolete]
    [SerializeField] protected float scoreAmount = 20f;

    [Header("Audio Settings")]
    [SerializeField] protected AudioClip destroySound;
    [Range(0f, 1f)]
    [SerializeField] protected float soundVolume = 1.0f;

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("FullRing") && other.gameObject.GetComponent<FullRing>().color == color)
        {
            HandleCapture(other.gameObject);
        }
    }

    protected void HandleCapture(GameObject ringObject)
    {
        Color visualColor = ConvertGameColorToUnityColor(color);

        // Visuals
        SpawnExplosionClientRpc(transform.position, visualColor);

        // Haptics
        var ringScript = ringObject.GetComponent<FullRing>();
        if (ringScript != null)
        {
            ringScript.PlayHaptics();
        }

        // Logic (Score)
        OnContactLogic();

        // Cleanup
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
        else
            Destroy(gameObject);
    }

    // 🔥【關鍵修正】這裡要改呼叫 GameStatusController
    protected virtual void OnContactLogic()
    {
        // 嘗試尋找新的管理器
        var status = FindObjectOfType<GameStatusController>();

        if (status != null)
        {
            // ✅ 正確：呼叫新的加分函式
            // 這會使用 GameStatusController 裡設定的 1 或 100 分
            status.OnEnemyCaptured();
        }
        else
        {
            Debug.LogWarning("找不到 GameStatusController，無法加分！");
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

    protected Color ConvertGameColorToUnityColor(GameColor gameColor)
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