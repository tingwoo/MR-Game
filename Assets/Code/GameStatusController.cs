using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class GameStatusController : NetworkBehaviour
{
    [Header("必須連接的管理器")]
    public GameFlowManager gameFlowManager;

    [Header("教學設定")]
    public int tutorialTargetTotal = 6; // 預設設為 6，Manager 會再覆寫它
    
    // 教學進度 (NetworkVariable 自動同步)
    private NetworkVariable<int> netTutorialCount = new NetworkVariable<int>(0);
    public TextMeshProUGUI tutorialCountText;

    [Header("遊戲設定 (分數與體力)")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 5f;  // 每秒扣血量
    public float staminaRecovery = 15f;  // 抓到一隻回血量
    public int scorePerSpirit = 100;     // 一隻幾分

    // 遊戲數據 (NetworkVariable 自動同步)
    private NetworkVariable<float> netCurrentStamina = new NetworkVariable<float>(100f);
    private NetworkVariable<int> netCurrentScore = new NetworkVariable<int>(0);

    [Header("HUD (介面)")]
    public Image staminaFillImage;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText; // 結算畫面的數字

    // 當連線建立時，初始化變數
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            netTutorialCount.Value = 0;
            netCurrentScore.Value = 0;
            netCurrentStamina.Value = maxStamina;
        }
        // 強制更新一次 UI
        UpdateHUD();
        UpdateTutorialUI();
    }

    void Update()
    {
        // Server 負責扣血邏輯 (只有在 Gameplay 狀態下)
        if (IsServer && gameFlowManager.currentNetworkState.Value == GameFlowManager.GameState.Gameplay)
        {
            DecreaseStaminaServer();
        }

        // Client & Server 都要負責更新 UI (從 NetworkVariable 讀值)
        UpdateHUD();
        UpdateTutorialUI();
    }

    // ==========================================
    //              教學模式邏輯
    // ==========================================

    public void ResetTutorial()
    {
        if (IsServer) netTutorialCount.Value = 0;
        UpdateTutorialUI();
    }

    // 被 TutorialTarget.cs 呼叫
    public void OnTutorialTargetCaptured()
    {
        if (IsServer)
        {
            HandleTutorialCapture();
        }
        else
        {
            SubmitTutorialCaptureServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitTutorialCaptureServerRpc()
    {
        HandleTutorialCapture();
    }

    // 統一處理教學進度 (Server 端)
    private void HandleTutorialCapture()
    {
        netTutorialCount.Value++;
        
        Debug.Log($"教學進度: {netTutorialCount.Value}/{tutorialTargetTotal}");

        if (netTutorialCount.Value >= tutorialTargetTotal)
        {
            // 延遲 1 秒後進入正式遊戲 (避免畫面切太快)
            CancelInvoke("FinishTutorialServer");
            Invoke("FinishTutorialServer", 1.0f);
        }
    }

    private void FinishTutorialServer()
    {
        gameFlowManager.OnClick_SkipTutorial(); // 切換到 Gameplay
    }


    // ==========================================
    //              正式遊戲邏輯 (分數)
    // ==========================================

    public void ResetGameplay()
    {
        if (IsServer)
        {
            netCurrentStamina.Value = maxStamina;
            netCurrentScore.Value = 0;
        }
    }

    // 被 SpiritDestroy.cs 呼叫 (即使是別人寫的腳本，也要透過這裡加分)
    public void OnEnemyCaptured()
    {
        if (IsServer)
        {
            AddScoreServer();
        }
        else
        {
            RequestAddScoreServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAddScoreServerRpc()
    {
        AddScoreServer();
    }

    private void AddScoreServer()
    {
        // 1. 加分
        netCurrentScore.Value += scorePerSpirit;

        // 2. 回血
        netCurrentStamina.Value += staminaRecovery;
        if (netCurrentStamina.Value > maxStamina) 
            netCurrentStamina.Value = maxStamina;
    }

    // Server 專用扣血
    void DecreaseStaminaServer()
    {
        netCurrentStamina.Value -= staminaDrainRate * Time.deltaTime;

        if (netCurrentStamina.Value <= 0)
        {
            netCurrentStamina.Value = 0;
            // 血量歸零 -> 觸發 Game Over
            gameFlowManager.TriggerGameOver(); 
        }
    }

    // ==========================================
    //              UI 更新 (Visuals)
    // ==========================================

    void UpdateTutorialUI()
    {
        if (tutorialCountText != null)
            tutorialCountText.text = $"教學進度: {netTutorialCount.Value} / {tutorialTargetTotal}";
    }

    void UpdateHUD()
    {
        // 更新血條
        if (staminaFillImage != null)
            staminaFillImage.fillAmount = netCurrentStamina.Value / maxStamina;

        // 更新遊戲中分數
        if (scoreText != null)
            scoreText.text = $"Score: {netCurrentScore.Value}";

        // 更新結算分數 (只顯示數字，配合你的圖片填空)
        if (finalScoreText != null)
            finalScoreText.text = $"{netCurrentScore.Value}";
    }
}