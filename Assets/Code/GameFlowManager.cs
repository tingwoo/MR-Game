using UnityEngine;
using Unity.Netcode;

public class GameFlowManager : NetworkBehaviour
{
    [Header("UI Groups (大群組)")]
    public GameObject uiIntroGroup;
    public GameObject uiTutorialGroup;
    public GameObject uiHudGroup;
    public GameObject uiGameOverGroup;

    [Header("Tutorial Sub-Phases (教學子階段)")]
    public GameObject tutorialPhase1_Instruction;
    public GameObject tutorialPhase2_Practice;

    [Header("Tutorial Pages (教學幻燈片)")]
    public GameObject[] tutorialPages;

    [Header("Scripts & Objects")]
    public GameStatusController statusController;
    public GameObject enemySpawner;

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

    // =========================================================
    // 🔥 按鍵輸入監聽 (A鍵 與 B鍵)
    // =========================================================
    void Update()
    {
        // 1. 偵測確認鍵：Button A (右手) 或 Button X (左手) 或 鍵盤空白鍵
        // 功能：開始、下一頁、OK、Restart
        if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
        {
            HandleConfirmInput(); // 處理 A 鍵邏輯
        }

        // 2. 偵測取消/跳過鍵：Button B (右手) 或 Button Y (左手) 或 鍵盤 Esc
        // 功能：Skip、Quit
        if (OVRInput.GetDown(OVRInput.Button.Two) || Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCancelInput(); // 處理 B 鍵邏輯
        }
    }

    // --- A 鍵邏輯 (正面選項) ---
    void HandleConfirmInput()
    {
        switch (currentNetworkState.Value)
        {
            case GameState.Intro:
                // Intro: 按 A 開始遊戲
                OnClick_StartGame();
                break;

            case GameState.Tutorial:
                // Tutorial: 按 A 下一頁 / OK
                if (tutorialPhase1_Instruction != null && tutorialPhase1_Instruction.activeSelf)
                {
                    if (tutorialPages != null && netTutorialPageIndex.Value >= tutorialPages.Length - 1)
                    {
                        // 最後一頁 -> OK (進練習)
                        OnClick_TutorialOK();
                    }
                    else
                    {
                        // 還沒看完 -> 下一頁
                        OnClick_NextTutorialPage();
                    }
                }
                break;

            case GameState.Gameplay:
                // 遊戲中按 A 通常是抓東西，這裡不處理 UI
                break;

            case GameState.GameOver:
                // 【您的需求】GameOver: 按 A 重玩 (Restart)
                OnClick_Restart();
                break;
        }
    }

    // --- B 鍵邏輯 (負面選項) ---
    void HandleCancelInput()
    {
        switch (currentNetworkState.Value)
        {
            case GameState.Tutorial:
                // 【您的需求】Tutorial: 按 B 跳過 (Skip)
                OnClick_SkipTutorial();
                break;

            case GameState.GameOver:
                // GameOver: 按 B 退出 (Quit)
                OnClick_Quit();
                break;
        }
    }

    // =========================================================

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

    private void UpdateUIState(GameState state)
    {
        if (uiIntroGroup) uiIntroGroup.SetActive(false);
        if (uiTutorialGroup) uiTutorialGroup.SetActive(false);
        if (uiHudGroup) uiHudGroup.SetActive(false);
        if (uiGameOverGroup) uiGameOverGroup.SetActive(false);
        if (enemySpawner) enemySpawner.SetActive(false);

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
                if (enemySpawner) enemySpawner.SetActive(true);
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

    // --- 按鈕功能 (RPC 入口) ---

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

    // --- RPC 網路溝通區 ---

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
            Debug.Log("正式遊戲開始！啟動 10 秒倒數...");
            CancelInvoke("TriggerGameOverServer");
            Invoke("TriggerGameOverServer", 10.0f);
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