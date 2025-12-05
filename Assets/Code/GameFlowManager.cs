using UnityEngine;
using Unity.Netcode; // 1. 引入 Netcode 命名空間

// 2. 改成繼承 NetworkBehaviour (原本是 MonoBehaviour)
public class GameFlowManager : NetworkBehaviour
{
    [Header("UI Groups")]
    public GameObject uiIntroGroup;
    public GameObject uiTutorialGroup;
    public GameObject tutorialPhase1_Instruction;
    public GameObject tutorialPhase2_Practice;
    public GameObject uiHudGroup;
    public GameObject uiGameOverGroup;

    [Header("Scripts")]
    public GameStatusController statusController;
    public GameObject enemySpawner;

    // 使用 NetworkVariable 來同步目前的狀態，這樣後加入的人也能同步到正確狀態
    public NetworkVariable<GameState> currentNetworkState = new NetworkVariable<GameState>(GameState.Intro);

    public enum GameState { Intro, Tutorial, Gameplay, GameOver }

    public override void OnNetworkSpawn()
    {
        // 當連線建立時，監聽狀態變化
        currentNetworkState.OnValueChanged += OnStateChanged;

        // 初始化畫面 (如果是 Host，預設設為 Intro)
        if (IsServer)
        {
            currentNetworkState.Value = GameState.Intro;
        }
        else
        {
            // 如果是 Client，手動執行一次目前的狀態以同步畫面
            UpdateUIState(currentNetworkState.Value);
        }
    }

    // 當網路變數改變時，自動執行這個 (所有人 update UI)
    private void OnStateChanged(GameState oldState, GameState newState)
    {
        UpdateUIState(newState);
    }

    // --- 核心 UI 切換邏輯 (純視覺，不含網路邏輯) ---
    private void UpdateUIState(GameState state)
    {
        // 先關閉所有
        if (uiIntroGroup) uiIntroGroup.SetActive(false);
        if (uiTutorialGroup) uiTutorialGroup.SetActive(false);
        if (uiHudGroup) uiHudGroup.SetActive(false);
        if (uiGameOverGroup) uiGameOverGroup.SetActive(false);
        if (enemySpawner) enemySpawner.SetActive(false);

        switch (state)
        {
            case GameState.Intro:
                if (uiIntroGroup) uiIntroGroup.SetActive(true);
                Debug.Log("系統: 切換至 Intro");
                break;

            case GameState.Tutorial:
                if (uiTutorialGroup) uiTutorialGroup.SetActive(true);
                // 預設進入 Phase 1
                if (tutorialPhase1_Instruction) tutorialPhase1_Instruction.SetActive(true);
                if (tutorialPhase2_Practice) tutorialPhase2_Practice.SetActive(false);
                Debug.Log("系統: 切換至 Tutorial");
                break;

            case GameState.Gameplay:
                if (uiHudGroup) uiHudGroup.SetActive(true);
                if (enemySpawner) enemySpawner.SetActive(true);
                Debug.Log("系統: 切換至 Gameplay");
                break;

            case GameState.GameOver:
                if (uiGameOverGroup) uiGameOverGroup.SetActive(true);
                Debug.Log("系統: 切換至 GameOver");
                break;
        }
    }

    // --- 按鈕點擊事件 (這是起點) ---

    public void OnClick_StartGame()
    {
        // 當有人按下按鈕，發送請求給 Server
        RequestStateChangeServerRpc(GameState.Tutorial);
    }

    public void OnClick_SkipTutorial()
    {
        RequestStateChangeServerRpc(GameState.Gameplay);
    }

    public void OnClick_Restart()
    {
        RequestStateChangeServerRpc(GameState.Intro);
    }

    // 特殊：教學內部的切換 (Phase1 -> Phase2)
    public void OnClick_TutorialOK()
    {
        // 這裡因為只是子狀態，我們用 ClientRpc 廣播即可，或者簡單點，直接廣播
        SwitchToPracticeServerRpc();
    }

    public void OnClick_Quit()
    {
        Application.Quit();
    }

    // --- RPC 網路溝通區 ---

    // ServerRpc: Client 呼叫 -> Server 執行
    [ServerRpc(RequireOwnership = false)] // false 代表任何人都能按按鈕
    private void RequestStateChangeServerRpc(GameState newState)
    {
        // 只有 Server 能修改 NetworkVariable
        currentNetworkState.Value = newState;

        // 如果是進遊戲，Server 開始倒數
        if (newState == GameState.Gameplay)
        {
            // 取消舊的 Invoke，避免重複
            CancelInvoke("TriggerGameOverServer");
            Invoke("TriggerGameOverServer", 3.0f); // 3秒後結束
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchToPracticeServerRpc()
    {
        // 通知所有人切換到練習模式
        SwitchToPracticeClientRpc();
        // 通知 Status Controller 重置
        if (statusController) statusController.ResetTutorial();
    }

    [ClientRpc] // Server 呼叫 -> 所有人執行
    private void SwitchToPracticeClientRpc()
    {
        if (tutorialPhase1_Instruction) tutorialPhase1_Instruction.SetActive(false);
        if (tutorialPhase2_Practice) tutorialPhase2_Practice.SetActive(true);
    }

    // Server 專用的倒數結束
    private void TriggerGameOverServer()
    {
        currentNetworkState.Value = GameState.GameOver;
    }
}