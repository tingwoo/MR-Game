 using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Palmmedia.ReportGenerator.Core;

public class RingManager : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkObject halfRingPrefab;
    [SerializeField] private NetworkObject fullRingPrefab;

    [Header("Settings")]
    [SerializeField] private float distanceThreshold = 0.1f; // Default value added

    // Caches to avoid GC allocation in Update
    private List<GameObject> _handsToRemoveCache = new List<GameObject>();
    private List<NetworkHalfRing> _pairsToRemoveCache = new List<NetworkHalfRing>();
    private List<KeyValuePair<GameObject, NetworkHalfRing>> _ringEntriesCache = new List<KeyValuePair<GameObject, NetworkHalfRing>>();

    // State tracking
    private Dictionary<GameObject, NetworkHalfRing> handRingDict = new Dictionary<GameObject, NetworkHalfRing>();
    
    // Mapping: HalfRing -> (PartnerHalfRing, TheSpawnedFullRing)
    private Dictionary<NetworkHalfRing, (NetworkHalfRing pairedRing, NetworkFullRing spawnedRing)> pairedRings = new Dictionary<NetworkHalfRing, (NetworkHalfRing, NetworkFullRing)>();

    private bool settingPosition = true;
    [SerializeField] private Vector3 ringOffset;
    [SerializeField] private Vector3 ringRotation;

    // [SerializeField] private GameObject testObject;
    

    private void Update()
    {
        if (!IsServer) return;

        HandleSpawningAndMovement();
        HandleNewPairs();
        HandleExistingPairs();

        Vector2 hChange = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        float vChange = 0f;

        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            settingPosition = !settingPosition;
        }

        if (OVRInput.Get(OVRInput.Button.Two))
        {
            vChange += 1f;
        } else if (OVRInput.Get(OVRInput.Button.One))
        {
            vChange -= 1f;
        }

        if (settingPosition)
            ringOffset += new Vector3(hChange.x, vChange, hChange.y) * Time.deltaTime;
        else
            ringRotation += new Vector3(hChange.y, vChange, hChange.x) * Time.deltaTime;

    }

    /// <summary>
    /// 1. Iterates clients, ensures rings exist for hands, and updates positions.
    /// </summary>
    private void HandleSpawningAndMovement()
    {
        // --- A. Spawn & Update Positions ---
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            // Ensure the player has the required rig component
            if (!client.PlayerObject.TryGetComponent<VRNetworkRig>(out var playerRig)) continue;

            // Process Left Hand
            if (playerRig.rootLeftHand != null)
            {
                ProcessHand(playerRig.rootLeftHand.gameObject, true);
            }

            // Process Right Hand
            if (playerRig.rootRightHand != null)
            {
                ProcessHand(playerRig.rootRightHand.gameObject, false);
            }
        }

        // --- B. Cleanup Invalid Entries ---
        _handsToRemoveCache.Clear();

        foreach (var kvp in handRingDict)
        {
            var hand = kvp.Key;
            var ring = kvp.Value;

            // Check if Hand or Ring has been destroyed/disconnected
            if (hand == null || ring == null || !ring.IsSpawned)
            {
                // Despawn if the ring still exists as a NetObject but the hand is gone
                if (ring != null && ring.NetworkObject != null && ring.NetworkObject.IsSpawned)
                {
                    ring.NetworkObject.Despawn();
                }
                
                _handsToRemoveCache.Add(hand); // Mark dictionary key for removal
            }
        }

        // Apply cleanup
        foreach (var hand in _handsToRemoveCache)
        {
            if (hand != null) handRingDict.Remove(hand);
            // Note: If hand is null (destroyed), the dictionary key might be tricky. 
            // Ideally, loop backward or use a separate list of keys, 
            // but Unity overrides == null, so the object reference remains valid as a key even if destroyed.
            // For safety, we remove strictly based on the loop we just ran.
             handRingDict.Remove(hand);
        }
    }

    /// <summary>
    /// Helper to handle creation and movement of a single ring.
    /// </summary>
    private void ProcessHand(GameObject handGO, bool isLeft)
    {
        // 1. Spawn if missing
        if (!handRingDict.TryGetValue(handGO, out var ring) || ring == null)
        {
            var netObj = Instantiate(halfRingPrefab);
            netObj.Spawn();
            
            ring = netObj.GetComponent<NetworkHalfRing>();
            
            // Assign color based on count
            ring.SetColor((GameColor)(handRingDict.Count % 4));
            ring.SetHandedness(isLeft);
            
            handRingDict[handGO] = ring;
        }

        // 2. Update Position/Rotation
        // Note: If Ring has a NetworkTransform, ensure it's in ServerAuth mode.

        // testObject.transform.position = handGO.transform.position;
        // testObject.transform.rotation = handGO.transform.rotation;

        // testObject.transform.localPosition += ringOffset;
        
        // Apply offset based on handedness
        if (isLeft)
        {
            ring.transform.position = handGO.transform.position;
            ring.transform.rotation = handGO.transform.rotation;

            ring.transform.localPosition += new Vector3(-ringOffset.x, ringOffset.y, ringOffset.z);
            // ring.transform.rotation = handGO.transform.rotation * Quaternion.Euler(-45f, 180f, 0f);
            ring.transform.rotation *= Quaternion.Euler(new Vector3(-ringRotation.x, 180f - ringRotation.y, -ringRotation.z));
        }
        else
        {
            ring.transform.position = handGO.transform.position;
            ring.transform.rotation = handGO.transform.rotation;

            ring.transform.localPosition += ringOffset;
            // ring.transform.rotation = handGO.transform.rotation * Quaternion.Euler(45f, 0f, 0f);
            ring.transform.rotation *= Quaternion.Euler(ringRotation);
        }
    }

    /// <summary>
    /// 2. Checks for collisions between unpaired rings.
    /// </summary>
    private void HandleNewPairs()
    {
        // Update the cache list from the dictionary
        _ringEntriesCache.Clear();
        foreach(var kvp in handRingDict) _ringEntriesCache.Add(kvp);

        int count = _ringEntriesCache.Count;

        // O(N^2) loop - Acceptable for low player counts (e.g., < 10 players)
        for (int i = 0; i < count; i++)
        {
            var ring1 = _ringEntriesCache[i].Value;

            // Skip if invalid or already paired
            if (!IsRingValid(ring1) || pairedRings.ContainsKey(ring1)) continue;

            for (int j = i + 1; j < count; j++)
            {
                var ring2 = _ringEntriesCache[j].Value;

                // Skip if invalid or already paired
                if (!IsRingValid(ring2) || pairedRings.ContainsKey(ring2)) continue;

                // Check distance
                if (CheckClose(ring1, ring2))
                {
                    CreatePair(ring1, ring2);
                }
            }
        }
    }

    private void CreatePair(NetworkHalfRing ring1, NetworkHalfRing ring2)
    {
        var fullRingNO = Instantiate(fullRingPrefab);
        fullRingNO.Spawn();
        
        var fullRing = fullRingNO.GetComponent<NetworkFullRing>();
        
        // Combine colors (Assuming you have an extension method for this)
        fullRing.SetColor(GameColorExtensions.Add(ring1.Color, ring2.Color));

        // Store relationship (Store on both? Currently logic implies storing on ring1 is enough to track the unique pair)
        // To prevent double booking, we usually store the relationship one way or check both.
        // Current logic: key is ring1.
        pairedRings[ring1] = (ring2, fullRing);
        // Also add ring2 as a key so it doesn't get picked up by the main loop again
        pairedRings[ring2] = (ring1, fullRing); 

        ring1.SetShow(false);
        ring2.SetShow(false);
    }

    /// <summary>
    /// 3. Updates existing pairs and breaks them if distance is exceeded or objects destroyed.
    /// </summary>
    private void HandleExistingPairs()
    {
        _pairsToRemoveCache.Clear();

        // We only need to iterate unique pairs. 
        // Since we added BOTH rings to the dictionary in CreatePair, we need to avoid processing the same pair twice.
        // We can use a HashSet to track processed fullRings, or simply iterate and check logic.
        
        var processedFullRings = new HashSet<NetworkFullRing>();

        foreach (var kvp in pairedRings)
        {
            var primaryRing = kvp.Key;
            var partnerRing = kvp.Value.pairedRing;
            var fullRing = kvp.Value.spawnedRing;

            // If we already processed this full ring (via the partner), skip
            if (processedFullRings.Contains(fullRing)) continue;
            processedFullRings.Add(fullRing);

            bool shouldUnpair = false;

            // Validation
            if (!IsRingValid(primaryRing) || !IsRingValid(partnerRing) || fullRing == null)
            {
                shouldUnpair = true;
            }
            else if (!CheckClose(primaryRing, partnerRing))
            {
                shouldUnpair = true;
            }

            if (shouldUnpair)
            {
                if (fullRing != null && fullRing.NetworkObject.IsSpawned)
                    fullRing.NetworkObject.Despawn();

                if (IsRingValid(primaryRing)) primaryRing.SetShow(true);
                if (IsRingValid(partnerRing)) partnerRing.SetShow(true);

                // Mark both for removal from dictionary
                _pairsToRemoveCache.Add(primaryRing);
                _pairsToRemoveCache.Add(partnerRing);
            }
            else
            {
                UpdateFullRingTransform(primaryRing, partnerRing, fullRing);
            }
        }

        foreach (var ring in _pairsToRemoveCache)
        {
            pairedRings.Remove(ring);
        }
    }

    private void UpdateFullRingTransform(NetworkHalfRing r1, NetworkHalfRing r2, NetworkFullRing full)
    {
        // Average position
        full.transform.position = (r1.transform.position + r2.transform.position) * 0.5f;

        // Average Rotation logic
        Vector3 avgForward = (r1.transform.forward + r2.transform.forward) * 0.5f;
        Vector3 avgUp = (r1.transform.up + r2.transform.up) * 0.5f;

        // Fix cancellation if facing opposite directions
        if (Vector3.Dot(r1.transform.forward, r2.transform.forward) < 0)
            avgForward = (r1.transform.forward - r2.transform.forward) * 0.5f;

        if (Vector3.Dot(r1.transform.up, r2.transform.up) < 0)
            avgUp = (r1.transform.up - r2.transform.up) * 0.5f;
        
        if (avgForward != Vector3.zero && avgUp != Vector3.zero)
        {
            full.transform.rotation = Quaternion.LookRotation(avgForward, avgUp);
        }
    }

    private bool IsRingValid(NetworkHalfRing r)
    {
        return r != null && r.NetworkObject != null && r.NetworkObject.IsSpawned && r.collidePoint1 != null && r.collidePoint2 != null;
    }

    private bool CheckClose(NetworkHalfRing r1, NetworkHalfRing r2)
    {
        float sqrThresh = distanceThreshold * distanceThreshold;

        // Cache positions to avoid Repeated Native calls
        Vector3 p1_1 = r1.collidePoint1.position;
        Vector3 p1_2 = r1.collidePoint2.position;
        Vector3 p2_1 = r2.collidePoint1.position;
        Vector3 p2_2 = r2.collidePoint2.position;

        float d11 = (p1_1 - p2_1).sqrMagnitude;
        float d22 = (p1_2 - p2_2).sqrMagnitude;
        
        // Cross distances
        float d12 = (p1_1 - p2_2).sqrMagnitude;
        float d21 = (p1_2 - p2_1).sqrMagnitude;

        return (d11 < sqrThresh && d22 < sqrThresh) || (d12 < sqrThresh && d21 < sqrThresh);
    }
}