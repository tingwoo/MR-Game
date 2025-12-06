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

    private HapticClipPlayer leftHandPlayer;
    private HapticClipPlayer rightHandPlayer;

    // Update is called once per frame
    void Update()
    {
        // if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        // {
        //     hapticsPlayerOne.Play(Oculus.Haptics.Controller.Left);
        // }
    }

    public void PlayHapticsOnHand(HandData hand, HapticType type)
    {
        if (!IsServer) return;
        PlayHapticsOnHandClientRpc(hand, type);
    }

    [ClientRpc]
    private void PlayHapticsOnHandClientRpc(HandData hand, HapticType type)
    {
        HapticClip clipToPlay = type == HapticType.One ? hapticClipOne : hapticClipThree;

        if (hand.isServer == IsServer)
        {
            if (hand.handedness == Handedness.Left)
            {
                leftHandPlayer?.Stop();
                leftHandPlayer = new HapticClipPlayer(clipToPlay);
                leftHandPlayer.Play(Oculus.Haptics.Controller.Left);
            } else
            {
                rightHandPlayer?.Stop();
                rightHandPlayer = new HapticClipPlayer(clipToPlay);
                rightHandPlayer.Play(Oculus.Haptics.Controller.Right);
            }
        }
    }
}
