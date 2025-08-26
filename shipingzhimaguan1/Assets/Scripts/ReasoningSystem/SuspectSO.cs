using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Suspect", menuName = "Reasoning/Suspect")]
public class SuspectSO : ScriptableObject
{
    [Header("嫌疑人基本信息")]
    public string suspectName;        // 嫌疑人姓名
    
    [Tooltip("年龄")]
    public int age = 25;
    
    [Tooltip("籍贯")]
    public string hometown = "籍贯";
    
    [Header("详细信息")]
    [Tooltip("简介")]
    [TextArea(3, 6)]
    public string description = "嫌疑人简介...";
   
    
    [Header("视觉表现")]
    public Sprite suspectPortrait;    // 嫌疑人画像/立绘
    
    [Header("相关结论")]
    public List<ItemSO> relatedConclusions = new List<ItemSO>(); // 与嫌疑人相关的结论列表
    
    [Header("解锁条件")]
    public int requiredConclusionsCount = 2; // 确认嫌疑人所需的结论数量
    
    [Header("案件相关")]
    [Tooltip("可能涉及的罪名")]
    public string[] possibleCrimes = new string[0];
    
    // 移除了isRevealed变量
    
    // 检查是否已收集足够的结论以揭示嫌疑人
    public bool CanBeRevealed(List<ItemSO> discoveredConclusions)
    {
        if (discoveredConclusions == null || discoveredConclusions.Count == 0)
        {
            return false;
        }
        
        int matchCount = 0;
        foreach (var conclusion in discoveredConclusions)
        {
            if (conclusion == null)
            {
                continue;
            }
            
            if (relatedConclusions.Contains(conclusion))
            {
                matchCount++;
            }
        }
        
        return matchCount >= requiredConclusionsCount;
    }
    
    /// <summary>
    /// 获取格式化的年龄文本
    /// </summary>
    public string GetAgeText()
    {
        return $"{age}岁";
    }
    
    /// <summary>
    /// 验证数据完整性
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(suspectName) && age > 0;
    }
}