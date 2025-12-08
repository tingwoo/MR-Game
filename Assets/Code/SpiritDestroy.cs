using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpiritDestroy : NetworkBehaviour
{
    public GameColor color;

    [Header("VFX References")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float scoreAmount = 20f; 

    // ==========================================
    // 【新增】音效設定欄位
    // ==========================================
    [Header("Audio Settings")]
    [Tooltip("請將音效檔 (AudioClip) 拉入這裡")]
    [SerializeField] private AudioClip destroySound; 

    [Range(0f, 1f)] 
    [SerializeField] private float soundVolume = 1.0f; // 音量大小調整
    // ==========================================

    private void OnTriggerEnter(Collider other)
    {
        // 1. Server Logic Only: Collision logic is authoritative on the server
        if (!IsServer) return;

        if (other.CompareTag("FullRing") && other.gameObject.GetComponent<FullRing>().color == color)
        {
            Color visualColor = ConvertGameColorToUnityColor(color);
            
            // 2. Visuals: Tell all clients to spawn explosion VFX AND Play Sound
            SpawnExplosionClientRpc(transform.position, visualColor);

            // 3. Haptics: Tell the full ring to play haptics on its two component hands
            other.gameObject.GetComponent<FullRing>().PlayHaptics();

            // 4. Logic: Add score directly on the Server
            if (StaminaBarController.Instance != null)
            {
                StaminaBarController.Instance.AddStaminaServer(scoreAmount);
            }
            else
            {
                Debug.LogWarning("StaminaBarController Instance not found!");
            }

            // 4. Cleanup: Despawn the network object
            if (NetworkObject != null && NetworkObject.IsSpawned) 
                NetworkObject.Despawn();
            else
                Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position, Color impactColor)
    {
        // Instantiate the visual effect locally on each client
        if (explosionPrefab != null)
        {
            GameObject boom = Instantiate(explosionPrefab, position, Quaternion.identity);
            ExplosionController controller = boom.GetComponent<ExplosionController>();
            if (controller != null)
            {
                controller.Initialize(impactColor);
            }
        }

        // ==========================================
        // 【新增】在客戶端播放音效
        // ==========================================
        if (destroySound != null)
        {
            // PlayClipAtPoint 會在指定位置建立一個暫時的 AudioSource，
            // 播完後自動銷毀，這樣就算精靈本體被 Destroy 了，聲音也會播完。
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