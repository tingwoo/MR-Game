using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(FairyThrowerNetwork))]
public class FairyDifficultyController : NetworkBehaviour
{
    [Header("Target Settings")]
    [Tooltip("如果不拉，會自動抓取同物件上的腳本")]
    public FairyThrowerNetwork thrower;

    [Header("Difficulty Progression")]
    [Tooltip("遊戲從開始到達到「最大難度」需要幾秒？")]
    public float timeToMaxDifficulty = 60f; // 例如 60 秒後達到最難

    [Header("Spawn Rate (Frequency)")]
    public float startSpawnRate = 0.5f; // 一開始每秒 0.5 隻
    public float maxSpawnRate = 2.0f;   // 最難時每秒 2.0 隻

    [Header("Speed Settings")]
    public Vector2 startSpeedRange = new Vector2(1.5f, 3f); // 一開始的速度範圍
    public Vector2 maxSpeedRange = new Vector2(4f, 6f);     // 最難時的速度範圍

    // 內部計時器
    private float _gameTimer = 0f;

    public override void OnNetworkSpawn()
    {
        // 確保只有 Server (Host) 負責計算難度，Client 不需要執行這段
        if (!IsServer)
        {
            this.enabled = false;
            return;
        }

        if (thrower == null)
        {
            thrower = GetComponent<FairyThrowerNetwork>();
        }

        // 初始化數值
        ApplyDifficulty(0f);
    }
    [Header("Mode Settings")]
    public bool usePingPongMode = true; // 勾選就是乒乓，不勾就是鎖死最難
void Update()
    {
        // 只有 Server 執行
        if (!IsServer) return;
        if (thrower == null) return;

        // 1. 累加時間
        _gameTimer += Time.deltaTime;

        // 2. 計算進度 (PingPong 模式)
        // 原本是用 Mathf.Clamp01 (鎖死在1)，現在改用 Mathf.PingPong
        // 參數說明：
        // 第一個參數是「目前的總進度」(時間 / 到達頂峰所需時間)
        // 第二個參數是「頂峰值」(也就是 1.0)
        
        float progress = Mathf.PingPong(_gameTimer / timeToMaxDifficulty, 1f);

        // 3. 應用難度
        ApplyDifficulty(progress);
    }
        void ApplyDifficulty(float t)
    {
        // --- 線性調整生成頻率 ---
        // Lerp (插值) 會根據 t (0~1) 在最小值和最大值之間滑動
        thrower.spawnsPerSecond = Mathf.Lerp(startSpawnRate, maxSpawnRate, t);

        // --- 線性調整速度範圍 ---
        // Vector2.Lerp 可以同時對 X (最小速) 和 Y (最大速) 做插值
        thrower.speedRange = Vector2.Lerp(startSpeedRange, maxSpeedRange, t);
    }
    
    // 讓你可以從外部重置難度 (例如重新開始遊戲時)
    public void ResetDifficulty()
    {
        _gameTimer = 0f;
        ApplyDifficulty(0f);
    }
}