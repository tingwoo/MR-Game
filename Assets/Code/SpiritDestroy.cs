using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpiritDestroy : NetworkBehaviour
{
    [Header("Base Settings")]
    public GameColor color;

    [Header("VFX References")]
    [SerializeField] protected GameObject explosionPrefab;
    [SerializeField] protected float scoreAmount = 20f; 

    [Header("Audio Settings")]
    [SerializeField] protected AudioClip destroySound; 

    [Range(0f, 1f)] 
    [SerializeField] protected float soundVolume = 1.0f;

    // Change to protected virtual so children can completely override if needed, 
    // though usually they will just override OnContactLogic.
    protected virtual void OnTriggerEnter(Collider other)
    {
        // 1. Server Logic Only
        if (!IsServer) return;

        // Shared Collision Validation
        if (other.CompareTag("FullRing") && other.gameObject.GetComponent<FullRing>().color == color)
        {
            HandleCapture(other.gameObject);
        }
    }

    protected void HandleCapture(GameObject ringObject)
    {
        Color visualColor = ConvertGameColorToUnityColor(color);
        
        // 2. Visuals: VFX AND Sound (via RPC)
        SpawnExplosionClientRpc(transform.position, visualColor);

        // 3. Haptics: Trigger haptics on the ring handles
        var ringScript = ringObject.GetComponent<FullRing>();
        if (ringScript != null)
        {
            ringScript.PlayHaptics();
        }

        // 4. Logic: Execute specific gameplay consequences (Score vs Tutorial)
        OnContactLogic();

        // 5. Cleanup: Despawn
        if (NetworkObject != null && NetworkObject.IsSpawned) 
            NetworkObject.Despawn();
        else
            Destroy(gameObject);
    }

    // Virtual method to be overridden by TutorialTarget
    protected virtual void OnContactLogic()
    {
        // Default behavior: Add Score / Stamina
        if (StaminaBarController.Instance != null)
        {
            StaminaBarController.Instance.AddStaminaServer(scoreAmount);
        }
        else
        {
            Debug.LogWarning("StaminaBarController Instance not found!");
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position, Color impactColor)
    {
        if (explosionPrefab != null)
        {
            GameObject boom = Instantiate(explosionPrefab, position, Quaternion.identity);
            ExplosionController controller = boom.GetComponent<ExplosionController>();
            if (controller != null)
            {
                controller.Initialize(impactColor);
            }
        }

        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, position, soundVolume);
        }
    }

    // Helper to convert colors
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