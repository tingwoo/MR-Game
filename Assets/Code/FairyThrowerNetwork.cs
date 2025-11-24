using System;
using UnityEngine;
using Unity.Netcode;

public class FairyThrowerNetwork : NetworkBehaviour
{
    public enum SpawnOriginMode { UseTransform, MidpointOfTwo, VolumeBox }
    public enum DirectionMode { UseForwardOfRef, AveragePlayersForward, TowardsPlayersCentroid, RandomConeAroundRef }

    [Header("Spawn Origin")]
    public SpawnOriginMode spawnOriginMode = SpawnOriginMode.VolumeBox;
    public Transform origin;
    
    // 預留給未來擴充玩家追蹤用
    public Transform playerA; 
    public Transform playerB;

    [Tooltip("請將 CenterEyeAnchor 底下的 Volume 拉到這裡 (務必勾選 Is Trigger)")]
    public BoxCollider spawnVolume;

    [Header("Direction")]
    public DirectionMode directionMode = DirectionMode.RandomConeAroundRef;
    [Tooltip("請拉入 ThrowDirectionRef，藍色軸向(Z)即為發射中心方向")]
    public Transform forwardRef; 

    [Range(0f, 45f)]
    public float angleDeg = 12f;

    [Header("Speed / Spin")]
    public Vector2 speedRange = new Vector2(1.5f, 3f);
    public Vector2 randomAngularVelRangeDeg = new Vector2(0f, 360f);

    [Header("Size (Scale)")]
    [Tooltip("隨機大小範圍：X為最小值，Y為最大值 (例如 0.5 ~ 1.5)")]
    public Vector2 scaleRange = new Vector2(1f, 1f); 

    [Header("Weighted Prefabs")]
    public WeightedPrefab[] weightedPrefabs = new WeightedPrefab[6];

    [Header("Auto Spawn")]
    public bool autoSpawn = false;
    [Min(0f)] public float spawnsPerSecond = 0.5f;

    [Header("Debug / Keys")]
    public bool drawGizmos = true;
    public KeyCode throwKey = KeyCode.Space;

    private float _accum;

    // Update 只在 Server 端執行邏輯
    void Update()
    {
        // 1. 只有 Server (Host) 有權力生成物件
        if (!IsServer) return;

        if (Input.GetKeyDown(throwKey)) ThrowOne();

        if (autoSpawn && spawnsPerSecond > 0f)
        {
            _accum += Time.deltaTime * spawnsPerSecond;
            while (_accum >= 1f) { _accum -= 1f; ThrowOne(); }
        }
    }
    
    public void ThrowOne()
    {
        var pf = PickPrefabByWeight();
        if (pf == null) return;

        Vector3 pos = GetSpawnPosition();
        Vector3 dir = GetDirection(pos);
        float v = UnityEngine.Random.Range(speedRange.x, speedRange.y);

        // 2. 生成物件 (Server 本地)
        var go = Instantiate(pf, pos, Quaternion.LookRotation(dir)); // 順便設定面向
        
        // 設定隨機大小
        float randomScale = UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
        go.transform.localScale = Vector3.one * randomScale;

        var netObj = go.GetComponent<NetworkObject>();

        // =================================================================
        // 【關鍵修正 1】先 Spawn，建立連線與 NetworkRigidbody 的關聯
        // =================================================================
        if (netObj != null)
        {
            netObj.Spawn(); 
        }

        // 3. 設定物理屬性
        var rb = go.GetComponent<Rigidbody>();
        if(rb == null) rb = go.AddComponent<Rigidbody>();
        
        // Server 強制設定物理參數 (確保 Prefab 設定錯誤時也能修正)
        rb.useGravity = false;
        rb.drag = 0f; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = false; // 確保不是 Kinematic

        // =================================================================
        // 【關鍵修正 2】在 Spawn 之後給予速度，NetworkRigidbody 才能同步給 Client
        // =================================================================
        rb.velocity = dir * v;

        if (randomAngularVelRangeDeg.y > 0f)
        {
            Vector3 axis = UnityEngine.Random.onUnitSphere;
            float w = UnityEngine.Random.Range(randomAngularVelRangeDeg.x, randomAngularVelRangeDeg.y);
            rb.angularVelocity = axis * Mathf.Deg2Rad * w;
        }
    }
    // 當這個物件在網路上成功生成(連線成功)時，會自動執行一次
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 如果我是 Server (Host)，我一進遊戲就先丟一隻，不用等計時器
        if (IsServer) 
        {
            ThrowOne();
        }
    }
    Vector3 GetSpawnPosition()
    {
        if (spawnOriginMode == SpawnOriginMode.VolumeBox && spawnVolume != null)
        {
            // 修正：使用 TransformPoint 確保支援 Volume 的旋轉與縮放
            Vector3 c = spawnVolume.center;
            Vector3 e = spawnVolume.size * 0.5f;
            Vector3 local = new Vector3(
                UnityEngine.Random.Range(-e.x, e.x),
                UnityEngine.Random.Range(-e.y, e.y),
                UnityEngine.Random.Range(-e.z, e.z)
            ) + c;
            return spawnVolume.transform.TransformPoint(local);
        }
        return transform.position;
    }

    Vector3 GetDirection(Vector3 spawnPos)
    {
        // 如果沒有拉 forwardRef，就用程式碼掛載物件的 forward
        Vector3 fwdRef = (forwardRef != null ? forwardRef.forward : transform.forward);

        if (directionMode == DirectionMode.RandomConeAroundRef)
             return RandomDirectionInCone(fwdRef, angleDeg);
        
        return fwdRef.normalized;
    }

    GameObject PickPrefabByWeight()
    {
        float sum = 0f;
        foreach (var e in weightedPrefabs) if (e?.prefab != null) sum += e.weight;
        
        float r = UnityEngine.Random.Range(0, sum);
        float acc = 0f;
        
        foreach (var e in weightedPrefabs)
        {
            if (e?.prefab == null) continue;
            acc += e.weight;
            if (r <= acc) return e.prefab;
        }
        return weightedPrefabs.Length > 0 ? weightedPrefabs[0].prefab : null;
    }
    
    // =================================================================
    // 【關鍵修正 3】正確的圓錐散射數學計算
    // =================================================================
    public static Vector3 RandomDirectionInCone(Vector3 forward, float angleDeg)
    {
        if (angleDeg <= 0) return forward.normalized;

        // 1. 在圓錐角度內隨機取一個偏角
        float deviation = UnityEngine.Random.Range(0f, angleDeg);
        
        // 2. 在 0-360 度隨機取一個旋轉角 (像輪盤一樣)
        float roll = UnityEngine.Random.Range(0f, 360f);

        // 3. 數學計算：
        // 先建立一個「面向正前方」的旋轉
        Quaternion lookRot = Quaternion.LookRotation(forward.normalized);
        
        // 產生偏移：
        // Quaternion.AngleAxis(roll, Vector3.forward) -> 繞著軸心轉 (滾轉)
        // Quaternion.AngleAxis(deviation, Vector3.up) -> 往旁邊偏 (俯仰/偏航)
        Quaternion randomRot = Quaternion.AngleAxis(roll, Vector3.forward) * Quaternion.AngleAxis(deviation, Vector3.up);

        // 組合：基準方向 * 隨機偏移 * 原始 Z 軸
        return lookRot * randomRot * Vector3.forward;
    }

    [Serializable]
    public class WeightedPrefab
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // 1. 畫出生成框 (黃色)
        Gizmos.color = Color.yellow;
        if (spawnVolume != null)
        {
            Gizmos.matrix = spawnVolume.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(spawnVolume.center, spawnVolume.size);
            Gizmos.matrix = Matrix4x4.identity; // 復原矩陣
        }

        // 2. 畫出發射方向參考線 (紅色) - 幫助你確認 ThrowDirectionRef 到底指哪裡
        Gizmos.color = Color.red;
        Vector3 startPos = (spawnVolume != null) ? spawnVolume.bounds.center : transform.position;
        Vector3 dir = (forwardRef != null) ? forwardRef.forward : transform.forward;
        
        Gizmos.DrawRay(startPos, dir * 2.0f); // 畫一條 2 公尺長的線
        
        // 畫個小球在箭頭末端方便看
        Gizmos.DrawSphere(startPos + dir * 2.0f, 0.05f);
    }
}