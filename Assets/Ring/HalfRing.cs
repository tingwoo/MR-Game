using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Unity.Netcode;

public class HalfRing : MonoBehaviour
{
    public Transform collidePoint1, collidePoint2;
    public GameColor color = GameColor.Red;
    // public bool onLeftHand = true;
    public bool show = true;

    // public GameColor color;
    public GameObject redHalfRing;
    public GameObject yellowHalfRing;
    public GameObject blueHalfRing;
    public GameObject transHalfRing;

    public void UpdateModels()
    {

        // disable all rings first
        if (redHalfRing) redHalfRing.SetActive(false);
        if (yellowHalfRing) yellowHalfRing.SetActive(false);
        if (blueHalfRing) blueHalfRing.SetActive(false);
        if (transHalfRing) transHalfRing.SetActive(false);

        if (!show) return;

        // enable only the corresponding ring
        switch (color)
        {
            case GameColor.Red:
                if (redHalfRing) redHalfRing.SetActive(true);
                break;
            case GameColor.Yellow:
                if (yellowHalfRing) yellowHalfRing.SetActive(true);
                break;
            case GameColor.Blue:
                if (blueHalfRing) blueHalfRing.SetActive(true);
                break;
            case GameColor.Transparent:
                if (transHalfRing) transHalfRing.SetActive(true);
                break;
            default:
                // no ring active
                break;
        }
    }
}
