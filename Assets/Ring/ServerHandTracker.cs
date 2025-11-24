using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ServerHandTracker : NetworkBehaviour
{
    public GameObject middleObject;

    // A helpful struct to keep data organized if you want to use it later
    public struct PlayerHandData
    {
        public ulong ClientId;
        public Vector3 LeftHandPos;
        public Vector3 RightHandPos;
    }

    void Update()
    {
        // 1. Only run this logic on the Server (or Host)
        if (!IsServer) return;

        List<Vector3> rightHandPositions = new List<Vector3>();

        // 2. Iterate through all currently connected clients
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // 3. Get the NetworkObject assigned to this client
            if (client.PlayerObject != null)
            {
                // 4. Get the VRNetworkRig component we wrote in Step 1
                var playerRig = client.PlayerObject.GetComponent<VRNetworkRig>();

                if (playerRig != null)
                {
                    // 5. Access the transforms
                    // Note: These values are automatically updated by the 
                    // NetworkTransform component on the client's prefab.
                    
                    Vector3 leftPos = Vector3.zero;
                    Vector3 rightPos = Vector3.zero;

                    // Safety check in case hands haven't initialized or were destroyed
                    if (playerRig.rootLeftHand != null) 
                        leftPos = playerRig.rootLeftHand.position;
                        
                    if (playerRig.rootRightHand != null) 
                        rightPos = playerRig.rootRightHand.position;

                    rightHandPositions.Add(rightPos);

                    // Example: Log the data (or pass it to your game logic)
                    // Debug.Log($"Client {client.ClientId} | Left: {leftPos} | Right: {rightPos}");
                    
                    // Logic Example: Check if ANY hand is inside a specific trigger zone
                    // CheckHitbox(leftPos, rightPos);

                    
                }
            }
        }

        if (rightHandPositions.Count == 2)
        {
            middleObject.transform.position = (rightHandPositions[0] + rightHandPositions[1]) * 0.5f;
        }
    }
}