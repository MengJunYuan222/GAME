using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }
    
    [Header("物品定义")]
    [SerializeField] private List<ItemSO> allItems = new List<ItemSO>();
    
    // 物品字典，用于快速查找
    private Dictionary<int, ItemSO> itemsById = new Dictionary<int, ItemSO>();
    private Dictionary<string, ItemSO> itemsByName = new Dictionary<string, ItemSO>();
    private Dictionary<ItemType, List<ItemSO>> itemsByType = new Dictionary<ItemType, List<ItemSO>>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeDatabase()
    {
        itemsById.Clear();
        itemsByName.Clear();
        itemsByType.Clear();
        
        foreach (ItemSO item in allItems)
        {
            if (item != null)
            {
                // 检查ID是否重复
                if (itemsById.ContainsKey(item.itemID))
                {
                    Debug.LogError($"物品数据库中存在重复ID: {item.itemID}，物品名称: {item.itemName}");
                    continue;
                }
                
                // 添加到ID索引
                itemsById[item.itemID] = item;
                
                // 添加到名称索引
                if (!string.IsNullOrEmpty(item.itemName))
                {
                    itemsByName[item.itemName] = item;
                }
                
                // 添加到类型索引
                if (!itemsByType.ContainsKey(item.itemType))
                {
                    itemsByType[item.itemType] = new List<ItemSO>();
                }
                itemsByType[item.itemType].Add(item);
            }
        }
        
        Debug.Log($"物品数据库初始化完成，共加载 {allItems.Count} 个物品");
    }
    
    // 通过ID获取物品
    public ItemSO GetItemByID(int id)
    {
        if (itemsById.TryGetValue(id, out ItemSO item))
        {
            return item;
        }
        
        Debug.LogWarning($"找不到ID为 {id} 的物品");
        return null;
    }
    
    // 通过名称获取物品
    public ItemSO GetItemByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("尝试使用空名称查找物品");
            return null;
        }
        
        if (itemsByName.TryGetValue(name, out ItemSO item))
        {
            return item;
        }
        
        Debug.LogWarning($"找不到名称为 {name} 的物品");
        return null;
    }
    
    // 获取特定类型的所有物品
    public List<ItemSO> GetItemsByType(ItemType type)
    {
        if (itemsByType.TryGetValue(type, out List<ItemSO> typeItems))
        {
            return new List<ItemSO>(typeItems);
        }
        
        return new List<ItemSO>();
    }
    
    // 获取所有物品
    public List<ItemSO> GetAllItems()
    {
        return new List<ItemSO>(allItems);
    }
    
    // 添加物品到数据库（运行时）
    public void AddItem(ItemSO item)
    {
        if (item == null) return;
        
        if (!allItems.Contains(item))
        {
            allItems.Add(item);
        }
        
        // 更新ID索引
        itemsById[item.itemID] = item;
        
        // 更新名称索引
        if (!string.IsNullOrEmpty(item.itemName))
        {
            itemsByName[item.itemName] = item;
        }
        
        // 更新类型索引
        if (!itemsByType.ContainsKey(item.itemType))
        {
            itemsByType[item.itemType] = new List<ItemSO>();
        }
        
        // 避免重复添加
        if (!itemsByType[item.itemType].Contains(item))
        {
            itemsByType[item.itemType].Add(item);
        }
    }
    
    // 模糊搜索物品
    public List<ItemSO> SearchItems(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new List<ItemSO>(allItems);
        }
        
        searchTerm = searchTerm.ToLower();
        
        return allItems
            .Where(item => item != null && 
                  (item.itemName.ToLower().Contains(searchTerm) || 
                   item.itemDescription.ToLower().Contains(searchTerm)))
            .ToList();
    }
} 