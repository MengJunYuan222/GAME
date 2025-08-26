using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品对象池管理器 - 负责管理物品GameObject的创建和回收
/// </summary>
public class ItemPoolManager : MonoBehaviour
{
    public static ItemPoolManager Instance { get; private set; }
    
    [Header("对象池设置")]
    [SerializeField] private int initialPoolSize = 20; // 初始池大小
    [SerializeField] private int maxPoolSize = 100; // 最大池大小
    [SerializeField] private Transform poolParent; // 对象池父物体
    
    // 对象池字典 - 按物品ID分类存储
    private Dictionary<int, Queue<Item>> itemPools = new Dictionary<int, Queue<Item>>();
    
    // 通用对象池 - 存储未分类的物品
    private Queue<Item> genericPool = new Queue<Item>();
    
    // 当前活跃的物品对象
    private List<Item> activeItems = new List<Item>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePool()
    {
        // 设置对象池父物体
        if (poolParent == null)
        {
            GameObject poolContainer = new GameObject("ItemPool");
            poolContainer.transform.SetParent(transform);
            poolParent = poolContainer.transform;
        }
        
        // 预先创建一些通用物品对象
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateGenericItem();
        }
        
        Debug.Log($"物品对象池初始化完成，通用池大小: {genericPool.Count}");
    }
    
    // 创建一个通用物品对象
    private Item CreateGenericItem()
    {
        GameObject itemObj = new GameObject("PooledItem");
        itemObj.SetActive(false);
        itemObj.transform.SetParent(poolParent);
        
        Item itemComponent = itemObj.AddComponent<Item>();
        genericPool.Enqueue(itemComponent);
        
        return itemComponent;
    }
    
    // 获取物品对象
    public Item GetItem(ItemSO itemSO)
    {
        if (itemSO == null) return null;
        
        int itemId = itemSO.itemID;
        Item item = null;
        
        // 检查是否有该物品ID的专用池
        if (itemPools.TryGetValue(itemId, out Queue<Item> pool) && pool.Count > 0)
        {
            // 从专用池获取
            item = pool.Dequeue();
        }
        else if (genericPool.Count > 0)
        {
            // 从通用池获取
            item = genericPool.Dequeue();
        }
        else
        {
            // 如果两个池都没有可用对象，检查是否可以创建新对象
            if (activeItems.Count < maxPoolSize)
            {
                item = CreateGenericItem();
                genericPool.Dequeue(); // 因为CreateGenericItem会将物品入队，需要再出队
            }
            else
            {
                Debug.LogWarning("物品对象池已达到最大容量，无法创建更多物品");
                return null;
            }
        }
        
        // 配置物品
        if (item != null)
        {
            item.gameObject.name = $"Item_{itemSO.itemName}";
            item.gameObject.SetActive(true);
            item.SetItemSO(itemSO);
            activeItems.Add(item);
        }
        
        return item;
    }
    
    // 释放物品对象回池
    public void ReleaseItem(Item item)
    {
        if (item == null) return;
        
        int itemId = item.ItemID;
        
        // 重置物品状态
        item.gameObject.SetActive(false);
        
        // 从活跃列表移除
        activeItems.Remove(item);
        
        // 将物品放回适当的池中
        if (itemId > 0)
        {
            // 如果是已知ID的物品，放回专用池
            if (!itemPools.ContainsKey(itemId))
            {
                itemPools[itemId] = new Queue<Item>();
            }
            
            itemPools[itemId].Enqueue(item);
        }
        else
        {
            // 未知ID的物品放回通用池
            genericPool.Enqueue(item);
        }
    }
    
    // 清空所有池
    public void ClearPools()
    {
        // 释放所有活跃物品
        foreach (var item in new List<Item>(activeItems))
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        
        // 清空活跃列表
        activeItems.Clear();
        
        // 清空并销毁专用池物品
        foreach (var pool in itemPools.Values)
        {
            while (pool.Count > 0)
            {
                Item item = pool.Dequeue();
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
        }
        
        // 清空专用池字典
        itemPools.Clear();
        
        // 清空并销毁通用池物品
        while (genericPool.Count > 0)
        {
            Item item = genericPool.Dequeue();
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
    }
    
    // 获取池大小统计
    public string GetPoolStats()
    {
        int totalPooledItems = genericPool.Count;
        foreach (var pool in itemPools.Values)
        {
            totalPooledItems += pool.Count;
        }
        
        return $"活跃物品: {activeItems.Count}, 池中物品: {totalPooledItems}, 专用池数量: {itemPools.Count}";
    }
    
    private void OnDestroy()
    {
        ClearPools();
        Instance = null;
    }
} 