using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode; // 引入 Netcode

public class GameStatusController : NetworkBehaviour // 改成 NetworkBehaviour
{
    [Header("必須連接的管理器")]
    public GameFlowManager gameFlowManager;

    [Header("教學設定")]
    public int tutorialTargetTotal = 3;
    // 使用 NetworkVariable 同步教學進度
    private NetworkVariable<int> netTutorialCount = new NetworkVariable<int>(0);
    public TextMeshProUGUI tutorialCountText;

    [Header("遊戲設定")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 5f;
    public float staminaRecovery = 15f;

    // 使用 NetworkVariable 同步體力和分數
    private NetworkVariable<float> netCurrentStamina = new NetworkVariable<float>(100f);
    private NetworkVariable<int> netCurrentScore = new NetworkVariable<int>(0);

    [Header("HUD")]
    public Image staminaFillImage;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText;

    void Update()
    {
        // 只有 Server 負責計算扣血邏輯
        if (IsServer && gameFlowManager.currentNetworkState.Value == GameFlowManager.GameState.Gameplay)
        {
            DecreaseStaminaServer();
        }

        // 所有人都負責更新 UI (讀取 NetworkVariable)
        UpdateHUD();
        UpdateTutorialUI();
    }

    // --- 教學邏輯 ---

    public void ResetTutorial()
    {
        if (IsServer) netTutorialCount.Value = 0;
    }

    // 這會被 TutorialTarget 呼叫
    public void OnTutorialTargetCaptured()
    {
        // 只有 Server 能修改數值
        if (IsServer)
        {
            netTutorialCount.Value++;

            if (netTutorialCount.Value >= tutorialTargetTotal)
            {
                Invoke("FinishTutorialServer", 1.0f);
            }
        }
        else
        {
            // 如果是 Client 抓到的，發送 RPC 告訴 Server
            SubmitTutorialCaptureServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitTutorialCaptureServerRpc()
    {
        netTutorialCount.Value++;
        if (netTutorialCount.Value >= tutorialTargetTotal)
        {
            Invoke("FinishTutorialServer", 1.0f);
        }
    }

    private void FinishTutorialServer()
    {
        // 透過 Manager 切換狀態
        gameFlowManager.OnClick_SkipTutorial(); // 這裡其實是呼叫 ServerRpc
    }

    // --- 遊戲邏輯 ---

    public void ResetGameplay()
    {
        if (IsServer)
        {
            netCurrentStamina.Value = maxStamina;
            netCurrentScore.Value = 0;
        }
    }

    // Server 專用扣血
    void DecreaseStaminaServer()
    {
        netCurrentStamina.Value -= staminaDrainRate * Time.deltaTime;

        if (netCurrentStamina.Value <= 0)
        {
            netCurrentStamina.Value = 0;
            // 通知 Manager 結束遊戲
            // 這裡直接改 State 變數即可，Manager 會偵測到
            // 但因為 Manager 已經有倒數機制，這裡可以雙重保險
        }
    }

    public void OnEnemyCaptured()
    {
        // 暫略，邏輯同 TutorialTargetCaptured
        // 如果是 Client 接到，要 ServerRpc 告知加分
    }

    // --- UI 更新 (所有人通用) ---
    // 不需要 RPC，因為 NetworkVariable 會自動同步，我們只要讀取 .Value 即可

    void UpdateTutorialUI()
    {
        if (tutorialCountText != null)
            tutorialCountText.text = $"教學進度: {netTutorialCount.Value} / {tutorialTargetTotal}";
    }

    void UpdateHUD()
    {
        if (staminaFillImage != null)
            staminaFillImage.fillAmount = netCurrentStamina.Value / maxStamina;

        if (scoreText != null)
            scoreText.text = $"Score: {netCurrentScore.Value}";

        if (finalScoreText != null)
            finalScoreText.text = $"{netCurrentScore.Value}";
    }
}