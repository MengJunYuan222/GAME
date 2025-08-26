using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ReasoningUIManager : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private ItemDetailManager itemDetailManager;
    [SerializeField] private ConclusionDisplayManager conclusionManager;
    [SerializeField] private ReasoningInteractionHandler interactionHandler;
    [SerializeField] private SuspectDisplayManager suspectManager;
    [SerializeField] private UIAnimationController animationController;
    
    [Header("面板引用")]
    [SerializeField] private GameObject backgroundPanel;    // 背景面板
    [SerializeField] private GameObject itemPanel;          // 物品面板
    
    [Header("背包引用")]
    [SerializeField] private InventoryManager inventoryManager;  // 背包管理器引用
    
    private ReasoningPanel reasoningPanel;                    // 推理面板引用
    
    private void Awake()
    {
        // 获取ReasoningPanel引用
        reasoningPanel = GetComponent<ReasoningPanel>();
        if (reasoningPanel == null)
        {
            reasoningPanel = FindObjectOfType<ReasoningPanel>();
        }
        
        // 获取InventoryManager引用
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
        }
        
        // 检查组件引用
        ValidateComponents();
    }
    
    private void Start()
    {
        // 订阅推理系统事件
        if (ReasoningManager.Instance != null)
        {
            ReasoningManager.Instance.OnReasoningSuccess += OnReasoningSuccess;
            ReasoningManager.Instance.OnReasoningFail += OnReasoningFail;
        }
        
        // 订阅背包变化事件
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged += OnInventoryChanged;
            
            // 初始显示物品
            RefreshItemDisplay();
        }
        
        // 主动设置所有ReasoningItem的UIManager引用
        StartCoroutine(SetupAllReasoningItems());
    }
    
    // 验证组件引用
    private void ValidateComponents()
    {
        // 检查并自动查找组件
        if (itemDetailManager == null)
        {
            itemDetailManager = GetComponent<ItemDetailManager>();
            if (itemDetailManager == null)
            {
                itemDetailManager = gameObject.AddComponent<ItemDetailManager>();
                Debug.LogWarning("[ReasoningUIManager] 自动添加了ItemDetailManager组件");
            }
        }
        
        if (conclusionManager == null)
        {
            conclusionManager = GetComponent<ConclusionDisplayManager>();
            if (conclusionManager == null)
            {
                conclusionManager = gameObject.AddComponent<ConclusionDisplayManager>();
                Debug.LogWarning("[ReasoningUIManager] 自动添加了ConclusionDisplayManager组件");
            }
        }
        
        if (interactionHandler == null)
        {
            interactionHandler = GetComponent<ReasoningInteractionHandler>();
            if (interactionHandler == null)
            {
                interactionHandler = gameObject.AddComponent<ReasoningInteractionHandler>();
                Debug.LogWarning("[ReasoningUIManager] 自动添加了ReasoningInteractionHandler组件");
            }
        }
        
        if (suspectManager == null)
        {
            suspectManager = GetComponent<SuspectDisplayManager>();
            if (suspectManager == null)
            {
                suspectManager = gameObject.AddComponent<SuspectDisplayManager>();
                Debug.LogWarning("[ReasoningUIManager] 自动添加了SuspectDisplayManager组件");
            }
        }
        
        if (animationController == null)
        {
            animationController = GetComponent<UIAnimationController>();
            if (animationController == null)
            {
                animationController = gameObject.AddComponent<UIAnimationController>();
                Debug.LogWarning("[ReasoningUIManager] 自动添加了UIAnimationController组件");
            }
        }
        
        // 初始化引用
        if (interactionHandler != null)
        {
            interactionHandler.Initialize(itemDetailManager);
        }
    }
    
    // 设置所有ReasoningItem的UIManager引用
    private IEnumerator SetupAllReasoningItems()
    {
        // 等待一帧，确保所有对象都已初始化
        yield return null;
        
        // 查找所有ReasoningItem
        ReasoningItem[] items = FindObjectsOfType<ReasoningItem>();
        foreach (var item in items)
        {
            // 设置UIManager引用
            item.SetUIManager(this);
            
            // 重置事件监听
            item.ResetItemSelectedEvent();
            
            // 添加选择事件监听
            item.OnItemSelected += OnItemSelected;
        }
    }
    
    // 当背包内容变化时
    private void OnInventoryChanged()
    {
        RefreshItemDisplay();
    }
    
    // 刷新物品显示
    public void RefreshItemDisplay()
    {
        if (itemPanel == null || inventoryManager == null) return;
        
        // 延迟刷新，等待背包内容更新
        StartCoroutine(DelayedRefresh());
    }
    
    // 延迟刷新协程
    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(0.1f);
        
        // 获取背包中所有物品
        List<Item> inventoryItems = GetAllInventoryItems();
        
        // 找到物品面板下所有的ReasoningItem
        ReasoningItem[] reasoningItems = itemPanel.GetComponentsInChildren<ReasoningItem>(true);
        
        // 更新物品显示
        for (int i = 0; i < reasoningItems.Length; i++)
        {
            if (i < inventoryItems.Count)
            {
                // 有物品可显示
                reasoningItems[i].SetItem(inventoryItems[i]);
            }
            else
            {
                // 无物品可显示，清空
                reasoningItems[i].SetItem(null);
            }
        }
    }
    
    // 获取所有背包物品
    public List<Item> GetAllInventoryItems()
    {
        if (inventoryManager == null) return new List<Item>();
        
        return inventoryManager.GetAllItems();
    }
    
    // 当物品被选中时
    private void OnItemSelected(Item item)
    {
        if (item == null) return;
        
        interactionHandler.ConfirmItemSelection(item);
    }
    
    // 当推理成功时
    private void OnReasoningSuccess(ItemSO resultItem)
    {
        if (resultItem == null) return;
        
        // 显示结论
        conclusionManager.ShowConclusion(resultItem);
    }
    
    // 当推理失败时
    private void OnReasoningFail(string message)
    {
        // 播放失败动画
        animationController.PlayFailureAnimation(message);
    }
    
    // 提供外部访问方法
    
    // 检查是否已确认第一个物品
    public bool IsFirstItemConfirmed()
    {
        return interactionHandler.IsFirstItemConfirmed();
    }
    
    // 获取结论面板
    public GameObject GetConclusionResultPanel()
    {
        return conclusionManager.GetConclusionPanel();
    }
    
    // 确认物品选择
    public void ConfirmItemSelection(Item item)
    {
        interactionHandler.ConfirmItemSelection(item);
    }
    
    // 取消物品选择
    public void CancelItemSelection()
    {
        interactionHandler.CancelItemSelection();
    }
    
    // 显示物品详情
    public void ShowItemDetail(Item item)
    {
        itemDetailManager.ShowItemDetail(item, interactionHandler.IsFirstItemConfirmed());
    }
    
    // 隐藏物品详情
    public void HideItemDetail(bool isSecondPanel)
    {
        itemDetailManager.HideItemDetail(isSecondPanel);
    }
    
    // 点击印章结论
    public void OnStampedConclusionClick(GameObject stamp)
    {
        conclusionManager.OnStampedConclusionClick(stamp);
    }
    
    // 确认按钮点击
    public void OnConfirmButtonClick()
    {
        conclusionManager.OnConfirmButtonClick();
    }
    
    // 检查结论是否已确认
    public bool IsConfirmedConclusion(ItemSO conclusion)
    {
        return conclusionManager.IsConfirmedConclusion(conclusion);
    }
    
    // 获取背包管理器
    public InventoryManager GetInventoryManager()
    {
        return inventoryManager;
    }
    
    // 显示结论
    public void ShowConclusion(ItemSO resultItem)
    {
        conclusionManager.ShowConclusion(resultItem);
    }
    
    // 结论悬停
    public void OnConclusionItemHover(ConclusionItem item)
    {
        // 此处可以添加悬停效果，如果需要
    }
    
    // 结论离开
    public void OnConclusionItemExit(ConclusionItem item)
    {
        // 此处可以添加离开效果，如果需要
    }
    
    private void OnDestroy()
    {
        // 取消事件订阅
        if (ReasoningManager.Instance != null)
        {
            ReasoningManager.Instance.OnReasoningSuccess -= OnReasoningSuccess;
            ReasoningManager.Instance.OnReasoningFail -= OnReasoningFail;
        }
        
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= OnInventoryChanged;
        }
    }
}