using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FullRing : MonoBehaviour
{
    public GameColor color = GameColor.Red;

    public GameObject redFullRing;
    public GameObject yellowFullRing;
    public GameObject blueFullRing;
    public GameObject orangeFullRing;
    public GameObject greenFullRing;
    public GameObject purpleFullRing;

    public (Handedness handedness, bool isServer) hand1;
    public (Handedness handedness, bool isServer) hand2;

    public void UpdateModels()
    {
        // Disable all full-ring models first
        if (redFullRing) redFullRing.SetActive(false);
        if (yellowFullRing) yellowFullRing.SetActive(false);
        if (blueFullRing) blueFullRing.SetActive(false);
        if (orangeFullRing) orangeFullRing.SetActive(false);
        if (greenFullRing) greenFullRing.SetActive(false);
        if (purpleFullRing) purpleFullRing.SetActive(false);

        // Enable only the corresponding full-ring model
        switch (color)
        {
            case GameColor.Red:
                if (redFullRing) redFullRing.SetActive(true);
                break;
            case GameColor.Yellow:
                if (yellowFullRing) yellowFullRing.SetActive(true);
                break;
            case GameColor.Blue:
                if (blueFullRing) blueFullRing.SetActive(true);
                break;
            case GameColor.Orange:
                if (orangeFullRing) orangeFullRing.SetActive(true);
                break;
            case GameColor.Green:
                if (greenFullRing) greenFullRing.SetActive(true);
                break;
            case GameColor.Purple:
                if (purpleFullRing) purpleFullRing.SetActive(true);
                break;
            default:
                // No ring active (e.g. Transparent or unknown)
                break;
        }
    }

    public void PlayHaptics()
    {
        if (hand1.isServer)
        {
            var haptics = FindObjectOfType<HapticsTest>();
            if (haptics != null)
            {
                haptics.PlayHaptics(hand1.handedness, HapticType.One);
            }
        }

        if (hand2.isServer)
        {
            var haptics = FindObjectOfType<HapticsTest>();
            if (haptics != null)
            {
                haptics.PlayHaptics(hand2.handedness, HapticType.One);
            }
        }
    }
}
