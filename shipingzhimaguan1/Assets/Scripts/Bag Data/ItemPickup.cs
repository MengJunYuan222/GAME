using UnityEngine;

/// <summary>
/// 物品拾取组件 - 背包系统唯一的物品拾取方式
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("物品数据")]
    [SerializeField] private ItemSO itemSO; // 直接引用ItemSO
    
    [Header("显示设置")]
    [SerializeField] private SpriteRenderer spriteRenderer; // 用于显示物品图标
    [SerializeField] private bool autoSetSprite = true; // 自动设置图标
    
    private bool isDisabled = false;

    private void Start()
    {
        // 自动查找或添加必要组件
        if (GetComponent<Collider>() == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }
        
        // 自动查找SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // 初始化显示
        if (itemSO != null && autoSetSprite && spriteRenderer != null)
        {
            spriteRenderer.sprite = itemSO.itemIcon;
        }
        
        // 检查物品数据是否存在
        if (itemSO == null)
        {
            Debug.LogError($"ItemPickup: {gameObject.name} 未设置物品数据，请设置itemSO");
        }
    }

    private void OnMouseDown()
    {
        if (isDisabled || itemSO == null) return;
        
        // 检查是否为结论物品
        if (itemSO.itemType == ItemType.结论)
        {
            // Debug.Log($"[ItemPickup] 拾取结论物品: {itemSO.itemName}");
            
            // 所有物品(包括结论)都添加到背包
            if (InventoryManager.Instance != null)
            {
                bool added = InventoryManager.Instance.AddItemByItemSO(itemSO);
                
                if (added)
                {
                    // Debug.Log($"[ItemPickup] 结论物品已添加到背包: {itemSO.itemName}");
                }
                else
                {
                    // Debug.LogWarning($"[ItemPickup] 背包已满，无法添加结论物品: {itemSO.itemName}");
                }
            }
            
            // 同时触发结论显示功能
            if (ReasoningManager.Instance != null)
            {
                // 触发结论显示
                ReasoningManager.Instance.OnReasoningSuccess?.Invoke(itemSO);
            }
            else
            {
                // Debug.LogWarning($"[ItemPickup] ReasoningManager实例不存在，无法显示结论: {itemSO.itemName}");
            }
            
            // 销毁物品
            Destroy(gameObject);
            return;
        }
        
        // 普通物品处理逻辑
        if (InventoryManager.Instance != null)
        {
            // 使用ItemSO添加物品
            bool added = InventoryManager.Instance.AddItemByItemSO(itemSO);
            
            if (added)
            {
                // Debug.Log($"[ItemPickup] 成功添加物品 {itemSO.itemName} 到背包");
                
                // 尝试更新推理系统显示
                UpdateReasoningSystem();
                
                // 物品成功添加到背包，禁用该物品
                Destroy(gameObject);
            }
            else
            {
                // Debug.Log($"背包已满，无法添加物品: {itemSO.itemName}");
            }
        }
        else
        {
            // Debug.LogError("ItemPickup: InventoryManager实例不存在！");
        }
    }
    
    // 更新推理系统显示
    private void UpdateReasoningSystem()
    {
        // 查找推理UI管理器
        ReasoningUIManager reasoningUIManager = FindObjectOfType<ReasoningUIManager>();
        
        // 如果找到UI管理器，调用它的刷新方法
        if (reasoningUIManager != null)
        {
            // Debug.Log("[ItemPickup] 强制更新推理系统物品显示");
            reasoningUIManager.RefreshItemDisplay();
        }
        else
        {
            // Debug.Log("[ItemPickup] 未找到ReasoningUIManager，跳过更新");
        }
    }
    
    // 设置物品并更新显示 - 提供给编辑器扩展使用
    public void SetItem(ItemSO newItemSO)
    {
        itemSO = newItemSO;
        
        // 如果有SpriteRenderer组件，更新显示
        if (spriteRenderer != null && newItemSO != null && autoSetSprite)
        {
            spriteRenderer.sprite = newItemSO.itemIcon;
        }
    }
    
    // 返回当前物品数据
    public ItemSO GetItemSO()
    {
        return itemSO;
    }
} 