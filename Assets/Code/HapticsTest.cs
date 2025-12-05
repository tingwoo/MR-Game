using System.Collections;
using System.Collections.Generic;
using Oculus.Haptics;
using Oculus.Interaction.Input;
using UnityEngine;

public class HapticsTest : MonoBehaviour
{
    public HapticClip hapticClipOne;
    public HapticClip hapticClipThree;
    private HapticClipPlayer hapticsPlayerOne;
    private HapticClipPlayer hapticsPlayerThree;

    // Start is called before the first frame update
    void Start()
    {
        hapticsPlayerOne = new HapticClipPlayer(hapticClipOne);
        hapticsPlayerThree = new HapticClipPlayer(hapticClipThree);
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            hapticsPlayerOne.Play(Oculus.Haptics.Controller.Left);
        }
    }

    public void PlayHaptics(Handedness hand, HapticType type)
    {
        if (hand == Handedness.Left && type == HapticType.One)
        {
            hapticsPlayerOne.Play(Oculus.Haptics.Controller.Left);
        } else if (hand == Handedness.Left && type == HapticType.Three)
        {
            hapticsPlayerThree.Play(Oculus.Haptics.Controller.Left);
        } else if (hand == Handedness.Right && type == HapticType.One)
        {
            hapticsPlayerOne.Play(Oculus.Haptics.Controller.Right);
        } else if (hand == Handedness.Right && type == HapticType.Three)
        {
            hapticsPlayerThree.Play(Oculus.Haptics.Controller.Right);
        }
    }
}
