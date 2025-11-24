using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkRingSpawner : NetworkBehaviour
{
    [Header("Ring Prefabs")]
    public NetworkObject halfRingPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {   
            for (int i = 0; i < 4; i++)
            {
                var r = Instantiate(halfRingPrefab, new Vector3(0.0f, 1.0f, 0.3f * (i - 1.5f)), Quaternion.identity);
                r.GetComponent<NetworkHalfRing>().SetColor((GameColor)i);
                r.Spawn();
            }
        }
    }
}
