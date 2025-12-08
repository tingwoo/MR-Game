using UnityEngine;
using Unity.Netcode;

public class GameFlowManager : NetworkBehaviour
{
    [Header("UI Groups (大群組)")]
    public GameObject uiIntroGroup;
    public GameObject uiTutorialGroup;
    public GameObject uiHudGroup;
    public GameObject uiGameOverGroup;
    public GameObject uiTunnelCanvas;

    [Header("Tutorial Sub-Phases (教學子階段)")]
    public GameObject tutorialPhase1_Instruction;
    public GameObject tutorialPhase2_Practice;

    [Header("Tutorial Pages (教學幻燈片)")]
    public GameObject[] tutorialPages;

    [Header("Scripts & Objects")]
    public GameStatusController statusController;


    // 🔴【修改】改用腳本控制，移除原本的 GameObject enemySpawner
    public FairyThrowerNetwork enemySpawnerScript;
    public FairyDifficultyController difficultyController;

    // --- 網路變數 ---
    public NetworkVariable<GameState> currentNetworkState = new NetworkVariable<GameState>(GameState.Intro);
    private NetworkVariable<int> netTutorialPageIndex = new NetworkVariable<int>(0);

    public System.Collections.Generic.List<NetworkObject> tutorialSpiritPrefabs;

    public enum GameState { Intro, Tutorial, Gameplay, GameOver }

    public override void OnNetworkSpawn()
    {
        currentNetworkState.OnValueChanged += OnStateChanged;
        netTutorialPageIndex.OnValueChanged += OnTutorialPageChanged;

        if (IsServer)
        {
            currentNetworkState.Value = GameState.Intro;
            netTutorialPageIndex.Value = 0;
        }
        else
        {
            UpdateUIState(currentNetworkState.Value);
            UpdateTutorialPageVisuals(netTutorialPageIndex.Value);
        }
    }

    void Update()
    {
        // 1. 偵測確認鍵 (A鍵)
        if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
        {
            HandleConfirmInput();
        }

        // 2. 偵測取消鍵 (B鍵)
        if (OVRInput.GetDown(OVRInput.Button.Two) || Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCancelInput();
        }
    }

    void HandleConfirmInput()
    {
        switch (currentNetworkState.Value)
        {
            case GameState.Intro:
                OnClick_StartGame();
                break;

            case GameState.Tutorial:
                if (tutorialPhase1_Instruction != null && tutorialPhase1_Instruction.activeSelf)
                {
                    if (tutorialPages != null && netTutorialPageIndex.Value >= tutorialPages.Length - 1)
                    {
                        OnClick_TutorialOK();
                    }
                    else
                    {
                        OnClick_NextTutorialPage();
                    }
                }
                break;

            case GameState.GameOver:
                OnClick_Restart();
                break;
        }
    }

    void HandleCancelInput()
    {
        switch (currentNetworkState.Value)
        {
            case GameState.Tutorial:
                OnClick_SkipTutorial();
                break;

            case GameState.GameOver:
                OnClick_Quit();
                break;
        }
    }

    private void OnStateChanged(GameState oldState, GameState newState)
    {
        UpdateUIState(newState);
    }

    private void OnTutorialPageChanged(int oldIndex, int newIndex)
    {
        if (currentNetworkState.Value == GameState.Tutorial)
        {
            UpdateTutorialPageVisuals(newIndex);
        }
    }

    // 🔴【關鍵修改】UI 狀態切換邏輯
    private void UpdateUIState(GameState state)
    {
        // 1. 關閉所有 UI
        if (uiIntroGroup) uiIntroGroup.SetActive(false);
        if (uiTutorialGroup) uiTutorialGroup.SetActive(false);
        if (uiHudGroup) uiHudGroup.SetActive(false);
        if (uiGameOverGroup) uiGameOverGroup.SetActive(false);
        if (uiTunnelCanvas) uiTunnelCanvas.SetActive(false);
        
        // 2. 預設「關閉生怪功能」(但物件保持開啟)
        if (enemySpawnerScript) enemySpawnerScript.autoSpawn = false;

        switch (state)
        {
            case GameState.Intro:
                if (uiIntroGroup) uiIntroGroup.SetActive(true);
                break;

            case GameState.Tutorial:
                if (uiTutorialGroup) uiTutorialGroup.SetActive(true);
                if (tutorialPhase1_Instruction) tutorialPhase1_Instruction.SetActive(true);
                if (tutorialPhase2_Practice) tutorialPhase2_Practice.SetActive(false);
                UpdateTutorialPageVisuals(netTutorialPageIndex.Value);
                break;

            case GameState.Gameplay:
                if (uiHudGroup) uiHudGroup.SetActive(true);
                if (uiTunnelCanvas) uiTunnelCanvas.SetActive(true);
                // 🔴【關鍵】進入遊戲，開啟自動生怪，並重置難度
                if (enemySpawnerScript)
                {
                    enemySpawnerScript.autoSpawn = true;
                    // if (IsServer) enemySpawnerScript.ThrowOne(); // 立刻先生一隻
                }
                if (difficultyController)
                {
                    difficultyController.ResetDifficulty();
                }
                break;

            case GameState.GameOver:
                if (uiGameOverGroup) uiGameOverGroup.SetActive(true);
                break;
        }
    }

    private void UpdateTutorialPageVisuals(int index)
    {
        if (tutorialPages != null)
        {
            for (int i = 0; i < tutorialPages.Length; i++)
            {
                if (tutorialPages[i] != null)
                    tutorialPages[i].SetActive(i == index);
            }
        }
    }

    // --- RPC ---

    public void OnClick_StartGame()
    {
        RequestStateChangeServerRpc(GameState.Tutorial);
    }

    public void OnClick_NextTutorialPage()
    {
        RequestNextTutorialPageServerRpc();
    }

    public void OnClick_TutorialOK()
    {
        SwitchToPracticeServerRpc();
    }

    public void OnClick_SkipTutorial()
    {
        RequestStateChangeServerRpc(GameState.Gameplay);
    }

    public void OnClick_Restart()
    {
        RequestStateChangeServerRpc(GameState.Intro);
    }

    public void OnClick_Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestStateChangeServerRpc(GameState newState)
    {
        currentNetworkState.Value = newState;

        if (newState == GameState.Tutorial)
        {
            netTutorialPageIndex.Value = 0;
        }
        else if (newState == GameState.Gameplay)
        {
            Debug.Log("正式遊戲開始！");

            // 🔥【關鍵修正 1】重置遊戲數據 (補滿血、分數歸零)
            if (statusController) 
            {
                statusController.ResetGameplay();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNextTutorialPageServerRpc()
    {
        if (tutorialPages != null && netTutorialPageIndex.Value < tutorialPages.Length - 1)
        {
            netTutorialPageIndex.Value++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchToPracticeServerRpc()
    {
        int spiritCount = tutorialSpiritPrefabs.Count;
        if (statusController) statusController.tutorialTargetTotal = spiritCount;

        int itemsPerRow = 3;
        float spacingX = 0.5f;
        float spacingY = 0.5f;
        float startHeight = 1.3f;
        float distanceZ = 1.0f;

        for (int i = 0; i < spiritCount; i++)
        {
            var p = Instantiate(tutorialSpiritPrefabs[i]);
            int row = i / itemsPerRow;
            int col = i % itemsPerRow;
            float xPos = (col - (itemsPerRow - 1) * 0.5f) * spacingX;
            float yPos = startHeight - (row * spacingY);

            p.transform.position = new Vector3(xPos, yPos, distanceZ);
            p.Spawn();
        }

        SwitchToPracticeClientRpc();
        if (statusController) statusController.ResetTutorial();
    }

    [ClientRpc]
    private void SwitchToPracticeClientRpc()
    {
        if (tutorialPhase1_Instruction) tutorialPhase1_Instruction.SetActive(false);
    }

    public void TriggerGameOverServer()
    {
        currentNetworkState.Value = GameState.GameOver;
    }

    public void TriggerGameOver()
    {
        if (IsServer) TriggerGameOverServer();
    }
}