using UnityEngine;
using Unity.Netcode;

public class TutorialUISync : NetworkBehaviour
{
    [Header("請依序拖入教學頁面 (Page1, Page2, Page3...)")]
    public GameObject[] tutorialPages;

    // 現在顯示第幾頁 (NetworkVariable 自動同步數值)
    private NetworkVariable<int> currentPageIndex = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        // 當變數改變時，所有人都更新畫面
        currentPageIndex.OnValueChanged += OnPageChanged;

        // 初始化：強制更新一次目前的頁面
        UpdatePageVisuals(currentPageIndex.Value);
    }

    private void OnPageChanged(int oldIndex, int newIndex)
    {
        UpdatePageVisuals(newIndex);
    }

    private void UpdatePageVisuals(int index)
    {
        // 迴圈檢查每一頁，只有對應 index 的頁面打開，其他全關
        for (int i = 0; i < tutorialPages.Length; i++)
        {
            if (tutorialPages[i] != null)
                tutorialPages[i].SetActive(i == index);
        }
    }

    // --- 按鈕功能 (綁定到 Button OnClick) ---

    public void OnClick_NextPage()
    {
        // 發送請求給 Server
        RequestNextPageServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNextPageServerRpc()
    {
        // 只有 Server 能修改 NetworkVariable
        // 限制不要超過頁數
        if (currentPageIndex.Value < tutorialPages.Length - 1)
        {
            currentPageIndex.Value++;
        }
    }
}