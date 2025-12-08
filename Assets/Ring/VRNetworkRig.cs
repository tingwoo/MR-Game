using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VRNetworkRig : NetworkBehaviour
{
    [Header("Network Visuals")]
    [Tooltip("Drag the child objects of this prefab here")]
    // public Transform rootHead;
    public Transform rootLeftHand;
    public Transform rootRightHand;

    [Header("Local Hardware Reference")]
    [Tooltip("We will auto-find these from the OVRCameraRig in the scene")]
    // private Transform _localHeadAnchor;
    private Transform _localLeftHandAnchor;
    private Transform _localRightHandAnchor;

    public static List<VRNetworkRig> ActiveRigs = new List<VRNetworkRig>();

    public override void OnNetworkDespawn() 
    {
        ActiveRigs.Remove(this);
    }

    public override void OnNetworkSpawn()
    {
        // If we are not the owner of this object, we don't need to find hardware.
        // We just listen to the network stream.
        // if (!IsOwner) return;

        ActiveRigs.Add(this);

        // 1. Find the OVRCameraRig in the scene (The physical hardware)
        var cameraRig = FindFirstObjectByType<OVRCameraRig>();
        
        if (cameraRig == null)
        {
            Debug.LogError("OVRCameraRig not found in scene! Ensure it is present.");
            return;
        }

        // 2. Cache the local hardware anchors
        // _localHeadAnchor = cameraRig.centerEyeAnchor;
        _localLeftHandAnchor = cameraRig.leftHandAnchor;
        _localRightHandAnchor = cameraRig.rightHandAnchor;
    }

    void Update()
    {
        // If we don't own this object, we exit. 
        // The NetworkTransform component handles the movement for us automatically based on data from the owner.
        if (!IsOwner) return;

        // 3. Copy Local Hardware Data -> To Network Object
        // The NetworkTransform component will detect these changes and broadcast them.
        
        // if (rootHead && _localHeadAnchor)
        // {
        //     rootHead.position = _localHeadAnchor.position;
        //     rootHead.rotation = _localHeadAnchor.rotation;
        // }

        if (rootLeftHand && _localLeftHandAnchor)
        {
            rootLeftHand.position = _localLeftHandAnchor.position;
            rootLeftHand.rotation = _localLeftHandAnchor.rotation;
        }

        if (rootRightHand && _localRightHandAnchor)
        {
            rootRightHand.position = _localRightHandAnchor.position;
            rootRightHand.rotation = _localRightHandAnchor.rotation;
        }
    }
}