using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkFullRing : NetworkBehaviour
{
    
    private NetworkVariable<GameColor> color = new NetworkVariable<GameColor>(
        GameColor.Red, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public GameColor Color
    {
        get => color.Value;
        // We omit the 'set' here to force users to use a specific method
        // which prevents accidental client-side crashes.
    }

    // public GameColor color;
    public GameObject redFullRing;
    public GameObject yellowFullRing;
    public GameObject blueFullRing;
    public GameObject orangeFullRing;
    public GameObject greenFullRing;
    public GameObject purpleFullRing;
    
    public override void OnNetworkSpawn()
    {
        // 1. Force the visual state to match the data immediately upon spawning
        UpdateModels(color.Value);

        // 2. Subscribe to changes so when the variable updates, the visuals update
        color.OnValueChanged += OnStateChanged;
    }

    public void SetColor(GameColor c)
    {
        if (IsServer)
        {
            color.Value = c;   
        }
    }

    private void OnStateChanged(GameColor previousValue, GameColor newValue)
    {
        UpdateModels(newValue);
    }

    private void UpdateModels(GameColor c)
    {
        // Disable all full-ring models first
        if (redFullRing) redFullRing.SetActive(false);
        if (yellowFullRing) yellowFullRing.SetActive(false);
        if (blueFullRing) blueFullRing.SetActive(false);
        if (orangeFullRing) orangeFullRing.SetActive(false);
        if (greenFullRing) greenFullRing.SetActive(false);
        if (purpleFullRing) purpleFullRing.SetActive(false);

        // Enable only the corresponding full-ring model
        switch (c)
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
}
