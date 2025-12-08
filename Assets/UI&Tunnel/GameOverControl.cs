using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverControl : NetworkBehaviour
{
    // Allow any client (even if not owner) to call this RPC
    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartServerRpc()
    {
        if (IsServer)
        {
            // Name of your main game scene
            string mainSceneName = "Ring"; 
            
            // Load the main scene for everyone
            NetworkManager.Singleton.SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
        }
    }

    // Call this method from the UI Button OnClick event
    public void OnRestartButtonClicked()
    {
        RequestRestartServerRpc();
    }
}