using UnityEngine;
using System.Collections.Generic;

public class ReasoningManager : MonoBehaviour
{
    public static ReasoningManager Instance { get; private set; }

    [Header("规则配置")]
    [SerializeField] private List<ReasoningRuleSO> allRules = new List<ReasoningRuleSO>();

    // 事件系统
    public System.Action<ItemSO> OnReasoningSuccess; // 推理成功时触发
    public System.Action<string> OnReasoningFail;    // 推理失败时触发

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

    // 尝试进行推理
    public bool TryReasoning(ItemSO item1, ItemSO item2, out ItemSO resultItem)
    {
        resultItem = null;

        // 检查输入物品是否有效
        if (item1 == null || item2 == null)
        {
            Debug.LogWarning("推理失败：输入物品为空");
            return false;
        }

        // 尝试推理

        // 查找匹配的规则
        ReasoningRuleSO matchedRule = FindMatchingRule(item1, item2);
        if (matchedRule == null)
        {
            // 未找到匹配的规则
            OnReasoningFail?.Invoke("这个组合似乎没有产生任何效果...");
            return false;
        }

        // 推理成功
        resultItem = matchedRule.outputItem;
        
        if (resultItem == null)
        {
            Debug.LogError($"[ReasoningManager] 规则 {matchedRule.name} 的输出物品为空!");
            OnReasoningFail?.Invoke("推理过程出现错误...");
            return false;
        }
        
        // 检查结果是否为结论物品
        if (resultItem.itemType == ItemType.结论)
        {
            // 推理成功! 匹配规则和输出结论
            // 结论物品详情已记录
        }
        else
        {
        // 推理成功! 匹配规则和输出物品
        }
        
        // 触发推理成功事件
        if (OnReasoningSuccess != null)
        {
            // 触发推理成功事件
            OnReasoningSuccess.Invoke(resultItem);
        }
        else
        {
            Debug.LogWarning("[ReasoningManager] 推理成功事件没有监听器!");
        }
        
        return true;
    }

    // 查找匹配的推理规则
    private ReasoningRuleSO FindMatchingRule(ItemSO item1, ItemSO item2)
    {
        int ruleIndex = 0;
        
        foreach (var rule in allRules)
        {
            if (rule == null)
            {
                Debug.LogWarning($"[ReasoningManager] 规则列表中存在空规则，索引: {ruleIndex}");
                ruleIndex++;
                continue;
            }

            // 检查规则输入物品是否为空
            if (rule.inputItem1 == null || rule.inputItem2 == null || rule.outputItem == null)
            {
                Debug.LogWarning($"[ReasoningManager] 规则 '{rule.name}' 配置不完整，跳过");
                ruleIndex++;
                continue;
            }
            
            // 使用itemID进行比较而不是对象引用
            // 检查正向组合
            bool forwardMatch = (rule.inputItem1.itemID == item1.itemID && rule.inputItem2.itemID == item2.itemID);
            
            if (forwardMatch)
            {
                return rule;
            }
            
            // 检查反向组合
            bool reverseMatch = (rule.inputItem1.itemID == item2.itemID && rule.inputItem2.itemID == item1.itemID);
            
            if (reverseMatch)
            {
                return rule;
            }
            
            // 如果ID匹配失败，尝试使用对象引用比较（兼容旧代码）
            bool forwardRefMatch = (rule.inputItem1 == item1 && rule.inputItem2 == item2);
            bool reverseRefMatch = (rule.inputItem1 == item2 && rule.inputItem2 == item1);
            
            if (forwardRefMatch || reverseRefMatch)
            {
                return rule;
            }
            
            ruleIndex++;
        }
        
        return null;
    }

    // 添加新规则
    public void AddRule(ReasoningRuleSO rule)
    {
        if (!allRules.Contains(rule))
        {
            allRules.Add(rule);
        }
    }

    // 移除规则
    public void RemoveRule(ReasoningRuleSO rule)
    {
        allRules.Remove(rule);
    }

    // 调试方法：打印所有规则信息 - 注释掉不必要的日志输出
    [ContextMenu("打印所有规则信息")]
    public void DebugPrintAllRules()
    {
        // 只在编辑器中输出规则信息，游戏运行时不输出
        #if UNITY_EDITOR
        // Debug.Log($"=== 推理系统规则信息 ===");
        // Debug.Log($"总规则数量: {allRules.Count}");
        
        if (allRules.Count == 0)
        {
            Debug.LogWarning("没有配置任何推理规则！");
            return;
        }
        
        int validRules = 0;
        int invalidRules = 0;
        
        for (int i = 0; i < allRules.Count; i++)
        {
            var rule = allRules[i];
            if (rule == null)
            {
                Debug.LogError($"规则 #{i+1}: 空引用");
                invalidRules++;
                continue;
            }
            
            if (rule.IsValid())
            {
                // Debug.Log($"规则 #{i+1}: {rule.name} - {rule.inputItem1.itemName} + {rule.inputItem2.itemName} => {rule.outputItem.itemName}");
                validRules++;
            }
            else
            {
                Debug.LogWarning($"规则 #{i+1}: {rule.name} - 配置不完整");
                invalidRules++;
            }
        }
        
        // Debug.Log($"=== 规则统计 ===");
        // Debug.Log($"有效规则: {validRules}");
        // Debug.Log($"无效规则: {invalidRules}");
        // Debug.Log($"总规则数: {allRules.Count}");
        #endif
    }

    // 在Start中自动打印规则信息（可选）
    private void Start()
    {
        // 初始化推理系统，但不输出日志
        // Debug.Log("[ReasoningManager] 推理系统已初始化，调试日志已启用");
        
        // 游戏开始时不再自动打印规则信息
        // DebugPrintAllRules();
    }

    // 强制输出调试信息的方法，可以在任何时候调用
    [ContextMenu("输出调试信息")]
    public void ForceDebugOutput()
    {
        Debug.Log("[ReasoningManager] 强制输出调试信息");
        Debug.Log("[ReasoningManager] 如果您能看到此消息，说明调试日志功能正常");
        Debug.Log("[ReasoningManager] Unity控制台是否已打开？请确保控制台窗口已打开并且日志级别设置为显示Debug信息");
        DebugPrintAllRules();
    }
}