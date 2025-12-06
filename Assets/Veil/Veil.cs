using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Veil : NetworkBehaviour
{
    public NetworkVariable<float> speed = new NetworkVariable<float>(
        2.5f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    void Update()
    {
        if (!IsServer) return;
        transform.position += transform.forward * speed.Value * Time.deltaTime;
        if (transform.position.z < -10.0f)
        {
            Remove();
        }
    }

    private void Remove()
    {
        if (!IsServer) return;
        if (NetworkObject != null && NetworkObject.IsSpawned) 
        {
            NetworkObject.Despawn();
        } else
        {
            Destroy(gameObject);
        }
    }
}