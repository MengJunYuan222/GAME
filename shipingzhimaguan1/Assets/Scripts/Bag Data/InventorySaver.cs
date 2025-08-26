using UnityEngine;
using System.Collections.Generic;
using System.IO;

// 可序列化的物品数据类，用于保存Item的信息
[System.Serializable]
public class ItemData
{
    public string itemName;
    public int itemID;
    public string itemDescription;
    public string itemIconName; // 保存Sprite的名称而不是Sprite对象
    public int itemType; // 使用枚举的整数值
}

// 可序列化的背包数据类
[System.Serializable]
public class InventoryData
{
    public List<ItemData> items = new List<ItemData>();
}

public class InventorySaver : MonoBehaviour
{
    public static InventorySaver Instance { get; private set; }
    
    [Header("设置")]
    public bool loadOnStart = true; // 是否在开始时自动加载物品
    public bool usePlayerPrefs = true; // 使用PlayerPrefs存储还是文件存储
    
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
        }
    }
    
    private void Start()
    {
        if (loadOnStart)
        {
            LoadInventory();
        }
    }
    
    /// <summary>
    /// 保存当前背包物品
    /// </summary>
    public void SaveInventory()
    {
        // 获取所有物品
        if (InventoryManager.Instance == null) 
        {
            Debug.LogWarning("[InventorySaver] 保存失败: InventoryManager实例不存在");
            return;
        }
        
        List<Item> allItems = InventoryManager.Instance.GetAllItems();
        if (allItems == null || allItems.Count == 0) 
        {
            Debug.Log("[InventorySaver] 没有物品需要保存");
            return;
        }
        
        // 创建保存数据
        List<int> itemIDs = new List<int>();
        foreach (var item in allItems)
        {
            if (item != null)
            {
                itemIDs.Add(item.ItemID);
                Debug.Log($"[InventorySaver] 保存物品: ID={item.ItemID}, 名称={item.ItemName}");
            }
        }
        
        // 保存所有物品ID
        if (usePlayerPrefs)
        {
            // 保存到PlayerPrefs
            string itemIDsJSON = JsonUtility.ToJson(new SerializableIntList { items = itemIDs });
            PlayerPrefs.SetString("SavedInventory", itemIDsJSON);
            PlayerPrefs.Save();
            Debug.Log($"[InventorySaver] 通过PlayerPrefs保存了{itemIDs.Count}个物品");
        }
        else
        {
            // 保存到文件
            SaveToFile(itemIDs);
        }
    }
    
    /// <summary>
    /// 加载背包物品
    /// </summary>
    public void LoadInventory()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventorySaver] 加载失败: InventoryManager实例不存在");
            return;
        }
        
        // 读取物品数据
        List<int> itemIDs = new List<int>();
        
        if (usePlayerPrefs)
        {
            // 从PlayerPrefs读取
            if (PlayerPrefs.HasKey("SavedInventory"))
            {
                string json = PlayerPrefs.GetString("SavedInventory");
                SerializableIntList itemList = JsonUtility.FromJson<SerializableIntList>(json);
                
                if (itemList != null && itemList.items != null)
                {
                    itemIDs = itemList.items;
                    Debug.Log($"[InventorySaver] 从PlayerPrefs中读取了{itemIDs.Count}个物品ID");
                }
            }
        }
        else
        {
            // 从文件读取
            itemIDs = LoadFromFile();
        }
        
        // 清空当前背包并添加物品
        InventoryManager.Instance.ResetInventory();
        
        // 添加所有物品
        if (itemIDs.Count > 0)
        {
            foreach (int itemID in itemIDs)
            {
                bool added = InventoryManager.Instance.AddItemByID(itemID);
                Debug.Log($"[InventorySaver] 加载物品: ID={itemID}, 添加{(added ? "成功" : "失败")}");
            }
        }
        else
        {
            Debug.Log("[InventorySaver] 没有找到已保存的物品数据");
        }
    }
    
    /// <summary>
    /// 保存到文件（可以替换为更复杂的文件保存系统）
    /// </summary>
    private void SaveToFile(List<int> itemIDs)
    {
        string filePath = Application.persistentDataPath + "/inventory_save.json";
        string json = JsonUtility.ToJson(new SerializableIntList { items = itemIDs });
        
        try
        {
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"[InventorySaver] 通过文件保存了{itemIDs.Count}个物品到: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventorySaver] 文件保存失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 从文件加载（可以替换为更复杂的文件加载系统）
    /// </summary>
    private List<int> LoadFromFile()
    {
        string filePath = Application.persistentDataPath + "/inventory_save.json";
        
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                SerializableIntList itemList = JsonUtility.FromJson<SerializableIntList>(json);
                
                if (itemList != null && itemList.items != null)
                {
                    Debug.Log($"[InventorySaver] 从文件加载了{itemList.items.Count}个物品");
                    return itemList.items;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventorySaver] 文件加载失败: {e.Message}");
        }
        
        return new List<int>();
    }
    
    /// <summary>
    /// 清除所有保存的物品数据
    /// </summary>
    public void ClearSavedData()
    {
        if (usePlayerPrefs)
        {
            PlayerPrefs.DeleteKey("SavedInventory");
            PlayerPrefs.Save();
        }
        else
        {
            string filePath = Application.persistentDataPath + "/inventory_save.json";
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        
        Debug.Log("[InventorySaver] 已清除保存的物品数据");
    }
}

// 用于序列化List<int>的辅助类
[System.Serializable]
public class SerializableIntList
{
    public List<int> items = new List<int>();
}
