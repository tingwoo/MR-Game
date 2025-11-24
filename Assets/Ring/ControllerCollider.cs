using Unity.Netcode;
using UnityEngine;

public class ControllerCollider : NetworkBehaviour 
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Ensure this script belongs to the local player 
        // (You don't want to run this logic for other people's hands)
        // if (!IsOwner) return;

        if (other.CompareTag("RingHandle"))
        {
            Vector3 originalScale = transform.localScale;
            transform.localScale = originalScale * 0.1f;

            // 2. Get the NetworkObject component from the item
            NetworkObject itemNetworkObject = other.transform.parent.GetComponent<NetworkObject>();

            if (itemNetworkObject != null)
            {
                // 3. Request the interaction via a Server RPC
                // We cannot parent it directly on the client. We must ask the server.
                RequestGrabServerRpc(itemNetworkObject.NetworkObjectId);

                itemNetworkObject.GetComponent<NetworkFollower>().targetToFollow = transform;

                // Disable collider so it won't interfere while attached
                GetComponent<Collider>().enabled = false;

                
            }
        }
    }

    [ServerRpc]
    private void RequestGrabServerRpc(ulong itemNetworkObjectId)
    {
        // 4. Find the object on the server side
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkObjectId, out NetworkObject item))
        {
            Vector3 originalScale = transform.localScale;
            transform.localScale = originalScale * 20f;

            // 5. Transfer Ownership to the player holding the controller (Optional but recommended for lower latency)
            item.ChangeOwnership(OwnerClientId);

            // 6. Perform the Network Parenting
            // This function automatically synchronizes the hierarchy change to all clients
            item.TrySetParent(transform);
            
            // 7. Handle Physics (Disable gravity/physics so it follows the hand smoothly)
            // Note: You might need a ClientRpc to handle this if the Rigidbody isn't NetworkRigidbody
            // Rigidbody rb = item.GetComponent<Rigidbody>();
            // if (rb != null)
            // {
            //     rb.isKinematic = true;
            // }
        }
    }
}