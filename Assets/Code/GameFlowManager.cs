using UnityEngine;
using Unity.Netcode;

public class GameFlowManager : NetworkBehaviour
{
    [Header("UI Groups (大群組)")]
    public GameObject uiIntroGroup;
    public GameObject uiTutorialGroup;
    public GameObject uiHudGroup;
    public GameObject uiGameOverGroup;

    // 🔥【控制進度條/血條的 Canvas】
    public GameObject uiTunnelCanvas;

    [Header("Tutorial Sub-Phases")]
    public GameObject tutorialPhase1_Instruction;
    public GameObject tutorialPhase2_Practice;

    [Header("Tutorial Pages")]
    public GameObject[] tutorialPages;

    [Header("Scripts & Objects")]
    public GameStatusController statusController;
    public FairyThrowerNetwork enemySpawnerScript;
    public FairyDifficultyController difficultyController;

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
        // A鍵確認
        if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
            HandleConfirmInput();

        // B鍵取消
        if (OVRInput.GetDown(OVRInput.Button.Two) || Input.GetKeyDown(KeyCode.Escape))
            HandleCancelInput();
    }

    void HandleConfirmInput()
    {
        switch (currentNetworkState.Value)
        {
            case GameState.Intro: OnClick_StartGame(); break;
            case GameState.Tutorial:
                if (tutorialPhase1_Instruction != null && tutorialPhase1_Instruction.activeSelf)
                {
                    if (tutorialPages != null && netTutorialPageIndex.Value >= tutorialPages.Length - 1)
                        OnClick_TutorialOK();
                    else
                        OnClick_NextTutorialPage();
                }
                break;
            case GameState.GameOver: OnClick_Restart(); break;
        }
    }

    void HandleCancelInput()
    {
        switch (currentNetworkState.Value)
        {
            case GameState.Tutorial: OnClick_SkipTutorial(); break;
            case GameState.GameOver: OnClick_Quit(); break;
        }
    }

    private void OnStateChanged(GameState oldState, GameState newState) => UpdateUIState(newState);
    private void OnTutorialPageChanged(int oldIndex, int newIndex) { if (currentNetworkState.Value == GameState.Tutorial) UpdateTutorialPageVisuals(newIndex); }

    private void UpdateUIState(GameState state)
    {
        // 1. 全部關閉
        if (uiIntroGroup) uiIntroGroup.SetActive(false);
        if (uiTutorialGroup) uiTutorialGroup.SetActive(false);
        if (uiHudGroup) uiHudGroup.SetActive(false);
        if (uiGameOverGroup) uiGameOverGroup.SetActive(false);
        if (uiTunnelCanvas) uiTunnelCanvas.SetActive(false); // 預設關閉血條

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

                // 🔥【開啟血條/進度條 Canvas】
                if (uiTunnelCanvas) uiTunnelCanvas.SetActive(true);

                if (enemySpawnerScript) enemySpawnerScript.autoSpawn = true;
                if (difficultyController) difficultyController.ResetDifficulty();
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
                if (tutorialPages[i] != null) tutorialPages[i].SetActive(i == index);
        }
    }

    // RPCs
    public void OnClick_StartGame() => RequestStateChangeServerRpc(GameState.Tutorial);
    public void OnClick_NextTutorialPage() => RequestNextTutorialPageServerRpc();
    public void OnClick_TutorialOK() => SwitchToPracticeServerRpc();
    public void OnClick_SkipTutorial() => RequestStateChangeServerRpc(GameState.Gameplay);
    public void OnClick_Restart() => RequestStateChangeServerRpc(GameState.Intro);
    public void OnClick_Quit() { Application.Quit(); }

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
            // 進遊戲時補滿血
            if (statusController) statusController.ResetGameplay();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNextTutorialPageServerRpc()
    {
        if (tutorialPages != null && netTutorialPageIndex.Value < tutorialPages.Length - 1)
            netTutorialPageIndex.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchToPracticeServerRpc()
    {
        int spiritCount = tutorialSpiritPrefabs.Count;
        if (statusController) statusController.tutorialTargetTotal = spiritCount;

        int itemsPerRow = 3;
        float spacingX = 0.4f;
        float spacingY = 0.3f;
        float startHeight = 1.3f;
        float distanceZ = 1.0f;

        for (int i = 0; i < spiritCount; i++)
        {
            var p = Instantiate(tutorialSpiritPrefabs[i]);
            int row = i / itemsPerRow;
            int col = i % itemsPerRow;
            p.transform.position = new Vector3((col - (itemsPerRow - 1) * 0.5f) * spacingX, startHeight - (row * spacingY), distanceZ);
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

    public void TriggerGameOver()
    {
        if (IsServer) currentNetworkState.Value = GameState.GameOver;
    }
}