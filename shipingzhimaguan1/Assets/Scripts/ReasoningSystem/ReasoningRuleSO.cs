using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReasoningRule
{
    public ItemSO[] inputItems1;       // 输入物品
    public ItemSO[] inputItems2;       // 输入物品
    public ItemSO outputItem;         // 输出物品
    public bool isOrderSensitive;     // 是否顺序敏感
    public string successText;        // 成功提示文本
    public string failText;           // 失败提示文本
}

[CreateAssetMenu(fileName = "New Reasoning Rule", menuName = "Reasoning/Rule")]
public class ReasoningRuleSO : ScriptableObject
{
    [Header("输入物品")]
    public ItemSO inputItem1;   // 输入物品1
    public ItemSO inputItem2;   // 输入物品2
    
    [Header("输出物品")]
    public ItemSO outputItem;   // 输出物品
    
    [Header("规则描述")]
    [TextArea(3, 5)]
    public string ruleDescription;  // 规则描述
    
    // 验证规则是否有效
    public bool IsValid()
    {
        bool isValid = inputItem1 != null && inputItem2 != null && outputItem != null;
        if (!isValid)
        {
            // 规则无效: 输入或输出物品为空
        }
        return isValid;
    }

    // 检查两个物品是否匹配此规则
    public bool IsMatch(ItemSO item1, ItemSO item2)
    {
        if (!IsValid() || item1 == null || item2 == null)
        {
            return false;
        }

        // 检查正向或反向匹配
        bool isMatch = (item1 == inputItem1 && item2 == inputItem2) || 
                       (item1 == inputItem2 && item2 == inputItem1);
                       
        if (isMatch)
        {
            // 规则匹配成功
        }
        
        return isMatch;
    }

    // 打印规则信息（用于调试）
    public void PrintRuleInfo()
    {
        if (IsValid())
        {
            // 打印规则信息
            // 打印规则描述
        }
        else
        {
            Debug.LogWarning($"[ReasoningRuleSO] 规则 '{name}' 配置不完整，无法打印详细信息");
        }
    }
}
