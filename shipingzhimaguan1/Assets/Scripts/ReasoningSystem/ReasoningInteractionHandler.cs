using UnityEngine;
using System.Collections.Generic;

// 处理用户与推理系统的交互
public class ReasoningInteractionHandler : MonoBehaviour
{
    [SerializeField] private ItemDetailManager detailManager;
    [SerializeField] private ReasoningUIManager uiManager;
    [SerializeField] private InventoryManager inventoryManager;
    private ReasoningPanel reasoningPanel;  // 推理面板引用，运行时自动查找
    
    // 用于防止重复组合的记录
    private HashSet<string> triedCombinations = new HashSet<string>();
    
    // 用于处理当前交互状态
    private const float HOVER_THRESHOLD = 0.1f;
    
    private bool firstItemConfirmed = false;
    private ItemSO selectedItem1SO = null;
    private ItemSO selectedItem2SO = null;
    
    // 物品选择确认
    public void ConfirmItemSelection(Item item)
    {
        if (item == null) return;
        
        // 保存物品SO引用
        ItemSO itemSO = item.ItemData;
        
        // 第一个物品的处理
        if (!firstItemConfirmed)
        {
            selectedItem1SO = itemSO;
            firstItemConfirmed = true;
            
            // 显示详情
            detailManager.ShowItemDetail(item, false);
        }
        // 第二个物品的处理
        else
        {
            // 检查是否和第一个物品相同
            if (selectedItem1SO == itemSO)
            {
                // 相同物品，不处理
                return;
            }
            
            selectedItem2SO = itemSO;
            
            // 显示详情
            detailManager.ShowItemDetail(item, true);
            
            // 检查这个组合是否已经尝试过
            string combinationKey = GetCombinationKey(selectedItem1SO, selectedItem2SO);
            if (triedCombinations.Contains(combinationKey))
            {
                // 已经尝试过，但继续尝试 - 不提示错误
                // 因为有些组合可能需要多次尝试
            }
            
            // 记录这个组合
            triedCombinations.Add(combinationKey);
            
            // 尝试进行推理
            TryReasoning();
        }
    }
    
    // 取消物品选择
    public void CancelItemSelection()
    {
        // 重置选择状态
        ResetSelectionState();
    }
    
    // 清除组合记录
    public void ClearCombinationRecord(ItemSO item1, ItemSO item2)
    {
        if (item1 == null || item2 == null) return;
        
        string key = GetCombinationKey(item1, item2);
        triedCombinations.Remove(key);
    }
    
    // 清除所有组合记录
    public void ClearAllCombinationRecords()
    {
        triedCombinations.Clear();
    }
    
    // 重置选择状态
    public void ResetSelectionState()
    {
        // 重置状态变量
        firstItemConfirmed = false;
        selectedItem1SO = null;
        selectedItem2SO = null;
        
        // 更新面板显示
        detailManager.HideItemDetail(false);
        detailManager.HideItemDetail(true);
        
        // 清除所有高亮
        ClearAllHighlights();
    }
    
    // 清除所有高亮
    private void ClearAllHighlights()
    {
        // 查找所有ReasoningItem并清除高亮
        ReasoningItem[] items = FindObjectsOfType<ReasoningItem>();
        foreach (var item in items)
        {
            item.SetHighlight(false);
        }
    }
    
    // 获取组合键
    private string GetCombinationKey(ItemSO item1, ItemSO item2)
    {
        // 确保相同的两个物品无论顺序如何都得到相同的键
        if (item1 == null || item2 == null) return string.Empty;
        
        // 使用物品ID生成唯一的组合键
        int id1 = item1.itemID;
        int id2 = item2.itemID;
        
        // 确保键顺序一致（较小的ID始终在前）
        if (id1 > id2)
        {
            return $"{id2}-{id1}";
        }
        else
        {
            return $"{id1}-{id2}";
        }
    }
    
    // 是否已确认第一个物品
    public bool IsFirstItemConfirmed()
    {
        return firstItemConfirmed;
    }
    
    // 初始化方法
    public void Initialize(ItemDetailManager detailMgr)
    {
        detailManager = detailMgr;
        
        // 查找推理面板引用
        if (reasoningPanel == null)
        {
            reasoningPanel = FindObjectOfType<ReasoningPanel>();
        }
    }
    
    // 尝试进行推理
    private void TryReasoning()
    {
        if (selectedItem1SO == null || selectedItem2SO == null)
            return;
            
        // 检查ReasoningManager是否存在
        if (ReasoningManager.Instance == null)
        {
            Debug.LogError("[ReasoningInteractionHandler] 找不到ReasoningManager实例！");
            return;
        }
        
        // 尝试推理
        if (ReasoningManager.Instance.TryReasoning(selectedItem1SO, selectedItem2SO, out ItemSO resultItem))
        {
            // 检查结果物品的类型
            if (resultItem.itemType != ItemType.结论)
            {
                // 仅当结果物品不是结论类型时，才添加到背包
                if (inventoryManager != null)
                {
                    inventoryManager.AddItemByItemSO(resultItem, false); // 第二个参数false表示不显示提示
                }
            }
            // 注意：结论类型的物品不添加到任何背包，只在结论展示面板上显示
                
            // 重置选择状态
            ResetSelectionState();
        }
        else
        {
            // 推理失败，不重置选择状态
            // 允许用户尝试其他组合
        }
    }
}