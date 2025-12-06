using System.Collections;
using System.Collections.Generic;
using Oculus.Haptics;
using Oculus.Interaction.Input;
using Unity.Netcode;
using UnityEngine;

public class HapticsManager : NetworkBehaviour
{
    public HapticClip hapticClipOne;
    public HapticClip hapticClipThree;
    // private HapticClipPlayer hapticsPlayerOne;
    // private HapticClipPlayer hapticsPlayerThree;

    // Start is called before the first frame update
    // void Start()
    // {
    //     hapticsPlayerOne = new HapticClipPlayer(hapticClipOne);
    //     hapticsPlayerThree = new HapticClipPlayer(hapticClipThree);
    // }

    // Update is called once per frame
    void Update()
    {
        // if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        // {
        //     hapticsPlayerOne.Play(Oculus.Haptics.Controller.Left);
        // }
    }

    // public void PlayHaptics((Handedness handedness, bool isServer) hand, HapticType type)
    // {
    //     HapticClipPlayer hcp = new HapticClipPlayer(type == HapticType.One ? hapticClipOne : hapticClipThree);
    //     if (hand == Handedness.Left)
    //     {
    //         hcp.Play(Oculus.Haptics.Controller.Left);
    //     } else
    //     {
    //         hcp.Play(Oculus.Haptics.Controller.Right);
    //     }
    // }

    public void PlayHapticsOnHand(HandData hand, HapticType type)
    {
        if (!IsServer) return;
        PlayHapticsOnHandClientRpc(hand, type);
    }

    [ClientRpc]
    private void PlayHapticsOnHandClientRpc(HandData hand, HapticType type)
    {
        if (hand.isServer == IsServer)
        {
            HapticClipPlayer hcp = new HapticClipPlayer(type == HapticType.One ? hapticClipOne : hapticClipThree);
            if (hand.handedness == Handedness.Left)
            {
                hcp.Play(Oculus.Haptics.Controller.Left);
            } else
            {
                hcp.Play(Oculus.Haptics.Controller.Right);
            }
        }
    }
}
