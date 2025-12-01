using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TrackHands : NetworkBehaviour
{
    public GameObject halfRingPrefab;
    public GameObject fullRingPrefab;
    private HalfRing[] halfRingList = new HalfRing[4];

    [SerializeField] private Vector3 ringOffset;
    [SerializeField] private float ringLongitude;
    [SerializeField] private float ringLatitude;

    [SerializeField] private float distanceThreshold = 0.1f;

    private Dictionary<HalfRing, (HalfRing pairedRing, FullRing spawnedRing)> pairedRings = new Dictionary<HalfRing, (HalfRing, FullRing)>();

    private List<HalfRing> _pairsToRemoveCache = new List<HalfRing>();

    void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject p = Instantiate(halfRingPrefab);
            p.GetComponent<HalfRing>().color = (GameColor)i;
            p.GetComponent<HalfRing>().UpdateModels();

            p.transform.position = new Vector3(0.25f * i, 1f, 0f);
            halfRingList[i] = p.GetComponent<HalfRing>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Snap rings to hands
        // 0, 1: Server Rings
        // 2, 3: Client Rings
        foreach (VRNetworkRig rig in VRNetworkRig.ActiveRigs)
        {
            bool isMine = rig.OwnerClientId == NetworkManager.Singleton.LocalClientId;
            int baseIndex = isMine ^ IsServer ? 0 : 2;

            UpdateTransform(halfRingList[baseIndex], rig.rootLeftHand, true);
            UpdateTransform(halfRingList[baseIndex + 1], rig.rootRightHand, false);
        }

        // Check connected pairs
        // O(N^2) loop - Acceptable for low player counts (e.g., < 10 players)
        for (int i = 0; i < 4; i++)
        {
            if (pairedRings.ContainsKey(halfRingList[i])) continue;

            for (int j = i + 1; j < 4; j++)
            {
                if (pairedRings.ContainsKey(halfRingList[j])) continue;

                // Check distance
                if (CheckClose(halfRingList[i], halfRingList[j]))
                {
                    CreatePair(halfRingList[i], halfRingList[j]);
                }
            }
        }
        
        _pairsToRemoveCache.Clear();

        var processedFullRings = new HashSet<FullRing>();

        foreach (var kvp in pairedRings)
        {
            var primaryRing = kvp.Key;
            var partnerRing = kvp.Value.pairedRing;
            var fullRing = kvp.Value.spawnedRing;

            // Validate rings first
            if (primaryRing == null || partnerRing == null)
            {
                // Mark both (if non-null) for removal
                if (primaryRing != null) _pairsToRemoveCache.Add(primaryRing);
                if (partnerRing != null) _pairsToRemoveCache.Add(partnerRing);
                continue;
            }

            // If we already processed this full ring (via the partner), skip
            if (fullRing != null && processedFullRings.Contains(fullRing)) continue;
            if (fullRing != null) processedFullRings.Add(fullRing);

            bool shouldUnpair = false;

            // Validation
            if (fullRing == null)
            {
                shouldUnpair = true;
            }
            else if (!CheckClose(primaryRing, partnerRing))
            {
                shouldUnpair = true;
            }

            if (shouldUnpair)
            {
                if (fullRing != null)
                    Destroy(fullRing.gameObject);

                primaryRing.show = true;
                partnerRing.show = true;

                primaryRing.UpdateModels();
                partnerRing.UpdateModels();

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

    private void UpdateFullRingTransform(HalfRing r1, HalfRing r2, FullRing full)
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

    void UpdateTransform(HalfRing ring, Transform handTF, bool isLeft)
    {
        // 2. Update Position/Rotation
        // Note: If Ring has a NetworkTransform, ensure it's in ServerAuth mode.
        Vector3 handedOffset;
        float handedLongitude, handedLatitude;

        if (isLeft)
        {
            handedOffset = new Vector3(-ringOffset.x, ringOffset.y, ringOffset.z);
            handedLongitude = 180f - ringLongitude;
            handedLatitude = -ringLatitude;
        } else
        {
            handedOffset = ringOffset;
            handedLongitude = ringLongitude;
            handedLatitude = ringLatitude;
        }

        // 1. Calculate the Rotation
        // Combine the hand's rotation with the ring's custom rotation
        ring.transform.rotation = handTF.rotation;

        // Longitude: Rotate around green axis (y)
        ring.transform.Rotate(Vector3.up, handedLongitude, Space.Self);

        // Latitude: Rotate around red axis (x)
        ring.transform.Rotate(Vector3.right, handedLatitude, Space.Self);

        // 2. Calculate the Position
        // Rotate the offset vector to match the hand's orientation
        // In Unity, "Quaternion * Vector3" applies the rotation to the vector
        Vector3 rotatedOffset = handTF.rotation * handedOffset;

        // Add the rotated offset to the hand's origin
        ring.transform.position = handTF.position + rotatedOffset;
    }

    private bool CheckClose(HalfRing r1, HalfRing r2)
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

    private void CreatePair(HalfRing ring1, HalfRing ring2)
    {
        GameObject fullRingGO = Instantiate(fullRingPrefab);
        
        FullRing fullRing = fullRingGO.GetComponent<FullRing>();
        
        // Combine colors (Assuming you have an extension method for this)
        fullRing.color = GameColorExtensions.Add(ring1.color, ring2.color);
        fullRing.UpdateModels();

        // Store relationship (Store on both? Currently logic implies storing on ring1 is enough to track the unique pair)
        // To prevent double booking, we usually store the relationship one way or check both.
        // Current logic: key is ring1.
        pairedRings[ring1] = (ring2, fullRing);
        // Also add ring2 as a key so it doesn't get picked up by the main loop again
        pairedRings[ring2] = (ring1, fullRing); 

        ring1.show = false;
        ring2.show = false;

        ring1.UpdateModels();
        ring2.UpdateModels();
    }
}
