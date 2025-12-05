using UnityEngine;
using UnityEngine.EventSystems;

public class AutoFixVRInput : MonoBehaviour
{
    void Start()
    {
        // =========================================================
        // Step 1: Fix EventSystem (Reconnect the Hand Controller)
        // =========================================================
        OVRInputModule inputModule = GetComponent<OVRInputModule>();
        if (inputModule != null)
        {
            // Find the RightHandAnchor object in the scene by name
            // Note: Ensure your CameraRig actually has a child named "RightHandAnchor"
            GameObject rightHand = GameObject.Find("RightHandAnchor");
            
            if (rightHand != null)
            {
                inputModule.rayTransform = rightHand.transform;
                Debug.Log("[AutoFix] Success: OVRInputModule RayTransform bound to RightHandAnchor.");
            }
            else
            {
                Debug.LogError("[AutoFix] Error: 'RightHandAnchor' not found! Please check your OVRCameraRig.");
            }
        }
        else
        {
            Debug.LogWarning("[AutoFix] Warning: No OVRInputModule found on this GameObject.");
        }

        // =========================================================
        // Step 2: Fix Canvas (Reconnect the Event Camera)
        // =========================================================
        
        // Find the main VR Camera (CenterEyeAnchor)
        Camera centerEyeCam = null;
        GameObject centerEyeObj = GameObject.Find("CenterEyeAnchor");
        
        if (centerEyeObj != null) 
        {
            centerEyeCam = centerEyeObj.GetComponent<Camera>();
        }

        if (centerEyeCam != null)
        {
            // Find all Canvases in the scene
            Canvas[] canvases = FindObjectsOfType<Canvas>();

            foreach (var canvas in canvases)
            {
                // Only assign camera if the Canvas is in World Space mode
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    canvas.worldCamera = centerEyeCam;
                }
            }
            Debug.Log("[AutoFix] Success: Assigned CenterEyeAnchor to World Space Canvases.");
        }
        else
        {
            Debug.LogError("[AutoFix] Error: 'CenterEyeAnchor' (Camera) not found!");
        }
    }
}