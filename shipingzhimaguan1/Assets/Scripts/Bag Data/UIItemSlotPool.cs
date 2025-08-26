using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI物品槽对象池 - 管理和重用物品槽对象
/// </summary>
public class UIItemSlotPool : MonoBehaviour
{
    public static UIItemSlotPool Instance { get; private set; }
    
    [Header("对象池设置")]
    [SerializeField] private GameObject itemSlotPrefab; // 物品槽预制体
    [SerializeField] private int initialPoolSize = 30;  // 初始池大小
    [SerializeField] private Transform poolParent;      // 池对象的父节点
    
    // 物品槽对象池
    private Queue<ItemSlot> slotPool = new Queue<ItemSlot>();
    
    // 当前活跃的物品槽
    private List<ItemSlot> activeSlots = new List<ItemSlot>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 如果没有指定池父节点，使用当前对象
        if (poolParent == null)
        {
            poolParent = transform;
        }
        
        // 初始化对象池
        InitializePool();
    }
    
    // 初始化对象池
    private void InitializePool()
    {
        if (itemSlotPrefab == null)
        {
            Debug.LogError("物品槽预制体未设置！");
            return;
        }
        
        // 预先创建物品槽对象
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, poolParent);
            slotObj.SetActive(false);
            
            ItemSlot slot = slotObj.GetComponent<ItemSlot>();
            if (slot != null)
            {
                slotPool.Enqueue(slot);
            }
            else
            {
                Debug.LogWarning("物品槽预制体没有ItemSlot组件！");
            }
        }
        
        Debug.Log($"[UIItemSlotPool] 初始化完成，对象池大小: {slotPool.Count}");
    }
    
    // 获取一个物品槽
    public ItemSlot GetSlot()
    {
        // 如果池中有可用对象，从池中取出
        if (slotPool.Count > 0)
        {
            ItemSlot slot = slotPool.Dequeue();
            slot.gameObject.SetActive(true);
            activeSlots.Add(slot);
            return slot;
        }
        
        // 如果池中没有可用对象，创建新对象
        Debug.Log("[UIItemSlotPool] 对象池已空，创建新物品槽");
        GameObject slotObj = Instantiate(itemSlotPrefab, poolParent);
        ItemSlot newSlot = slotObj.GetComponent<ItemSlot>();
        
        if (newSlot != null)
        {
            activeSlots.Add(newSlot);
            return newSlot;
        }
        
        Debug.LogError("[UIItemSlotPool] 创建新物品槽失败！");
        return null;
    }
    
    // 释放一个物品槽回池
    public void ReleaseSlot(ItemSlot slot)
    {
        if (slot == null) return;
        
        // 重置槽位状态
        slot.ClearSlot();
        slot.gameObject.SetActive(false);
        
        // 如果这个槽是活跃列表中的，移除它
        activeSlots.Remove(slot);
        
        // 将槽位放回对象池
        slotPool.Enqueue(slot);
    }
    
    // 释放所有激活的槽位
    public void ReleaseAllSlots()
    {
        foreach (var slot in new List<ItemSlot>(activeSlots))
        {
            ReleaseSlot(slot);
        }
        
        // 确保活跃列表已清空
        activeSlots.Clear();
    }
    
    // 获取当前活跃槽位
    public List<ItemSlot> GetActiveSlots()
    {
        return new List<ItemSlot>(activeSlots);
    }
} 