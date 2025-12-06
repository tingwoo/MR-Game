using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VeilSpawner : NetworkBehaviour
{
    public GameObject veilPrefab;
    public Transform spawnLocation;
    public float veilSpeed = 2.5f;

    public override void OnNetworkSpawn()
    {
        if (IsServer && veilPrefab != null)
        {
            InvokeRepeating(nameof(SpawnVeil), 0f, 5f);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            CancelInvoke(nameof(SpawnVeil));
        }
    }

    public void SpawnVeil()
    {
        if (!IsServer || veilPrefab == null || spawnLocation == null) return;

        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        Quaternion finalRotation = Quaternion.LookRotation(Vector3.back) * randomRotation;

        GameObject veilGO = Instantiate(veilPrefab, spawnLocation.position, finalRotation);

        var veilScript = veilGO.GetComponent<Veil>();
        if (veilScript != null)
        {
            veilScript.speed.Value = veilSpeed;
        }

        var veilNO = veilGO.GetComponent<NetworkObject>();
        if (veilNO != null)
        {
            veilNO.Spawn();
            
            if (GameCleanupManager.Instance != null) 
            {
                GameCleanupManager.Instance.RegisterObject(veilNO);
            }
        }
    }
}