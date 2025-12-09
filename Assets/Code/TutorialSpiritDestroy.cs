using UnityEngine;
using Unity.Netcode;

// Inherit from SpiritDestroy to get Sound, Haptics, and VFX logic automatically
public class TutorialSpiritDestroy : SpiritDestroy
{
    // We override only the logic that happens specifically when captured.
    // Base implementation adds Stamina; this implementation updates Tutorial Progress.
    protected override void OnContactLogic()
    {
        // A. Tutorial Progress +1
        var status = FindObjectOfType<GameStatusController>();
        if (status != null)
        {
            status.OnTutorialTargetCaptured();
        }
        
        // Note: We do NOT call base.OnContactLogic() because 
        // we don't want to add Stamina/Score in the tutorial.
    }
    
    // Note on Inspector Setup:
    // Since we inherit from SpiritDestroy, you will now see the 
    // "Audio Settings" (AudioClip destroySound) in the Inspector for this object too.
    // Make sure to assign the sound file there!
}