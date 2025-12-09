using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCheck : NetworkBehaviour
{
    public GameObject check1, check2, loadingIndicator, playButton;

    // 1. Create a NetworkVariable to hold the state.
    // "value" defaults to 0. "NetworkVariableReadPermission.Everyone" ensures clients can read it.
    private NetworkVariable<int> netPlayerCount = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private RingManager ringManager;

    public override void OnNetworkSpawn()
    {
        // 2. Subscribe to the variable changing. 
        // This runs on BOTH Server and Clients whenever the value changes.
        netPlayerCount.OnValueChanged += OnCountChanged;

        // 3. Initial check to set visuals immediately upon spawn based on current data
        UpdateVisuals(netPlayerCount.Value);

        // 4. Cache the manager (Server side mainly)
        if (IsServer)
        {
            ringManager = FindAnyObjectByType<RingManager>();
        }
    }

    public override void OnNetworkDespawn()
    {
        // Clean up subscription to avoid memory leaks
        netPlayerCount.OnValueChanged -= OnCountChanged;
    }

    private void Update()
    {
        // 5. The SERVER checks the manager and updates the NetworkVariable.
        // Clients do not run this logic; they just react to the variable changing.
        if (IsServer && ringManager != null)
        {
            int currentCount = ringManager.PlayerCount();
            
            // Only assign if different to prevent spamming network traffic
            if (netPlayerCount.Value != currentCount)
            {
                netPlayerCount.Value = currentCount;
            }
        }
    }

    // 6. The Event Listener
    private void OnCountChanged(int previousValue, int newValue)
    {
        UpdateVisuals(newValue);
    }

    // 7. Pure visual logic (No networking code here)
    private void UpdateVisuals(int n)
    {
        if (n == 0)
        {
            check1.SetActive(false);
            check2.SetActive(false);
            loadingIndicator.SetActive(true);
            playButton.SetActive(false);
        }
        else if (n == 1)
        {
            check1.SetActive(true);
            check2.SetActive(false);
            loadingIndicator.SetActive(true);
            playButton.SetActive(false);
        }
        else
        {
            check1.SetActive(true);
            check2.SetActive(true);
            loadingIndicator.SetActive(false);
            playButton.SetActive(true);
        }
    }
}