using Unity.Netcode;
using UnityEngine;

public class NetworkFollower : NetworkBehaviour
{
    // Assign the regular (non-networked) object here via Inspector or code
    public Transform targetToFollow; 
    
    // Offset if you don't want it exactly at the center
    public Vector3 positionOffset;

    void Update()
    {
        // Only the owner (or server) should control the position
        // if (IsOwner && targetToFollow != null)
        if (targetToFollow != null)
        {
            transform.position = targetToFollow.position + positionOffset;
            transform.rotation = targetToFollow.rotation;
        }
    }
}