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

    private void OnTriggerEnter(Collider other)
    {
        // 1. Server Logic Only: Collision logic is authoritative on the server
        if (!IsServer) return;

        if (other.CompareTag("FullRing") && other.gameObject.GetComponent<FullRing>().color == color)
        {
            Color visualColor = ConvertGameColorToUnityColor(color);
            
            // 2. Visuals: Tell all clients to spawn explosion VFX
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
        GameObject boom = Instantiate(explosionPrefab, position, Quaternion.identity);
        
        ExplosionController controller = boom.GetComponent<ExplosionController>();
        if (controller != null)
        {
            controller.Initialize(impactColor);
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