using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement; // Important for Scene Loading

public class StaminaBarController : NetworkBehaviour
{
    public static StaminaBarController Instance { get; private set; }

    [Header("UI Elements")]
    public Slider staminaSlider;
    public Image fillImage;

    [Header("Settings")]
    public float maxStamina = 100f;
    public float drainSpeed = 10f;
    public float smoothSpeed = 5f;

    public Color fullColor = new Color(1f, 0.8745f, 0.651f);
    public Color lowColor = Color.red;

    private readonly NetworkVariable<float> _netStamina = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float _displayedStamina;
    private bool _isGameOverTriggered = false; // Prevent multiple triggers

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        _displayedStamina = _netStamina.Value;
        if(staminaSlider != null) staminaSlider.maxValue = maxStamina;
    }

    void Update()
    {
        if (IsServer)
        {
            if (!_isGameOverTriggered)
            {
                float current = _netStamina.Value;
                current -= drainSpeed * Time.deltaTime;
                _netStamina.Value = Mathf.Clamp(current, 0, maxStamina);

                // Check for Game Over condition
                if (current <= 0f)
                {
                    _isGameOverTriggered = true;
                    LoadGameOverScene();
                }
            }
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        float targetStamina = _netStamina.Value;
        _displayedStamina = Mathf.Lerp(_displayedStamina, targetStamina, Time.deltaTime * smoothSpeed);
        
        if (staminaSlider != null) staminaSlider.value = _displayedStamina;

        if (fillImage != null)
        {
            float percent = _displayedStamina / maxStamina;
            fillImage.color = Color.Lerp(lowColor, fullColor, percent);
        }
    }

    public void AddStaminaServer(float amount)
    {
        if (!IsServer) return;
        if (_isGameOverTriggered) return; // Do not add score if game is over

        float current = _netStamina.Value;
        _netStamina.Value = Mathf.Clamp(current + amount, 0, maxStamina);
    }

// Inside StaminaBarController.cs

    private void LoadGameOverScene()
    {
        // 1. 清理動態物件 (確保先做這步)
        if (IsServer)
        {
            if (GameCleanupManager.Instance != null)
            {
                GameCleanupManager.Instance.CleanupAllDynamicObjectsServer();
            }
            else
            {
                Debug.LogError("Cleanup Manager not found! Game objects may persist.");
            }
        }

        // 2. 載入 Game Over 場景
        // 錯誤寫法: SceneManagement.LoadSceneMode.Single
        // 正確寫法: LoadSceneMode.Single (因為上面已經有 using UnityEngine.SceneManagement; 了)
        NetworkManager.Singleton.SceneManager.LoadScene("GameOverScene", LoadSceneMode.Single);
    }
}