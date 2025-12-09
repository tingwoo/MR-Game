using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class GameStatusController : NetworkBehaviour
{
    [Header("必須連接的管理器")]
    public GameFlowManager gameFlowManager;

    [Header("教學設定")]
    public int tutorialTargetTotal = 6;
    private NetworkVariable<int> netTutorialCount = new NetworkVariable<int>(0);
    public TextMeshProUGUI tutorialCountText;

    [Header("遊戲設定 (分數與體力)")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 2f;  // 建議改小一點，例如 2，不然死太快
    public float staminaRecovery = 15f;
    public int scorePerSpirit = 1;

    // 遊戲數據 (自動同步)
    private NetworkVariable<float> netCurrentStamina = new NetworkVariable<float>(100f);
    private NetworkVariable<int> netCurrentScore = new NetworkVariable<int>(0);

    [Header("HUD (介面)")]
    public Image staminaFillImage; // 您原本的 (如果還有用)
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText;

    // 🔥【新增】朋友做的 UI 介面
    [Header("Friend's UI Integration")]
    public Slider friendStaminaSlider;
    public Image friendFillImage; // 如果想要控制顏色變化 (綠->紅)

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            netTutorialCount.Value = 0;
            netCurrentScore.Value = 0;
            netCurrentStamina.Value = maxStamina;
        }

        // 初始化 UI 最大值
        if (friendStaminaSlider != null) friendStaminaSlider.maxValue = maxStamina;

        UpdateHUD();
        UpdateTutorialUI();
    }

    void Update()
    {
        // 只有 Server 負責扣血 (在 Gameplay 狀態下)
        if (IsServer && gameFlowManager.currentNetworkState.Value == GameFlowManager.GameState.Gameplay)
        {
            DecreaseStaminaServer();
        }

        // 所有人都要更新 UI
        UpdateHUD();
        UpdateTutorialUI();
    }

    // --- 教學與重置邏輯 ---
    public void ResetTutorial()
    {
        if (IsServer) netTutorialCount.Value = 0;
        UpdateTutorialUI();
    }

    public void ResetGameplay()
    {
        if (IsServer)
        {
            netCurrentStamina.Value = maxStamina;
            netCurrentScore.Value = 0;
        }
    }

    // --- 加分與扣血邏輯 ---
    public void OnTutorialTargetCaptured()
    {
        if (IsServer) HandleTutorialCapture();
        else SubmitTutorialCaptureServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitTutorialCaptureServerRpc() => HandleTutorialCapture();

    private void HandleTutorialCapture()
    {
        netTutorialCount.Value++;
        if (netTutorialCount.Value >= tutorialTargetTotal)
        {
            CancelInvoke("FinishTutorialServer");
            Invoke("FinishTutorialServer", 1.0f);
        }
    }

    private void FinishTutorialServer() => gameFlowManager.OnClick_SkipTutorial();

    // 被 SpiritDestroy 呼叫
    public void OnEnemyCaptured()
    {
        if (IsServer) AddScoreServer();
        else RequestAddScoreServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAddScoreServerRpc() => AddScoreServer();

    private void AddScoreServer()
    {
        netCurrentScore.Value += scorePerSpirit;
        netCurrentStamina.Value += staminaRecovery;
        if (netCurrentStamina.Value > maxStamina)
            netCurrentStamina.Value = maxStamina;
    }

    void DecreaseStaminaServer()
    {
        netCurrentStamina.Value -= staminaDrainRate * Time.deltaTime;
        if (netCurrentStamina.Value <= 0)
        {
            netCurrentStamina.Value = 0;
            gameFlowManager.TriggerGameOver();
        }
    }

    // --- UI 更新邏輯 ---
    void UpdateTutorialUI()
    {
        if (tutorialCountText != null)
            tutorialCountText.text = $"教學進度: {netTutorialCount.Value} / {tutorialTargetTotal}";
    }

    void UpdateHUD()
    {
        // 1. 更新數值
        if (scoreText != null) scoreText.text = $"Score: {netCurrentScore.Value}";
        if (finalScoreText != null) finalScoreText.text = $"{netCurrentScore.Value}";

        // 2. 更新朋友的 Slider
        if (friendStaminaSlider != null)
        {
            // 使用 Lerp 讓血條移動平滑一點
            friendStaminaSlider.value = Mathf.Lerp(friendStaminaSlider.value, netCurrentStamina.Value, Time.deltaTime * 5f);
        }

        // 3. (選用) 更新顏色：血量低於 30% 變紅，否則為黃/綠
        if (friendFillImage != null)
        {
            float ratio = netCurrentStamina.Value / maxStamina;
            Color healthyColor = new Color(1f, 0.87f, 0.65f); // 朋友原本的米黃色
            friendFillImage.color = Color.Lerp(Color.red, healthyColor, ratio);
        }

        // 4. 更新您原本的 Image Fill (如果還留著)
        if (staminaFillImage != null)
            staminaFillImage.fillAmount = netCurrentStamina.Value / maxStamina;
    }
}