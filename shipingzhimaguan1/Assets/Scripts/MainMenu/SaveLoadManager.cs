using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

// 用于序列化的游戏存档数据类
[Serializable]
public class SaveData
{
    // 存档基本信息
    public string playerName = "玩家";
    public int level = 1;
    public Vector3 playerPosition;
    public float playTime;
    public string saveDate;
    
    // 游戏进度数据
    public Dictionary<string, bool> completedQuests = new Dictionary<string, bool>();
    public List<string> collectedItems = new List<string>();
    public Dictionary<string, int> stats = new Dictionary<string, int>();
    
    // 构造函数，创建新存档时使用
    public SaveData()
    {
        playerPosition = Vector3.zero;
        playTime = 0f;
        saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// 存储使用JsonUtility序列化字典的辅助类
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [Serializable]
    public class KeyValuePair
    {
        public TKey key;
        public TValue value;
    }
    
    public List<KeyValuePair> pairs = new List<KeyValuePair>();
    
    // 从字典转换为可序列化列表
    public void FromDictionary(Dictionary<TKey, TValue> dict)
    {
        pairs.Clear();
        foreach (var kvp in dict)
        {
            pairs.Add(new KeyValuePair { key = kvp.Key, value = kvp.Value });
        }
    }
    
    // 从可序列化列表转回字典
    public Dictionary<TKey, TValue> ToDictionary()
    {
        Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
        foreach (var pair in pairs)
        {
            dict[pair.key] = pair.value;
        }
        return dict;
    }
}

// 包装SaveData中的字典为可序列化格式
[Serializable]
public class SerializableSaveData
{
    // 基本数据直接复制
    public string playerName;
    public int level;
    public Vector3 playerPosition;
    public float playTime;
    public string saveDate;
    
    // 将字典转为可序列化格式
    public SerializableDictionary<string, bool> completedQuests = new SerializableDictionary<string, bool>();
    public List<string> collectedItems = new List<string>();
    public SerializableDictionary<string, int> stats = new SerializableDictionary<string, int>();
    
    // 从SaveData创建可序列化格式
    public SerializableSaveData(SaveData data)
    {
        playerName = data.playerName;
        level = data.level;
        playerPosition = data.playerPosition;
        playTime = data.playTime;
        saveDate = data.saveDate;
        collectedItems = data.collectedItems;
        
        // 转换字典
        completedQuests.FromDictionary(data.completedQuests);
        stats.FromDictionary(data.stats);
    }
    
    // 转回SaveData
    public SaveData ToSaveData()
    {
        SaveData data = new SaveData();
        data.playerName = playerName;
        data.level = level;
        data.playerPosition = playerPosition;
        data.playTime = playTime;
        data.saveDate = saveDate;
        data.collectedItems = collectedItems;
        
        // 转回字典
        data.completedQuests = completedQuests.ToDictionary();
        data.stats = stats.ToDictionary();
        
        return data;
    }
}

public class SaveLoadManager : MonoBehaviour
{
    // 当前游戏的存档数据
    private SaveData currentSaveData;
    
    // 存档文件名
    private readonly string saveFileName = "gamesave.json";
    
    // 存档文件的完整路径
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    
    private void Awake()
    {
        // 初始化存档数据
        currentSaveData = new SaveData();
    }
    
    // 判断是否存在存档
    public bool HasSaveData()
    {
        return File.Exists(SaveFilePath);
    }
    
    // 保存游戏
    public void SaveGame()
    {
        try
        {
            // 更新保存时间
            currentSaveData.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // 转换为可序列化格式
            SerializableSaveData serializableData = new SerializableSaveData(currentSaveData);
            
            // 序列化为JSON
            string json = JsonUtility.ToJson(serializableData, true);
            
            // 写入文件
            File.WriteAllText(SaveFilePath, json);
            
            Debug.Log("游戏已保存: " + SaveFilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("保存游戏失败: " + e.Message);
        }
    }
    
    // 加载游戏
    public bool LoadGame()
    {
        try
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("没有找到存档文件");
                return false;
            }
            
            // 读取文件
            string json = File.ReadAllText(SaveFilePath);
            
            // 反序列化
            SerializableSaveData serializableData = JsonUtility.FromJson<SerializableSaveData>(json);
            
            // 转换为SaveData
            currentSaveData = serializableData.ToSaveData();
            
            Debug.Log("游戏已加载: " + SaveFilePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("加载游戏失败: " + e.Message);
            return false;
        }
    }
    
    // 获取当前存档数据
    public SaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }
    
    // 创建新游戏数据
    public void CreateNewGame()
    {
        currentSaveData = new SaveData();
        Debug.Log("创建新游戏数据");
    }
    
    // 更新存档数据
    public void UpdateSaveData(SaveData newData)
    {
        currentSaveData = newData;
    }
    
    // 删除存档
    public void DeleteSave()
    {
        if (HasSaveData())
        {
            File.Delete(SaveFilePath);
            Debug.Log("存档已删除");
        }
    }
} 