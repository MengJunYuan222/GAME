using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using UnityEngine.Playables;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "New Dialogue Node Graph", menuName = "Dialogue System/Dialogue Node Graph")]
    public class DialogueNodeGraph : NodeGraph
    {
        [Header("对话设置")]
        [Tooltip("必须手动指定起始节点")]
        public BaseNode startNode;
        
        [Header("调试设置")]
        [SerializeField] private bool logDebugInfo = false;
        
        [Header("存档设置")]
        [Tooltip("如果启用，一次性对话状态将被保存到PlayerPrefs")]
        [SerializeField] private bool saveCompletedDialogues = true;
        [Tooltip("重置对话状态（仅编辑模式）")]
        [SerializeField] private bool resetDialoguesOnPlay = false;

        // 当前节点
        [System.NonSerialized]
        public BaseNode currentNode;
        
        // 对话UI管理器引用
        [System.NonSerialized]
        private DialogueUIManager uiManager;
        
        // 对话是否处于活动状态
        [System.NonSerialized]
        private bool isDialogueActive = false;
        
        // 持久化键前缀
        private const string SAVE_KEY = "DialogueGraph_";

        public PlayableDirector timelineDirector; // 在Inspector中拖入

        // 开始对话
        public void StartDialogue(DialogueUIManager uiManager)
        {
            this.uiManager = uiManager;
            
            // 校验startNode
            if (startNode == null)
            {
                Debug.LogError($"[DialogueGraph] 对话图错误：没有指定起始节点！图名称: {name}");
                return;
            }
            
            // 检查起始节点是否为一次性对话且已完成
            if (startNode is DialogueNode dialogueStartNode && 
                dialogueStartNode.isOneTimeDialogue && 
                IsDialogueCompleted(dialogueStartNode))
            {
                LogDebug($"起始节点 {dialogueStartNode.name} 是一次性对话且已完成，不再触发");
                
                // 不要在这里调用OnDialogueEnded，而是返回一个状态让调用者处理
                return;
            }

            // 重置对话状态
            isDialogueActive = true;

            LogDebug($"开始对话: {name}");
            
            // 设置当前节点为起始节点
            currentNode = startNode;
            
            // 通知UI管理器
            if (uiManager != null)
            {
                uiManager.SetDialogueGraph(this);
                
                // 直接处理当前节点
                LogDebug($"处理起始节点: {currentNode.name}");
                
                // 直接处理起始节点，确保显示内容
                ProcessCurrentNode();
            }
            else
            {
                Debug.LogError($"[DialogueGraph] 对话图错误：UI管理器为空！图名称: {name}");
                isDialogueActive = false; // 设置对话为非活动状态
            }
        }

        // 处理当前节点
        public void ProcessCurrentNode()
        {
            if (!isDialogueActive)
            {
                LogDebug("对话已结束，不处理节点");
                return;
            }
            
            if (currentNode == null)
            {
                LogDebug("当前节点为空，结束对话");
                EndDialogue();
                return;
            }
            
            try
            {
                // 检查当前节点是否是结束节点，并通知UI管理器
                bool isEndNode = currentNode.IsEndNode();
                
                // 所有节点都视为需要用户输入才能继续
                // 只有真正的结束节点才会在点击空格后结束对话
                if (uiManager != null)
                {
                    // 如果是真正的结束节点，设置为结束节点，否则设置为普通节点
                    uiManager.SetCurrentNodeAsEndNode(isEndNode);
                }
                
                // 调用当前节点的处理方法，显示节点内容
                LogDebug($"处理节点: {currentNode.name}, 是否是结束节点: {isEndNode}");
                currentNode.ProcessNode(uiManager, this);
                
                // 如果是DialogueNode类型，并且设置了一次性对话，将其标记为已完成
                if (currentNode is DialogueNode dialogNode && dialogNode.isOneTimeDialogue)
                {
                    MarkDialogueAsCompleted(dialogNode);
                }
                
                // 不再自动处理下一个节点，而是等待用户输入
                // 所有节点都需要用户点击空格才能继续
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DialogueGraph] 处理节点出错: {ex.Message}");
                EndDialogue();
            }
        }
        
        // 检查对话是否已完成
        public bool IsDialogueCompleted(DialogueNode node)
        {
            if (!saveCompletedDialogues) return false;
            
            string key = GetSaveKey(node.name, node.GetInstanceID());
            return PlayerPrefs.GetInt(key, 0) == 1;
        }
        
        // 标记对话为已完成
        public void MarkDialogueAsCompleted(DialogueNode node)
        {
            if (!saveCompletedDialogues) return;
            
            string key = GetSaveKey(node.name, node.GetInstanceID());
            if (PlayerPrefs.GetInt(key, 0) != 1)
            {
                LogDebug($"标记节点 {node.name} 为已完成的一次性对话");
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
        }
        
        // 生成存储键
        private string GetSaveKey(string dialogueNodeName, int nodeId)
        {
            return $"{SAVE_KEY}{name}_{dialogueNodeName}_{nodeId}";
        }
        
        // 重置特定对话节点
        public void ResetDialogueNode(DialogueNode node)
        {
            string key = GetSaveKey(node.name, node.GetInstanceID());
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                LogDebug($"已重置节点 {node.name} 的对话状态");
            }
        }
        
        // 处理下一个节点
        public void ProcessNextNode()
        {
            if (!isDialogueActive)
            {
                LogDebug("对话已结束，不处理下一节点");
                return;
            }
            
            if (currentNode == null)
            {
                LogDebug("当前节点为空，结束对话");
                EndDialogue();
                return;
            }
            
            // 获取下一个节点
            BaseNode nextNode = currentNode.GetNextNode();
            
            if (nextNode != null)
            {
                // 设置新的当前节点
                currentNode = nextNode;
                
                // 通知UI管理器节点已变化
                if (uiManager != null)
                {
                    uiManager.HandleNodeChanged(currentNode);
                }
                
                // 处理新节点 - 仅显示内容，不自动链式处理
                ProcessCurrentNode();
            }
            else
            {
                // 如果没有下一个节点，结束对话
                LogDebug("没有下一个节点，结束对话");
                EndDialogue();
            }
        }
        
        // 手动前进对话（适用于单一输出的节点）
        public void Next()
        {
            if (!isDialogueActive)
            {
                LogDebug("对话已结束，无法前进");
                return;
            }
            
            if (currentNode == null)
            {
                LogDebug("当前节点为空，无法前进");
                EndDialogue();
                return;
            }
            
            // 检查当前节点是否是结束节点，如果是则直接结束
            if (currentNode.IsEndNode())
            {
                LogDebug($"当前是结束节点 {currentNode.name}，Next()结束对话");
                EndDialogue();
                return;
            }
            
            // 检查当前节点是否是选项节点，如果是则不响应next
            if (currentNode is DialogueNode dialogNode && dialogNode.dialogueType == DialogueNode.DialogueType.Choice)
            {
                LogDebug("当前是选项对话节点，需要用户选择，忽略Next调用");
                return;
            }
            
            LogDebug("手动触发Next()，处理下一节点");
            ProcessNextNode();
        }
        
        // 结束对话
        public void EndDialogue()
        {
            LogDebug("结束对话");
            isDialogueActive = false;
            
            // 通知UI管理器关闭对话框
            if (uiManager != null)
            {
                uiManager.OnDialogueEnded();
            }
            
            // 清除引用
            currentNode = null;

            if (timelineDirector != null)
                timelineDirector.Resume();
        }
        
        // 重置所有对话节点
        [ContextMenu("重置所有一次性对话状态")]
        public void ResetAllOneTimeDialogues(bool showLog = true)
        {
            // 遍历所有节点
            foreach (Node node in nodes)
            {
                if (node is DialogueNode dialogueNode && dialogueNode.isOneTimeDialogue)
                {
                    ResetDialogueNode(dialogueNode);
                }
            }
            
            // 只有在showLog为true时才输出日志
            if (showLog)
            {
                Debug.Log($"[DialogueGraph] 已重置对话图 {name} 的所有一次性对话状态");
            }
        }
        
        // 验证整个对话图
        public bool ValidateDialogueGraph()
        {
            bool isValid = true;
            
            // 检查起始节点
            if (startNode == null)
            {
                Debug.LogError($"[DialogueGraph] 对话图验证失败：没有指定起始节点, 图名称: {name}");
                isValid = false;
            }
            
            // 验证所有节点的连接
            foreach (Node node in nodes)
            {
                if (node is DialogueNode dialogueNode)
                {
                    if (dialogueNode.dialogueType == DialogueNode.DialogueType.Simple)
                    {
                        // 检查简单对话节点
                        var outputPort = dialogueNode.GetOutputPort("output");
                        if (outputPort != null && !outputPort.IsConnected && !dialogueNode.IsEndNode())
                        {
                            Debug.LogWarning($"[DialogueGraph] 对话节点 '{dialogueNode.name}' 没有连接输出端口，但也不是结束节点");
                        }
                    }
                    else
                    {
                        // 检查选项对话节点
                        if (dialogueNode.options.Count == 0)
                        {
                            Debug.LogWarning($"[DialogueGraph] 选项对话节点 '{dialogueNode.name}' 没有选项");
                        }
                        
                        for (int i = 0; i < dialogueNode.options.Count; i++)
                        {
                            if (i >= dialogueNode.nextNodes.Count || dialogueNode.nextNodes[i] == null)
                            {
                                Debug.LogWarning($"[DialogueGraph] 选项对话节点 '{dialogueNode.name}' 的选项 {i+1} 没有连接目标节点");
                                isValid = false;
                            }
                        }
                    }
                }
                else if (node is ConditionNode conditionNode)
                {
                    var truePort = conditionNode.GetOutputPort("trueNode");
                    var falsePort = conditionNode.GetOutputPort("falseNode");
                    
                    if (truePort == null || !truePort.IsConnected)
                    {
                        Debug.LogWarning($"[DialogueGraph] 条件节点 '{conditionNode.name}' 的True分支没有连接");
                        isValid = false;
                    }
                    
                    if (falsePort == null || !falsePort.IsConnected)
                    {
                        Debug.LogWarning($"[DialogueGraph] 条件节点 '{conditionNode.name}' 的False分支没有连接");
                        isValid = false;
                    }
                }
                else if (node is EventNode eventNode)
                {
                    var outputPort = eventNode.GetOutputPort("output");
                    if (outputPort != null && !outputPort.IsConnected && !eventNode.isEndEvent)
                    {
                        Debug.LogWarning($"[DialogueGraph] 事件节点 '{eventNode.name}' 没有连接输出端口，但也不是结束事件");
                    }
                }
            }
            
            return isValid;
        }
        
        [ContextMenu("验证对话图")]
        private void ValidateGraph()
        {
            if (ValidateDialogueGraph())
            {
                Debug.Log($"[DialogueGraph] 对话图 '{name}' 验证通过！");
            }
            else
            {
                Debug.LogError($"[DialogueGraph] 对话图 '{name}' 验证失败！请修复上述警告。");
            }
        }
        
        // 调试日志
        private void LogDebug(string message)
        {
            if (logDebugInfo)
            {
                Debug.Log($"[DialogueGraph] {message}");
            }
        }
        
        // Unity编辑器下重置对话
        #if UNITY_EDITOR
        private void OnEnable()
        {
            if (resetDialoguesOnPlay && !Application.IsPlaying(this))
            {
                // 使用不显示日志的方式调用
                ResetAllOneTimeDialogues(false);
            }
            // UpdateAllNodeNames(); // 注释掉或删除
        }
        #endif
        
        // 更新所有节点的名称
        public void UpdateAllNodeNames()
        {
#if UNITY_EDITOR
            // 避免在资源导入或编译时执行
            if (UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorApplication.isCompiling)
                return;

            foreach (var node in nodes)
            {
                if (node is DialogueNode dialogueNode)
                {
                    dialogueNode.UpdateNodeName();
                    UnityEditor.EditorUtility.SetDirty(dialogueNode);
                }
                else if (node is EventNode eventNode)
                {
                    eventNode.UpdateNodeName();
                    UnityEditor.EditorUtility.SetDirty(eventNode);
                }
                else if (node is ConditionNode conditionNode)
                {
                    conditionNode.UpdateNodeName();
                    UnityEditor.EditorUtility.SetDirty(conditionNode);
                }
            }
            
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        // 添加公共方法来设置UIManager
        public void SetUIManager(DialogueUIManager manager)
        {
            this.uiManager = manager;
            LogDebug($"设置UIManager: {(manager != null ? "成功" : "失败")}");
        }

        // 获取当前UIManager
        public DialogueUIManager GetUIManager()
        {
            return uiManager;
        }

        // 准备对话内容但不显示 - 用于解决UI闪烁问题
        public void PrepareDialogue(DialogueUIManager uiManager)
        {
            this.uiManager = uiManager;
            
            // 校验startNode
            if (startNode == null)
            {
                Debug.LogError($"[DialogueGraph] 对话图错误：没有指定起始节点！图名称: {name}");
                return;
            }
            
            // 检查起始节点是否为一次性对话且已完成
            if (startNode is DialogueNode dialogueStartNode && 
                dialogueStartNode.isOneTimeDialogue && 
                IsDialogueCompleted(dialogueStartNode))
            {
                LogDebug($"起始节点 {dialogueStartNode.name} 是一次性对话且已完成，不再触发");
                return;
            }

            // 设置当前节点为起始节点
            currentNode = startNode;
            
            // 只设置图引用，不处理节点
            if (uiManager != null)
            {
                uiManager.SetDialogueGraph(this);
            }
            else
            {
                Debug.LogError($"[DialogueGraph] 对话图错误：UI管理器为空！图名称: {name}");
            }
            
            LogDebug($"对话内容已准备: {name}");
        }
    }
}