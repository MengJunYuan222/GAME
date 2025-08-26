using UnityEngine;
using System;
using System.Collections.Generic;

// 物品数据类，用于背包系统内部使用，不再负责拾取功能
public class Item : MonoBehaviour
{
    [Header("引用数据")]
    [SerializeField] private ItemSO itemData; // 引用源ItemSO
    
    [Header("实例数据")]
    [SerializeField] private int stackCount = 1; // 堆叠数量，默认为1
    
    // 属性直接从ItemSO获取，避免数据冗余
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

    // 设置关联的ItemSO
    public void SetItemSO(ItemSO so)
    {
        itemData = so;
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
}

// 物品类型枚举
public enum ItemType
{
    物品,    // 普通物品
    线索,    // 线索类物品
    结论     // 推理结论
}

// ServiceLocator模式 - 依赖注入容器
public class ServiceLocator
{
    private static ServiceLocator instance;
    private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
    
    public static ServiceLocator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ServiceLocator();
            }
            return instance;
        }
    }
    
    public void RegisterService<T>(T service)
    {
        Type type = typeof(T);
        if (services.ContainsKey(type))
        {
            services[type] = service;
        }
        else
        {
            services.Add(type, service);
        }
    }
    
    public T GetService<T>()
    {
        Type type = typeof(T);
        if (services.TryGetValue(type, out object service))
        {
            return (T)service;
        }
        return default;
    }
    
    public void ClearServices()
    {
        services.Clear();
    }
}
