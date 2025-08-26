using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace DialogueSystem
{
    /// <summary>
    /// 出示节点 - 用于玩家向NPC出示物品并获得反应的对话节点
    /// </summary>
    [Serializable] // 添加Serializable特性，确保Unity序列化
    public class PresentationNode : BaseNode
    {
        // 注意：我们从BaseNode继承了[Input] public BaseNode input;
        // 但在编辑器中可能不会自动显示，所以在编辑器脚本中特殊处理
        
        [Output] public BaseNode defaultOutput; // 默认输出，当没有匹配的物品时使用

        [Header("角色与对话")]
        [SerializeField] 
        public ActorSO actorSO; // 对话角色
        [TextArea(3, 10)]
        public string dialogueText; // 对话内容
        
        [Header("节点设置")]
        [Tooltip("勾选此项表示此节点是一次性对话，玩家只能交互一次")]
        public bool isOneTimeDialogue = false;
        [Tooltip("勾选此项表示此节点是对话结束节点")]
        public bool isEndNode = false;

        [Header("物品反应设置")]
        [Tooltip("物品SO列表及其对应的反应节点")]
        public List<ItemReactionPair> itemReactions = new List<ItemReactionPair>();

        [System.Serializable]
        public class ItemReactionPair
        {
            public ItemSO item; // 物品SO
            public BaseNode targetNode; // 对应的反应节点
        }

        // 当前已出示的物品
        private ItemSO presentedItem;
        // 是否已经处理过物品出示
        private bool hasProcessedPresentation = false;

        // 初始化节点
        protected override void Init()
        {
            base.Init();
            UpdateNodeName();
        }
        
        // 验证节点配置
        private void OnValidate()
        {
            // 更新节点名称
            UpdateNodeName();
            
            try
            {
                // 确保动态端口与物品反应数量匹配
                UpdateDynamicPorts();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"PresentationNode.OnValidate错误: {e.Message}");
            }
        }
        
        // 更新动态端口，确保端口数量与物品反应数量一致
        public void UpdateDynamicPorts()
        {
            if (itemReactions == null) return;
            
            // 为每个物品反应创建一个动态输出端口
            for (int i = 0; i < itemReactions.Count; i++)
            {
                string portName = $"itemReactions {i}";
                if (!HasPort(portName))
                {
                    try 
                    {
                        AddDynamicOutput(typeof(BaseNode), Node.ConnectionType.Override, Node.TypeConstraint.Inherited, portName);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"添加端口失败: {portName}，错误: {e.Message}");
                    }
                }
            }
            
            // 安全移除多余的端口
            var portsToRemove = new List<NodePort>();
            foreach (var port in DynamicOutputs)
            {
                if (port.fieldName.StartsWith("itemReactions "))
                {
                    string indexStr = port.fieldName.Substring("itemReactions ".Length);
                    int index;
                    if (int.TryParse(indexStr, out index) && index >= itemReactions.Count)
                    {
                        portsToRemove.Add(port);
                    }
                }
            }
            
            foreach (var port in portsToRemove)
            {
                try
                {
                    RemoveDynamicPort(port);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"移除端口失败: {port.fieldName}，错误: {e.Message}");
                }
            }
        }

        // 更新节点名称
        public override void UpdateNodeName()
        {
            // 获取角色名称和对话预览
            string actorName = actorSO != null ? actorSO.actorName : "未知角色";
            string textPreview = "";
            
            if (!string.IsNullOrEmpty(dialogueText))
            {
                // 截取前15个字符作为预览
                textPreview = dialogueText.Length > 15 ? dialogueText.Substring(0, 15) + "..." : dialogueText;
            }
            
            // 根据物品反应数量和角色名称更新名称
            if (itemReactions != null && itemReactions.Count > 0)
            {
                name = $"{actorName}: {textPreview} ({itemReactions.Count}种反应)";
            }
            else
            {
                name = $"{actorName}: {textPreview}";
            }
            
            // 如果没有设置角色和对话，使用默认名称
            if (actorSO == null && string.IsNullOrEmpty(dialogueText))
            {
                name = "出示物品节点";
            }
        }

        // 表示是否等待物品选择
        private bool waitingForItemSelection = false;
        
        // 处理节点
        public override void ProcessNode(DialogueUIManager uiManager, DialogueNodeGraph graph)
        {
            if (uiManager == null || graph == null) return;

            // 重置状态
            hasProcessedPresentation = false;
            presentedItem = null;
            waitingForItemSelection = true;  // 标记为正在等待物品选择
            
            Debug.Log("[PresentationNode] ProcessNode: 启用出示功能，等待玩家选择物品");

            // 启用出示按钮并设置回调
            uiManager.EnableItemPresentation(OnItemPresented);

            // 显示对话 - 使用角色和对话内容
            uiManager.ShowDialogue(actorSO, dialogueText);
            
            // 如果是一次性对话，标记为已完成
            if (isOneTimeDialogue && graph != null)
            {
                // 检查是否有MarkDialogueAsCompleted方法
                System.Reflection.MethodInfo method = graph.GetType().GetMethod("MarkDialogueAsCompleted", 
                                                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    try
                    {
                        // 调用MarkDialogueAsCompleted方法，传递this作为DialogueNode参数
                        method.Invoke(graph, new object[] { this });
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"标记一次性对话节点时出错: {ex.Message}");
                    }
                }
            }
        }

        // 物品出示回调
        private void OnItemPresented(ItemSO item)
        {
            if (hasProcessedPresentation) return;
            hasProcessedPresentation = true;
            
            // 重要：标记不再等待物品选择
            waitingForItemSelection = false;

            // 保存出示的物品
            presentedItem = item;

            // 获取当前对话UI管理器和对话图
            DialogueUIManager uiManager = DialogueUIManager.Instance;
            DialogueNodeGraph graph = null;
            
            if (uiManager != null)
            {
                graph = uiManager.CurrentGraph;
                
                // 隐藏出示按钮
                uiManager.DisableItemPresentation();
                
                // 添加调试日志
                Debug.Log($"[PresentationNode] 玩家出示物品: {item.itemName} (ID: {item.itemID})");
                
                // 检查是否有匹配的物品反应
                bool hasMatchingReaction = false;
                for (int i = 0; i < itemReactions.Count; i++)
                {
                    if (itemReactions[i].item != null && itemReactions[i].item.itemID == item.itemID)
                    {
                        hasMatchingReaction = true;
                        Debug.Log($"[PresentationNode] 找到匹配的物品反应: {itemReactions[i].item.itemName}");
                        break;
                    }
                }
                
                if (!hasMatchingReaction)
                {
                    Debug.Log("[PresentationNode] 没有找到匹配的物品反应，将使用默认输出");
                }
            }

            if (graph != null)
            {
                // 触发下一个节点
                Debug.Log("[PresentationNode] OnItemPresented: 物品已选择，处理下一节点");
                graph.ProcessNextNode();
            }
        }

        // 获取下一个节点
        public override BaseNode GetNextNode()
        {
            // 如果明确设置为结束节点，返回null
            if (isEndNode)
            {
                Debug.Log("[PresentationNode] GetNextNode: 这是结束节点，返回null");
                return null;
            }
            
            // 如果还在等待物品选择，不应该继续到下一个节点
            if (waitingForItemSelection && presentedItem == null)
            {
                Debug.Log("[PresentationNode] GetNextNode: 正在等待玩家选择物品，暂不进行到下一节点");
                return this; // 返回自身，表示需要停留在当前节点
            }
            
            // 如果没有出示物品，使用默认输出
            if (presentedItem == null)
            {
                Debug.Log("[PresentationNode] GetNextNode: 未出示物品，使用默认输出");
                var port = GetOutputPort("defaultOutput");
                if (port != null && port.IsConnected)
                {
                    var nextNode = port.Connection.node as BaseNode;
                    Debug.Log($"[PresentationNode] GetNextNode: 默认输出连接到 {(nextNode != null ? nextNode.name : "null")}");
                    return nextNode;
                }
                Debug.Log("[PresentationNode] GetNextNode: 默认输出未连接，返回null");
                return null;
            }

            Debug.Log($"[PresentationNode] GetNextNode: 正在处理出示的物品 {presentedItem.itemName} (ID: {presentedItem.itemID})");
            
            // 查找匹配的物品反应
            for (int i = 0; i < itemReactions.Count; i++)
            {
                if (itemReactions[i].item == null)
                {
                    Debug.Log($"[PresentationNode] GetNextNode: 物品反应 {i} 的item为null，跳过");
                    continue;
                }
                
                Debug.Log($"[PresentationNode] GetNextNode: 比较物品 - 出示: {presentedItem.itemID}, 配置: {itemReactions[i].item.itemID}");
                
                if (itemReactions[i].item.itemID == presentedItem.itemID)
                {
                    Debug.Log($"[PresentationNode] GetNextNode: 找到匹配的物品反应 {i}");
                    
                    // 首先检查是否通过端口连接
                    string portName = $"itemReactions {i}";
                    var port = GetOutputPort(portName);
                    
                    if (port != null && port.IsConnected)
                    {
                        var nextNode = port.Connection.node as BaseNode;
                        Debug.Log($"[PresentationNode] GetNextNode: 通过端口连接到下一个节点 {(nextNode != null ? nextNode.name : "null")}");
                        return nextNode;
                    }
                    else
                    {
                        Debug.Log($"[PresentationNode] GetNextNode: 端口 {portName} 未连接或不存在");
                    }
                    
                    // 如果没有通过端口连接，检查是否通过直接引用连接
                    if (itemReactions[i].targetNode != null)
                    {
                        Debug.Log($"[PresentationNode] GetNextNode: 通过直接引用连接到 {itemReactions[i].targetNode.name}");
                        return itemReactions[i].targetNode;
                    }
                    else
                    {
                        Debug.Log($"[PresentationNode] GetNextNode: 物品反应 {i} 的targetNode为null");
                    }
                }
            }

            // 如果没有匹配的反应，使用默认输出
            Debug.Log("[PresentationNode] GetNextNode: 没有匹配的物品反应，使用默认输出");
            var defaultPort = GetOutputPort("defaultOutput");
            if (defaultPort != null && defaultPort.IsConnected)
            {
                var nextNode = defaultPort.Connection.node as BaseNode;
                Debug.Log($"[PresentationNode] GetNextNode: 默认输出连接到 {(nextNode != null ? nextNode.name : "null")}");
                return nextNode;
            }
            
            Debug.Log("[PresentationNode] GetNextNode: 没有可用的输出连接，返回null");
            return null;
        }

        // 检查是否是结束节点
        public override bool IsEndNode()
        {
            // 如果明确设置为结束节点，返回true
            if (isEndNode) return true;
            
            // 如果默认输出没有连接，并且没有任何物品反应的连接，则视为结束节点
            bool hasConnection = false;
            
            var defaultPort = GetOutputPort("defaultOutput");
            if (defaultPort != null && defaultPort.IsConnected)
            {
                hasConnection = true;
            }

            // 检查所有物品反应端口
            for (int i = 0; i < itemReactions.Count; i++)
            {
                string portName = $"itemReactions {i}";
                var port = GetOutputPort(portName);
                if (port != null && port.IsConnected)
                {
                    hasConnection = true;
                    break;
                }
            }

            return !hasConnection;
        }

        // 获取输出端口值
        public override object GetValue(NodePort port)
        {
            // 如果是输入端口，直接返回this
            if (port.IsInput)
                return this;
            
            // 处理基类输入端口
            if (port.fieldName == "baseInput")
                return this;
                
            // 如果是默认输出端口
            if (port.fieldName == "defaultOutput")
                return this;
                
            // 检查是否是物品反应的端口
            if (port.fieldName.StartsWith("itemReactions "))
            {
                return this;
            }
                
            return this;
        }
        
        // 当连接被创建时调用
        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            base.OnCreateConnection(from, to);
            
            // 确保动态端口更新
            UpdateDynamicPorts();
        }
        
        // 当连接被移除时调用
        public override void OnRemoveConnection(NodePort port)
        {
            base.OnRemoveConnection(port);
            
            // 确保动态端口更新
            UpdateDynamicPorts();
        }
        
        // 添加物品反应
        [ContextMenu("添加物品反应")]
        public void AddItemReaction()
        {
            itemReactions.Add(new ItemReactionPair());
            
            // 更新动态端口
            UpdateDynamicPorts();
        }
        
        // 清空所有物品反应
        [ContextMenu("清空所有物品反应")]
        public void ClearAllItemReactions()
        {
            itemReactions.Clear();
            
            // 更新动态端口
            UpdateDynamicPorts();
        }
    }
} 