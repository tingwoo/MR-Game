using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpiritDestroy : NetworkBehaviour
{
    public GameColor color;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("FullRing") && other.gameObject.GetComponent<NetworkFullRing>().Color == color)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned) 
                NetworkObject.Despawn();
            else
                Destroy(gameObject);
        }
    }
}
