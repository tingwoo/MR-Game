using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpiritDestroy : NetworkBehaviour
{
    public GameColor color;
    
    [Header("VFX References")]
    [SerializeField] private GameObject explosionPrefab;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Server Logic Only
        if (!IsServer) return;

        if (other.CompareTag("FullRing") && other.gameObject.GetComponent<FullRing>().color == color)
        {
            // 2. Tell all clients to play the VFX BEFORE destroying the object
            // We convert the Enum 'color' to a real Unity Color struct here
            Color visualColor = ConvertGameColorToUnityColor(color);
            SpawnExplosionClientRpc(transform.position, visualColor);

            // 3. Handle Destruction
            if (NetworkObject != null && NetworkObject.IsSpawned) 
                NetworkObject.Despawn();
            else
                Destroy(gameObject);
        }
    }

    // The [ClientRpc] attribute makes this function run on all connected clients
    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position, Color impactColor)
    {
        // A. Instantiate locally (no network identity needed on the explosion prefab)
        GameObject boom = Instantiate(explosionPrefab, position, Quaternion.identity);

        // B. Apply the color using the script we made in the previous step
        ExplosionController controller = boom.GetComponent<ExplosionController>();
        if (controller != null)
        {
            controller.Initialize(impactColor);
        }
    }

    // Helper to map your GameColor enum to actual visible colors
    private Color ConvertGameColorToUnityColor(GameColor gameColor)
    {
        // Assuming GameColor is an enum. Adjust these cases to match your enum names!
        switch (gameColor)
        {
            // Example Cases:
            case GameColor.Red: return Color.red;
            case GameColor.Yellow: return Color.yellow;
            case GameColor.Blue: return Color.blue;
            case GameColor.Orange: return new Color(1.0f, 0.5f, 0.0f);
            case GameColor.Green: return Color.green;
            case GameColor.Purple: return Color.magenta;
            // Fallback
            default: return Color.white;
        }
    }
}