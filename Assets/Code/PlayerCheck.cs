using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCheck : NetworkBehaviour
{
    public Sprite[] ringImagesSource;
    public GameObject[] ringImages;
    public GameObject check1, check2, loadingIndicator, playButton;

    private NetworkVariable<int> netPlayerCount = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private NetworkList<int> ringColors;
    private RingManager ringManager;

    private void Awake()
    {
        ringColors = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        netPlayerCount.OnValueChanged += OnCountChanged;
        ringColors.OnListChanged += OnColorsChanged;

        UpdateVisuals(netPlayerCount.Value);

        if (IsServer)
        {
            ringManager = FindAnyObjectByType<RingManager>();
            if (ringColors.Count < 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    ringColors.Add(i); // Assuming 0 is the default color index
                }
            }
        }
        
        // Force an initial update of the UI based on current list state
        UpdateRingVisuals();
    }

    public override void OnNetworkDespawn()
    {
        netPlayerCount.OnValueChanged -= OnCountChanged;
        ringColors.OnListChanged -= OnColorsChanged;
    }

    // Dispose of the list
    public override void OnDestroy()
    {
        // Always check if the list is created before disposing
        if (ringColors != null)
        {
            ringColors.Dispose();
        }
        
        base.OnDestroy();
    }

    private void Update()
    {
        if (IsServer && ringManager != null)
        {
            int currentCount = VRNetworkRig.ActiveRigs.Count;
            if (netPlayerCount.Value != currentCount)
            {
                netPlayerCount.Value = currentCount;
            }
        }
    }

    private void OnCountChanged(int previousValue, int newValue)
    {
        UpdateVisuals(newValue);
    }

    private void OnColorsChanged(NetworkListEvent<int> changeEvent)
    {
        // Optimally, we just update the specific index that changed,
        // but updating all is safer for synchronization catch-up.
        UpdateRingVisuals();
    }

    private void UpdateRingVisuals() 
    {
        // Safety check to ensure we don't crash if the list isn't synced yet
        if (ringColors.Count < 4) return;

        for (int i = 0; i < 4; i++)
        {
            // Safety check for UI array bounds
            if (i < ringImages.Length) 
            {
                int colorIndex = ringColors[i];
                if(colorIndex >= 0 && colorIndex < ringImagesSource.Length)
                {
                    ringImages[i].GetComponent<Image>().sprite = ringImagesSource[colorIndex];
                }
            }
        }
    }

    public void SetRingColors(int idx, GameColor color)
    {
        if (!IsServer) return;
        // Safety check
        if (idx >= 0 && idx < ringColors.Count)
        {
            ringColors[idx] = (int)color;
        }
    }

    private void UpdateVisuals(int n)
    {
        // (Your existing visual logic remains unchanged)
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