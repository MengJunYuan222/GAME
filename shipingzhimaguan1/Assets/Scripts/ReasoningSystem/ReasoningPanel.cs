using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ReasoningPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject reasoningPanelUI;     // 推理面板UI

    [Header("合成系统")]
    [SerializeField] private ReasoningSlot materialSlot1;     // 材料槽1
    [SerializeField] private ReasoningSlot materialSlot2;     // 材料槽2
    
    // UI组件引用
    
    private ReasoningUIManager uiManager;
    private InventoryManager inventoryManager;
    
    private void Start()
    {
        // 检查必要引用
        bool referencesReady = true;
        
        if (reasoningPanelUI == null)
        {
            Debug.LogError("面板引用为空！");
            referencesReady = false;
        }
        
        if (materialSlot1 == null || materialSlot2 == null)
        {
            Debug.LogWarning("合成槽位引用不完整，合成功能可能无法正常工作");
        }
        
        // 获取UI管理器引用
        uiManager = FindObjectOfType<ReasoningUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("找不到ReasoningUIManager！");
            referencesReady = false;
        }
        else
        {
            // 通过UI管理器获取背包管理器
            inventoryManager = uiManager.GetInventoryManager();
            if (inventoryManager == null)
            {
                Debug.LogError("找不到InventoryManager！");
                referencesReady = false;
            }
        }

        // 如果所有引用都准备好了，初始化UI
        if (referencesReady)
        {
            InitializeUI();
        }
        else
        {
            Debug.LogError("引用检查失败，无法初始化UI");
        }
        
        // 确保面板初始状态为隐藏
        if (reasoningPanelUI != null)
        {
            reasoningPanelUI.SetActive(false);
        }
    }

    private void InitializeUI()
    {
        // 初始化合成槽位事件
        if (materialSlot1 != null)
        {
            // 确保清除之前的事件，避免重复订阅
            materialSlot1.ResetEvents();
            
            // 更新材料槽1显示
            materialSlot1.UpdateUI();
            
            materialSlot1.OnItemDropped += (item) => {
                // 当放入物品时，检查是否可以合成
                CheckAndAutoSynthesize();
            };
        }
        else
        {
            Debug.LogError("[ReasoningPanel] 材料槽1引用为空！");
        }
        
        if (materialSlot2 != null)
        {
            // 确保清除之前的事件，避免重复订阅
            materialSlot2.ResetEvents();
            
            // 更新材料槽2显示
            materialSlot2.UpdateUI();
            
            materialSlot2.OnItemDropped += (item) => {
                // 当放入物品时，检查是否可以合成
                CheckAndAutoSynthesize();
            };
        }
        else
        {
            Debug.LogError("[ReasoningPanel] 材料槽2引用为空！");
        }
        
        // 确保所有槽位事件设置完成
        EnsureAllSlotEventsSetup();
    }
    
    // 选中一个物品
    public void SelectItem(Item item, ReasoningItem reasoningItem)
    {
        // 如果槽位1为空，优先放入槽位1
        if (materialSlot1 != null && materialSlot1.IsEmpty()) 
        {
            materialSlot1.SetItem(item);
            
            // 检查是否可以自动合成
            CheckAndAutoSynthesize();
            return;
        }
        
        // 如果槽位2为空，放入槽位2
        if (materialSlot2 != null && materialSlot2.IsEmpty())
                {
            materialSlot2.SetItem(item);
            
            // 检查是否可以自动合成
        CheckAndAutoSynthesize();
            return;
        }
        
        // 如果两个槽位都不为空，提示玩家需要先清空槽位
        // 所有槽位已满，请先清空一个槽位
    }

    // 自动检查并尝试合成
    private void CheckAndAutoSynthesize()
    {
        // 检查必要引用
        if (materialSlot1 == null || materialSlot2 == null)
        {
            Debug.LogError("[ReasoningPanel] 合成所需槽位引用不完整！");
            return;
        }
        
        // 检查材料槽是否都有物品
        if (materialSlot1.IsEmpty() || materialSlot2.IsEmpty())
        {
            return;
        }

        // 获取两个物品
        Item item1 = materialSlot1.GetItem();
        Item item2 = materialSlot2.GetItem();
        
        // 检查物品是否为空
        if (item1 == null || item2 == null)
        {
            Debug.LogError("[ReasoningPanel] 获取到的物品为空！");
            return;
        }
        
        // 保存原始索引，用于合成成功后从背包中移除物品
        int originalIndex1 = materialSlot1.IsClonedItem() ? materialSlot1.GetOriginalItemIndex() : -1;
        int originalIndex2 = materialSlot2.IsClonedItem() ? materialSlot2.GetOriginalItemIndex() : -1;
        
        // 检查ReasoningManager是否存在
        if (ReasoningManager.Instance == null)
        {
            Debug.LogError("[ReasoningPanel] 找不到ReasoningManager实例！");
            return;
        }

        // 尝试推理
        if (ReasoningManager.Instance.TryReasoning(item1.ItemData, item2.ItemData, out ItemSO resultItem))
        {
            // 将结果物品添加到背包，但不显示提示
            if (inventoryManager != null)
            {
                inventoryManager.AddItemByItemSO(resultItem, false); // 禁用提示显示
                }
                
            // 调用UI管理器的印章功能
            if (uiManager != null)
                    {
                uiManager.ShowConclusion(resultItem);
            }
            
            // 清空材料槽
            materialSlot1.ClearSlot();
            materialSlot2.ClearSlot();
        }
        else
        {
            // 合成失败，但不清空槽位，保留物品
            // 合成失败，两个物品无法合成
        }
    }

    // 切换面板显示状态
    public void TogglePanel()
    {
        if (reasoningPanelUI == null) return;
        
        if (reasoningPanelUI.activeSelf)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }
    
    // 打开推理面板
    public void OpenPanel()
    {
        if (reasoningPanelUI != null && !reasoningPanelUI.activeSelf)
        {
            // 重置所有槽位
            ResetAllSlots();
            
            // 确保所有槽位事件正确设置
            EnsureAllSlotEventsSetup();
            
            reasoningPanelUI.SetActive(true);
            
            // 订阅背包变化事件
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryChanged += OnInventoryChanged;
            }
        }
    }
    
    // 确保所有槽位事件正确设置
    private void EnsureAllSlotEventsSetup()
    {
        // 设置合成槽位事件
        if (materialSlot1 != null)
        {
            // 先重置所有事件，避免重复订阅
            materialSlot1.ResetEvents();
            
            // 更新UI
            materialSlot1.UpdateUI();
            
            // 添加物品放入事件
            materialSlot1.OnItemDropped += (item) => {
                CheckAndAutoSynthesize();
            };
        }
        
        if (materialSlot2 != null)
        {
            // 先重置所有事件，避免重复订阅
            materialSlot2.ResetEvents();
            
            // 更新UI
            materialSlot2.UpdateUI();
            
            // 添加物品放入事件
            materialSlot2.OnItemDropped += (item) => {
                CheckAndAutoSynthesize();
            };
        }
    }

    // 关闭推理面板
    public void ClosePanel()
    {
        if (reasoningPanelUI != null && reasoningPanelUI.activeSelf)
        {
            reasoningPanelUI.SetActive(false);
            
            // 取消订阅背包变化事件
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryChanged -= OnInventoryChanged;
            }
            
            // 重置所有槽位
            ResetAllSlots();
        }
    }

    // 当背包内容变化时刷新显示
    private void OnInventoryChanged()
    {
        // 不需要做任何事情，简化处理
    }

    // 重置所有槽位
    private void ResetAllSlots()
    {
        if (materialSlot1 != null)
            {
            materialSlot1.ClearSlot();
        }
        
        if (materialSlot2 != null)
            {
            materialSlot2.ClearSlot();
        }
    }
} 