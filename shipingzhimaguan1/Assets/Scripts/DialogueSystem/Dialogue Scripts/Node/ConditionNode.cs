using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace DialogueSystem
{
    /// <summary>
    /// 条件节点 - 根据条件自动选择分支
    /// </summary>
    public class ConditionNode : BaseNode
    {
        [Output] public BaseNode trueNode; // 条件为真时的节点
        [Output] public BaseNode falseNode; // 条件为假时的节点
        
        [Header("条件类型")]
        [Tooltip("选择用于分支判断的条件类型")]
        public ConditionType conditionType = ConditionType.None;
        
        // 条件类型枚举
        public enum ConditionType
        {
            None,           // 无条件：默认返回false
            HasItem,        // 物品检查：玩家是否拥有特定物品
            CheckFlag,      // 标志检查：检查游戏中的标志状态
            CompareValue,   // 值比较：比较游戏中数值
            CheckQuestStatus, // 任务状态：检查任务完成状态
            Custom          // 自定义条件：使用代码实现自定义判断
        }
        
        // 比较操作符
        public enum CompareOperator
        {
            Equal,          // 等于
            NotEqual,       // 不等于
            Greater,        // 大于
            Less,           // 小于
            GreaterOrEqual, // 大于等于
            LessOrEqual     // 小于等于
        }
        
        // 物品检查设置 (使用ItemSO直接引用)
        [Header("物品检查 (HasItem)")]
        [Tooltip("拖拽要检查的物品SO，检查玩家是否拥有该物品")]
        public ItemSO itemSO; // 直接拖拽物品SO对象
        
        // 标志检查设置
        [Header("标志检查 (CheckFlag)")]
        [Tooltip("要检查的标志名称")]
        public string flagName = "";
        [Tooltip("期望的标志值")]
        public bool expectedValue = true;
        
        // 值比较设置
        [Header("值比较 (CompareValue)")]
        [Tooltip("要比较的变量名称")]
        public string variableName = "";
        [Tooltip("比较运算符")]
        public CompareOperator compareOperator = CompareOperator.Equal;
        [Tooltip("比较目标值")]
        public float compareValue = 0;
        
        // 任务状态检查
        [Header("任务状态 (CheckQuestStatus)")]
        [Tooltip("任务ID")]
        public string questId = "";
        public enum QuestStatus { NotStarted, InProgress, Completed, Failed }
        [Tooltip("期望的任务状态")]
        public QuestStatus expectedStatus = QuestStatus.Completed;
        
        // 自定义条件
        [Header("自定义条件 (Custom)")]
        [Tooltip("脚本会使用SetCustomCondition方法设置自定义条件回调")]
        private System.Func<bool> customConditionCallback;
        
        // 设置自定义条件回调
        public void SetCustomCondition(System.Func<bool> callback)
        {
            customConditionCallback = callback;
        }
        
        // 初始化节点
        protected override void Init()
        {
            base.Init();
            
            // 设置初始名称
            UpdateNodeName();
        }
        
        // 当节点添加到图表时调用
        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            if (from != null && to != null)
            {
                base.OnCreateConnection(from, to);
            }
            
            // 连接后确保名称唯一
            UpdateNodeName();
        }
        
        // 更新节点名称，确保唯一性
        public override void UpdateNodeName()
        {
            // 基础名称使用条件类型
            string baseName = "Condition";
            
            // 根据条件类型细化名称
            switch (conditionType)
            {
                case ConditionType.HasItem:
                    if (itemSO != null)
                        baseName = $"Condition_有{itemSO.itemName}";
                    else
                        baseName = "Condition_有物品";
                    break;
                case ConditionType.CheckFlag:
                    if (!string.IsNullOrEmpty(flagName))
                        baseName = $"Condition_检查{flagName}";
                    else
                        baseName = "Condition_检查标志";
                    break;
                case ConditionType.CompareValue:
                    if (!string.IsNullOrEmpty(variableName))
                        baseName = $"Condition_比较{variableName}";
                    else
                        baseName = "Condition_比较值";
                    break;
                case ConditionType.CheckQuestStatus:
                    if (!string.IsNullOrEmpty(questId))
                        baseName = $"Condition_任务{questId}";
                    else
                        baseName = "Condition_任务状态";
                    break;
                case ConditionType.Custom:
                    baseName = "Condition_自定义";
                    break;
            }
            
            // 确保名称唯一
            name = NodeUtility.EnsureUniqueName(graph, this, baseName);
        }
        
        // 在Unity编辑器下验证
        #if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateNodeName();
        }
        #endif
        
        // 处理节点
        public override void ProcessNode(DialogueUIManager uiManager, DialogueNodeGraph graph)
        {
            // 条件节点不显示UI内容，直接评估条件并处理下一个节点
            if (graph != null)
            {
                graph.ProcessNextNode();
            }
        }
        
        // 获取下一个节点
        public override BaseNode GetNextNode()
        {
            // 评估条件
            bool conditionResult = EvaluateCondition();
            
            Debug.Log($"条件节点结果: {conditionResult}，将选择{(conditionResult ? "True" : "False")}分支");
            
            // 根据条件结果，返回相应的节点
            if (conditionResult)
            {
                var truePort = GetOutputPort("trueNode");
                if (truePort != null && truePort.IsConnected)
                {
                    return truePort.Connection.node as BaseNode;
                }
            }
            else
            {
                var falsePort = GetOutputPort("falseNode");
                if (falsePort != null && falsePort.IsConnected)
                {
                    return falsePort.Connection.node as BaseNode;
                }
            }
            
            // 如果没有连接相应的节点，返回null
            Debug.LogWarning($"条件节点没有连接{(conditionResult ? "True" : "False")}分支");
            return null;
        }
        
        // 评估条件
        private bool EvaluateCondition()
        {
            switch (conditionType)
            {
                case ConditionType.HasItem:
                    return CheckHasItem();
                case ConditionType.CheckFlag:
                    return CheckFlag();
                case ConditionType.CompareValue:
                    return CompareValue();
                case ConditionType.CheckQuestStatus:
                    return CheckQuestStatus();
                case ConditionType.Custom:
                    return customConditionCallback != null ? customConditionCallback() : false;
                default:
                    return false;
            }
        }
        
        // 检查物品 - 简化版，直接使用ItemSO引用
        private bool CheckHasItem()
        {
            if (itemSO == null)
            {
                Debug.LogWarning("没有指定物品SO，物品检查失败");
                return false;
            }

            Debug.Log($"检查玩家是否拥有物品: {itemSO.itemName} (ID: {itemSO.itemID})");

            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("InventoryManager实例不可用");
                return false;
            }

            List<Item> playerItems = InventoryManager.Instance.GetAllItems();
            if (playerItems == null || playerItems.Count == 0)
            {
                Debug.Log("玩家没有任何物品");
                return false;
            }

            bool playerHasItem = false;
            foreach (Item item in playerItems)
            {
                if (item != null && item.ItemData != null && item.ItemData.itemID == itemSO.itemID)
                {
                    playerHasItem = true;
                    break;
                }
            }

            Debug.Log($"玩家{(playerHasItem ? "拥有" : "没有")}物品: {itemSO.itemName}");
            return playerHasItem;
        }
        
        // 检查标志
        private bool CheckFlag()
        {
            if (string.IsNullOrEmpty(flagName))
            {
                Debug.LogWarning("没有指定标志名称");
                return false;
            }
            
            Debug.Log($"检查标志: {flagName} == {expectedValue}");
            
            // 使用PlayerPrefs读取标志值
            int storedValue = PlayerPrefs.GetInt(flagName, 0);
            bool currentValue = storedValue == 1;
            
            bool result = currentValue == expectedValue;
            Debug.Log($"标志值为: {currentValue}，期望值为: {expectedValue}，结果: {result}");
            return result;
            
            // 注意：在实际项目中可能需要使用更复杂的游戏状态管理系统
            // 例如: return GameStateManager.Instance.GetFlag(flagName) == expectedValue;
        }
        
        // 比较值
        private bool CompareValue()
        {
            if (string.IsNullOrEmpty(variableName))
            {
                Debug.LogWarning("没有指定变量名称");
                return false;
            }
            
            Debug.Log($"比较值: {variableName} {compareOperator} {compareValue}");
            
            // 使用PlayerPrefs读取变量值
            float currentValue = PlayerPrefs.GetFloat(variableName, 0);
            
            bool result = false;
            switch (compareOperator)
            {
                case CompareOperator.Equal:
                    result = Mathf.Approximately(currentValue, compareValue);
                    break;
                case CompareOperator.NotEqual:
                    result = !Mathf.Approximately(currentValue, compareValue);
                    break;
                case CompareOperator.Greater:
                    result = currentValue > compareValue;
                    break;
                case CompareOperator.Less:
                    result = currentValue < compareValue;
                    break;
                case CompareOperator.GreaterOrEqual:
                    result = currentValue >= compareValue;
                    break;
                case CompareOperator.LessOrEqual:
                    result = currentValue <= compareValue;
                    break;
            }
            
            Debug.Log($"当前值为: {currentValue}，比较值为: {compareValue}，结果: {result}");
            return result;
        }
        
        // 检查任务状态
        private bool CheckQuestStatus()
        {
            if (string.IsNullOrEmpty(questId))
            {
                Debug.LogWarning("没有指定任务ID");
                return false;
            }
            
            Debug.Log($"检查任务: {questId} 状态 == {expectedStatus}");
            
            // 使用PlayerPrefs读取任务状态
            int storedStatus = PlayerPrefs.GetInt("Quest_" + questId, 0);
            QuestStatus currentStatus = (QuestStatus)storedStatus;
            
            bool result = currentStatus == expectedStatus;
            Debug.Log($"当前任务状态: {currentStatus}，期望状态: {expectedStatus}，结果: {result}");
            return result;
        }
        
        // 获取输出端口值
        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "trueNode" || port.fieldName == "falseNode")
            {
                return this;
            }
            return null;
        }
    }
}