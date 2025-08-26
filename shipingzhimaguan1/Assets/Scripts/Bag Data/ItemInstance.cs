using UnityEngine;

/// <summary>
/// 轻量级物品实例类，用于在不需要MonoBehaviour的情况下表示物品
/// </summary>
public class ItemInstance
{
    // 物品数据引用
    private ItemSO itemData;
    
    // 实例特有数据
    private int stackCount = 1;
    
    // 属性
    public string ItemName => itemData != null ? itemData.itemName : "";
    public int ItemID => itemData != null ? itemData.itemID : -1;
    public string ItemDescription => itemData != null ? itemData.itemDescription : "";
    public Sprite ItemIcon => itemData != null ? itemData.itemIcon : null;
    public ItemType ItemType => itemData != null ? itemData.itemType : ItemType.物品;
    public ItemSO ItemData => itemData;
    
    // 堆叠数量属性
    public int StackCount 
    { 
        get => stackCount; 
        set => stackCount = Mathf.Max(1, value); // 确保最小为1
    }
    
    // 构造函数
    public ItemInstance(ItemSO itemSO, int count = 1)
    {
        itemData = itemSO;
        stackCount = Mathf.Max(1, count);
    }
    
    // 增加堆叠数量
    public void AddStack(int amount = 1)
    {
        stackCount = Mathf.Max(1, stackCount + amount);
    }
    
    // 减少堆叠数量，返回是否还有剩余
    public bool RemoveStack(int amount = 1)
    {
        stackCount -= amount;
        if (stackCount <= 0)
        {
            stackCount = 0;
            return false; // 返回false表示没有剩余
        }
        return true; // 返回true表示还有剩余
    }
    
    // 克隆方法
    public ItemInstance Clone()
    {
        return new ItemInstance(itemData, stackCount);
    }
    
    // 获取描述文本（包含堆叠数量）
    public string GetFullDescription()
    {
        if (itemData == null) return "";
        
        if (stackCount > 1)
        {
            return $"数量: {stackCount}\n{itemData.itemDescription}";
        }
        
        return itemData.itemDescription;
    }
} 