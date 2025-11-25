using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
// using Unity.Mathematics;
// using Unity.VisualScripting;

public class RingManager : NetworkBehaviour
{
    public NetworkObject fullRingPrefab;
    public NetworkObject halfRingPrefab;
    public float distanceThreshold;

    // A helpful struct to keep data organized if you want to use it later
    public struct PlayerHandData
    {
        public ulong ClientId;
        public Vector3 LeftHandPos;
        public Vector3 RightHandPos;
    }

    private Dictionary<GameObject, NetworkHalfRing> handRingDict = new Dictionary<GameObject, NetworkHalfRing>();
    private Dictionary<NetworkHalfRing, (NetworkHalfRing pairedRing, NetworkFullRing spawnedRing)> pairedRings = new Dictionary<NetworkHalfRing, (NetworkHalfRing, NetworkFullRing)>();

    void Update()
    {
        // 1. Only run this logic on the Server (or Host)
        if (!IsServer) return;

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

                    // Safety check in case hands haven't initialized or were destroyed
                    if (playerRig.rootLeftHand != null) {

                        var leftHandGO = playerRig.rootLeftHand.gameObject;
                        if (!handRingDict.TryGetValue(leftHandGO, out var ring) || ring == null)
                        {
                            var netObj = Instantiate(halfRingPrefab); // returns NetworkObject
                            netObj.Spawn(); // ensure it's spawned on the server/host
                            ring = netObj.GetComponent<NetworkHalfRing>();
                            ring.SetColor((GameColor)(handRingDict.Count % 4));
                            ring.SetHandedness(true);
                            handRingDict[leftHandGO] = ring;
                        }
                    }
                        
                    if (playerRig.rootRightHand != null) {

                        var rightHandGO = playerRig.rootRightHand.gameObject;
                        if (!handRingDict.TryGetValue(rightHandGO, out var ring) || ring == null)
                        {
                            var netObj = Instantiate(halfRingPrefab); // returns NetworkObject
                            netObj.Spawn(); // ensure it's spawned on the server/host
                            ring = netObj.GetComponent<NetworkHalfRing>();
                            ring.SetColor((GameColor)(handRingDict.Count % 4));
                            ring.SetHandedness(false);
                            handRingDict[rightHandGO] = ring;
                        }
                    }
                }
            }
        }

        // Update ring transforms to follow their corresponding hand GameObjects.
        // Clean up entries whose hand or ring was destroyed.
        var toRemove = new List<GameObject>();
        foreach (var kvp in handRingDict)
        {
            var hand = kvp.Key;
            var ring = kvp.Value;

            if (hand == null || ring == null)
            {
                if (ring != null && ring.NetworkObject != null && ring.NetworkObject.IsSpawned)
                {
                    ring.NetworkObject.Despawn();
                }
                if (ring != null && ring.gameObject != null)
                {
                    Destroy(ring.gameObject);
                }
                toRemove.Add(hand);
                continue;
            }

            // Snap ring to hand transform on the server
            ring.transform.position = hand.transform.position;
            if (ring.OnLeftHand)
            {
                ring.transform.rotation = hand.transform.rotation * Quaternion.Euler(-45f, 180f, 0f);
            }
            else
            {
                ring.transform.rotation = hand.transform.rotation * Quaternion.Euler(45f, 0f, 0f);
            }
        }

        // Remove cleaned-up entries
        foreach (var key in toRemove)
        {
            handRingDict.Remove(key);
        }

        // Find close pairs of rings
        var entries = new List<KeyValuePair<GameObject, NetworkHalfRing>>(handRingDict);

        for (int i = 0; i < entries.Count; i++)
        {
            var hand1 = entries[i].Key;
            var ring1 = entries[i].Value;
            if (hand1 == null || !RingExists(ring1)) continue;
            if (pairedRings.ContainsKey(ring1)) continue;

            for (int j = i + 1; j < entries.Count; j++)
            {
                var hand2 = entries[j].Key;
                var ring2 = entries[j].Value;
                if (hand2 == null || !RingExists(ring2)) continue;
                if (pairedRings.ContainsKey(ring2)) continue;

                if (CheckClose(ring1, ring2))
                {
                    var fullRingNO = Instantiate(fullRingPrefab);
                    fullRingNO.Spawn();
                    var fullRing = fullRingNO.GetComponent<NetworkFullRing>();
                    fullRing.SetColor(GameColorExtensions.Add(ring1.Color, ring2.Color));
                    pairedRings[ring1] = (ring2, fullRing);

                    ring1.SetShow(false);
                    ring2.SetShow(false);
                }
            }
        }

        // Check whether old pairs are still paired, if not, remove them from dictionary
        // Validate existing pairs and remove ones that no longer meet criteria

        var pairsToRemove = new List<NetworkHalfRing>();

        foreach (var kvp in pairedRings)
        {
            var ring1 = kvp.Key;
            var ring2 = kvp.Value.pairedRing;
            var fullRing = kvp.Value.spawnedRing;
            bool shouldUnpair = false;

            if (!RingExists(ring1) || !RingExists(ring2))
            {
                shouldUnpair = true;
            }
            else if (!CheckClose(ring1, ring2))
            {
                shouldUnpair = true;
            }

            if (shouldUnpair)
            {
                fullRing.NetworkObject.Despawn();
                // Destroy(fullRing.NetworkObject);

                ring1.SetShow(true);
                ring2.SetShow(true);

                pairsToRemove.Add(ring1);
            }
            else
            {
                fullRing.transform.position = (ring1.transform.position + ring2.transform.position) * 0.5f;

                Vector3 avgForward = (ring1.transform.forward + ring2.transform.forward) * 0.5f;
                Vector3 avgUp = (ring1.transform.up + ring2.transform.up) * 0.5f;
                
                if (Vector3.Dot(ring1.transform.forward, ring2.transform.forward) < 0)
                    avgForward = (ring1.transform.forward - ring2.transform.forward) * 0.5f;
                
                if (Vector3.Dot(ring1.transform.up, ring2.transform.up) < 0)
                    avgUp = (ring1.transform.up - ring2.transform.up) * 0.5f;
                
                fullRing.transform.rotation = Quaternion.LookRotation(avgForward, avgUp);
            }
        }

        foreach (var ring in pairsToRemove)
        {
            pairedRings.Remove(ring);
        }
    }

    private bool RingExists(NetworkHalfRing r)
    {
        if (r == null) return false;
        if (r.collidePoint1 == null || r.collidePoint2 == null) return false;
        return true;
    }

    private bool CheckClose(NetworkHalfRing r1, NetworkHalfRing r2)
    {
        float sqrThresh = distanceThreshold * distanceThreshold;

        var p1_1 = r1.collidePoint1.position;
        var p1_2 = r1.collidePoint2.position;
        var p2_1 = r2.collidePoint1.position;
        var p2_2 = r2.collidePoint2.position;

        float d11 = (p1_1 - p2_1).sqrMagnitude;
        float d22 = (p1_2 - p2_2).sqrMagnitude;
        float d12 = (p1_1 - p2_2).sqrMagnitude;
        float d21 = (p1_2 - p2_1).sqrMagnitude;

        if ((d11 < sqrThresh && d22 < sqrThresh) || (d12 < sqrThresh && d21 < sqrThresh))
        {
            return true;
        }
        return false;
    }
}