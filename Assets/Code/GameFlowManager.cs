using System.Collections;
using System.Collections.Generic;
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
    public GameObject tutorialPhase1_Instruction; // 閱讀區 (放 Canvas)
    public GameObject tutorialPhase2_Practice;    // 練習區 (放小精靈)

    [Header("Tutorial Pages (教學幻燈片 - 合併功能)")]
    // ⚠️ 請依序拖入: Page1_Rings, Page2_Colors, Page3_YourTurn
    public GameObject[] tutorialPages;

    [Header("Scripts & Objects")]
    public GameStatusController statusController;
    public GameObject enemySpawner; // 這裡是你的 FairyThrower

    // --- 網路變數同步區 ---

    // 1. 遊戲大狀態 (Intro, Tutorial, Gameplay, GameOver)
    public NetworkVariable<GameState> currentNetworkState = new NetworkVariable<GameState>(GameState.Intro);

    // 2. 教學頁碼 (0, 1, 2...)
    private NetworkVariable<int> netTutorialPageIndex = new NetworkVariable<int>(0);

    public List<NetworkObject> tutorialSpiritPrefabs;

    public enum GameState { Intro, Tutorial, Gameplay, GameOver }

    public override void OnNetworkSpawn()
    {
        // 當連線建立時，監聽狀態變化
        currentNetworkState.OnValueChanged += OnStateChanged;
        netTutorialPageIndex.OnValueChanged += OnTutorialPageChanged;

        // 初始化
        if (IsServer)
        {
            currentNetworkState.Value = GameState.Intro;
            netTutorialPageIndex.Value = 0;
        }
        else
        {
            // Client 進來時手動更新一次畫面
            UpdateUIState(currentNetworkState.Value);
            UpdateTutorialPageVisuals(netTutorialPageIndex.Value);
        }
    }

    // --- 狀態監聽與更新 ---

    private void OnStateChanged(GameState oldState, GameState newState)
    {
        UpdateUIState(newState);
    }

    private void OnTutorialPageChanged(int oldIndex, int newIndex)
    {
        // 只有在教學模式下才需要更新頁面
        if (currentNetworkState.Value == GameState.Tutorial)
        {
            UpdateTutorialPageVisuals(newIndex);
        }
    }

    // --- 視覺處理邏輯 (Visuals) ---

    private void UpdateUIState(GameState state)
    {
        // 1. 先全部關閉
        if (uiIntroGroup) uiIntroGroup.SetActive(false);
        if (uiTutorialGroup) uiTutorialGroup.SetActive(false);
        if (uiHudGroup) uiHudGroup.SetActive(false);
        if (uiGameOverGroup) uiGameOverGroup.SetActive(false);
        if (enemySpawner) enemySpawner.SetActive(false);

        // 2. 根據狀態開啟對應 UI
        switch (state)
        {
            case GameState.Intro:
                if (uiIntroGroup) uiIntroGroup.SetActive(true);
                Debug.Log("系統: 切換至 Intro");
                break;

            case GameState.Tutorial:
                if (uiTutorialGroup) uiTutorialGroup.SetActive(true);

                // 進入教學時，預設顯示 Phase 1 (說明)，隱藏 Phase 2 (練習)
                if (tutorialPhase1_Instruction) tutorialPhase1_Instruction.SetActive(true);
                if (tutorialPhase2_Practice) tutorialPhase2_Practice.SetActive(false);

                // 確保從第一頁開始
                UpdateTutorialPageVisuals(netTutorialPageIndex.Value);
                Debug.Log("系統: 切換至 Tutorial (Phase 1)");
                break;

            case GameState.Gameplay:
                if (uiHudGroup) uiHudGroup.SetActive(true);
                if (enemySpawner) enemySpawner.SetActive(true); // 開啟生怪器
                Debug.Log("系統: 切換至 Gameplay");
                break;

            case GameState.GameOver:
                if (uiGameOverGroup) uiGameOverGroup.SetActive(true);
                Debug.Log("系統: 切換至 GameOver");
                break;
        }
    }

    private void UpdateTutorialPageVisuals(int index)
    {
        // 迴圈檢查每一頁，只有對應 index 的頁面打開，其他全關
        if (tutorialPages != null)
        {
            for (int i = 0; i < tutorialPages.Length; i++)
            {
                if (tutorialPages[i] != null)
                    tutorialPages[i].SetActive(i == index);
            }
        }
    }

    // --- 按鈕點擊事件 (UI Button OnClick) ---

    public void OnClick_StartGame()
    {
        // 開始遊戲 -> 進入教學模式
        RequestStateChangeServerRpc(GameState.Tutorial);
    }

    // 新增：教學換頁按鈕 (綁定給 Page1 和 Page2 的 Button_Next)
    public void OnClick_NextTutorialPage()
    {
        RequestNextTutorialPageServerRpc();
    }

    // 教學 OK 按鈕 (綁定給 Page3 的 Button_OK)
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
        // 重玩 -> 回到 Intro
        RequestStateChangeServerRpc(GameState.Intro);
    }

    public void OnClick_Quit()
    {
        Application.Quit();
    }

    // --- RPC 網路溝通區 (Logic) ---

    [ServerRpc(RequireOwnership = false)]
    private void RequestStateChangeServerRpc(GameState newState)
    {
        currentNetworkState.Value = newState;

        // 初始化狀態數值
        if (newState == GameState.Tutorial)
        {
            netTutorialPageIndex.Value = 0; // 重置頁碼
        }
        else if (newState == GameState.Gameplay)
        {
            // ⚠️ 注意：這裡原本有 3 秒結束的測試代碼。
            // 既然現在你有 GameStatusController 來控制血量歸零結束，
            // 建議把下面這兩行註解掉，讓遊戲能正常玩。

            // CancelInvoke("TriggerGameOverServer");
            // Invoke("TriggerGameOverServer", 3.0f); 
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNextTutorialPageServerRpc()
    {
        // 如果還沒到最後一頁，就 +1
        if (tutorialPages != null && netTutorialPageIndex.Value < tutorialPages.Length - 1)
        {
            netTutorialPageIndex.Value++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchToPracticeServerRpc()
    {
        int spiritCount = tutorialSpiritPrefabs.Count;
        statusController.tutorialTargetTotal = spiritCount;
        for (int i = 0; i < spiritCount; i++)
        {
            var p = Instantiate(tutorialSpiritPrefabs[i]);
            p.transform.position = new Vector3(0.5f * (i - (spiritCount - 1) * 0.5f), 1f, 1f);
            p.Spawn();
        }

        // 通知所有人切換到練習模式 (Phase 2)
        SwitchToPracticeClientRpc();

        // 重置教學計數 (由 Server 執行)
        if (statusController) statusController.ResetTutorial();
    }

    [ClientRpc]
    private void SwitchToPracticeClientRpc()
    {
        // 關閉說明，開啟練習
        if (tutorialPhase1_Instruction) tutorialPhase1_Instruction.SetActive(false);
        // if (tutorialPhase2_Practice) tutorialPhase2_Practice.SetActive(true);
    }

    // Server 專用的倒數結束 (配合上面的 Invoke 使用，如果沒用 Invoke 則備用)
    public void TriggerGameOverServer() // 改成 Public 讓 StatusController 可以呼叫
    {
        currentNetworkState.Value = GameState.GameOver;
    }

    // 提供給 GameStatusController 呼叫的接口 (因為它在 Client 端也可能需要發起)
    public void TriggerGameOver()
    {
        if (IsServer)
        {
            TriggerGameOverServer();
        }
        else
        {
            // 如果是 Client 發現血量歸零，可以寫一個 ServerRpc 來通知
            // 但目前的架構是 Server 算血量，所以 Server 會自己呼叫 TriggerGameOverServer
        }
    }
}