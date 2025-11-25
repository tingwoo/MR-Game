using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkHalfRing : NetworkBehaviour
{
    public Transform collidePoint1, collidePoint2;
    
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

    private NetworkVariable<bool> onLeftHand = new NetworkVariable<bool>(
        true, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public bool OnLeftHand
    {
        get => onLeftHand.Value;
        // We omit the 'set' here to force users to use a specific method
        // which prevents accidental client-side crashes.
    }

    private NetworkVariable<bool> show = new NetworkVariable<bool>(
        true, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public bool Show
    {
        get => show.Value;
        // We omit the 'set' here to force users to use a specific method
        // which prevents accidental client-side crashes.
    }

    // public GameColor color;
    public GameObject redHalfRing;
    public GameObject yellowHalfRing;
    public GameObject blueHalfRing;
    public GameObject transHalfRing;
    
    public override void OnNetworkSpawn()
    {
        // 1. Force the visual state to match the data immediately upon spawning
        UpdateModels(color.Value);

        // 2. Subscribe to changes so when the variable updates, the visuals update
        color.OnValueChanged += OnColorChanged;
        show.OnValueChanged += OnShowChanged;
    }

    public void SetHandedness(bool isLeft)
    {
        if (IsServer)
        {
            onLeftHand.Value = isLeft;   
        }
    }

    public void SetShow(bool s)
    {
        if (IsServer)
        {
            show.Value = s;   
        }
    }

    public void SetColor(GameColor c)
    {
        if (IsServer)
        {
            color.Value = c;   
        }
    }

    private void OnColorChanged(GameColor previousValue, GameColor newValue)
    {
        UpdateModels(newValue);
    }

    private void OnShowChanged(bool previousValue, bool newValue)
    {
        UpdateModels();
    }

    private void UpdateModels()
    {
        UpdateModels(Color);
    }

    private void UpdateModels(GameColor c)
    {

        // disable all rings first
        if (redHalfRing) redHalfRing.SetActive(false);
        if (yellowHalfRing) yellowHalfRing.SetActive(false);
        if (blueHalfRing) blueHalfRing.SetActive(false);
        if (transHalfRing) transHalfRing.SetActive(false);

        if (!Show) return;

        // enable only the corresponding ring
        switch (c)
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
