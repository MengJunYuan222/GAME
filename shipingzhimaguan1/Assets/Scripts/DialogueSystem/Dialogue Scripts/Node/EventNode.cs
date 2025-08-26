using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using UnityEngine.Events;

namespace DialogueSystem
{
    /// <summary>
    /// 事件节点 - 执行游戏事件，不显示对话，自动进入下一节点
    /// </summary>
    public class EventNode : BaseNode
    {
        // 使用基类中的baseInput作为输入端口
        [Output] public BaseNode output;
        
        [Header("事件设置")]
        [Tooltip("勾选此项表示事件执行后结束对话")]
        public bool isEndEvent = false;
        
        [Header("事件类型")]
        [Tooltip("选择要执行的事件类型")]
        public EventType eventType = EventType.None;
        
        // 事件类型枚举
        public enum EventType
        {
            None,           // 无事件：不执行任何操作
            GiveItem,       // 给予物品：将指定的物品添加到玩家背包
            PlaySound,      // 播放音效：播放一个音效，增强游戏体验
            QuestEvent,     // 任务事件：更新或完成任务目标/任务
            Custom          // 自定义事件：使用Unity事件系统执行自定义功能
        }
        
        // 特定事件类型的设置
        [Header("物品设置 (GiveItem)")]
        [Tooltip("直接拖拽物品SO对象到此处")]
        public ItemSO itemSO; // 要给予玩家的物品对象
        
        [Header("音效设置 (PlaySound)")]
        [Tooltip("音效片段 - 拖拽音效文件到此处")]
        public AudioClip soundClip;
        [Tooltip("音效路径 - 从Resources加载，如'Audio/SFX/sound1'")]
        public string soundPath;
        [Range(0f, 1f)]
        [Tooltip("音量大小 - 0为静音，1为最大音量")]
        public float volume = 1f;
        
        [Header("任务设置 (QuestEvent)")]
        [Tooltip("选择任务操作类型")]
        public QuestActionType questActionType = QuestActionType.CompleteObjective;
        // 任务操作类型枚举
        public enum QuestActionType
        {
            CompleteObjective,    // 完成任务目标
            AcceptQuest,          // 接受任务
            CompleteQuest,        // 完成整个任务
            FailQuest             // 失败任务
        }
        
        [Header("任务引用 (直接使用SO对象)")]
        [Tooltip("直接拖拽Quest任务SO对象")]
        public Quest questSO;
        
        [Header("目标与任务ID (当没有SO引用时使用)")]
        [Tooltip("任务目标ID - 与任务系统中的目标ID一致")]
        public string questObjectiveID = "";
        [Tooltip("任务ID - 与任务系统中的任务ID一致")]
        public string questID = "";
        
        [Header("自定义事件 (Custom)")]
        [Tooltip("在Inspector中点击+添加函数，可调用任意对象的公共方法")]
        public UnityEvent onEventTriggered;
        
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
            base.OnCreateConnection(from, to);
            
            // 连接后确保名称唯一
            UpdateNodeName();
        }
        
        // 更新节点名称，确保唯一性
        public override void UpdateNodeName()
        {
            // 基础名称使用事件类型
            string baseName = "Event";
            
            // 根据事件类型细化名称
            switch (eventType)
            {
                case EventType.GiveItem:
                    if (itemSO != null)
                        baseName = $"Event_给{itemSO.itemName}";
                    else
                        baseName = "Event_给物品";
                    break;
                case EventType.PlaySound:
                    if (soundClip != null)
                        baseName = $"Event_播放{soundClip.name}";
                    else if (!string.IsNullOrEmpty(soundPath))
                        baseName = $"Event_播放{soundPath}";
                    else
                        baseName = "Event_播放音效";
                    break;
                case EventType.QuestEvent:
                    switch (questActionType)
                    {
                        case QuestActionType.CompleteObjective:
                            if (questSO != null)
                                baseName = $"Event_完成目标_{questSO.questName}";
                            else
                                baseName = $"Event_完成目标_{questObjectiveID}";
                            break;
                        case QuestActionType.AcceptQuest:
                            if (questSO != null)
                                baseName = $"Event_接受任务_{questSO.questName}";
                            else
                                baseName = $"Event_接受任务_{questID}";
                            break;
                        case QuestActionType.CompleteQuest:
                            if (questSO != null)
                                baseName = $"Event_完成任务_{questSO.questName}";
                            else
                                baseName = $"Event_完成任务_{questID}";
                            break;
                        case QuestActionType.FailQuest:
                            if (questSO != null)
                                baseName = $"Event_任务失败_{questSO.questName}";
                            else
                                baseName = $"Event_任务失败_{questID}";
                            break;
                    }
                    break;
                case EventType.Custom:
                    baseName = "Event_自定义";
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
        
        // 处理节点 - 执行事件
        public override void ProcessNode(DialogueUIManager uiManager, DialogueNodeGraph graph)
        {
            // 执行事件
            ExecuteEvent();
            
            // 事件节点不显示UI，立即处理下一个节点
            if (graph != null)
            {
                graph.ProcessNextNode();
            }
        }
        
        // 执行事件
        private void ExecuteEvent()
        {
            Debug.Log($"执行事件类型: {eventType}");
            
            // 根据事件类型执行不同的操作
            switch (eventType)
            {
                case EventType.GiveItem:
                    GiveItem();
                    break;
                case EventType.PlaySound:
                    PlaySound();
                    break;
                case EventType.QuestEvent:
                    ExecuteQuestEvent();
                    break;
                case EventType.Custom:
                    // 使用Unity事件系统执行自定义事件
                    onEventTriggered?.Invoke();
                    break;
            }
        }
        
        // 给予物品：将物品SO添加到玩家背包
        private void GiveItem()
        {
            if (itemSO != null)
            {
                Debug.Log($"尝试添加物品: {itemSO.itemName}");
                if (InventoryManager.Instance == null)
                {
                    Debug.LogError("InventoryManager实例不存在！");
                    return;
                }
                bool added = InventoryManager.Instance.AddItemByItemSO(itemSO);
                Debug.Log($"物品添加{(added ? "成功" : "失败")}");
            }
            else
            {
                Debug.LogWarning("没有指定物品SO，无法添加物品");
            }
        }
        
        // 播放音效：通过AudioManager播放或创建临时音频源播放声音
        private void PlaySound()
        {
            // 首先尝试使用AudioManager播放
            if (AudioManager.Instance != null)
            {
                if (soundClip != null)
                {
                    // 使用直接引用的音频剪辑
                    AudioManager.Instance.PlaySFX(soundClip, volume);
                    Debug.Log($"通过AudioManager播放音效: {soundClip.name}，音量: {volume}");
                    return;
                }
                else if (!string.IsNullOrEmpty(soundPath))
                {
                    // 通过路径加载并播放
                    AudioClip loadedClip = Resources.Load<AudioClip>(soundPath);
                    if (loadedClip != null)
                    {
                        AudioManager.Instance.PlaySFX(loadedClip, volume);
                        Debug.Log($"通过AudioManager使用路径播放音效: {soundPath}，音量: {volume}");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"无法从路径加载音效: {soundPath}");
                    }
                }
            }
            
            // 如果AudioManager不可用或加载失败，使用临时音频源
            if (soundClip != null)
            {
                Debug.Log($"使用临时音频源播放音效: {soundClip.name}，音量: {volume}");
                
                // 创建临时音频源播放声音
                GameObject audioObj = new GameObject("TempAudio");
                AudioSource audioSource = audioObj.AddComponent<AudioSource>();
                audioSource.clip = soundClip;
                audioSource.volume = volume;
                audioSource.Play();
                
                // 声音播放完毕后销毁对象
                Object.Destroy(audioObj, soundClip.length);
            }
            else if (!string.IsNullOrEmpty(soundPath))
            {
                // 尝试从Resources加载
                AudioClip loadedClip = Resources.Load<AudioClip>(soundPath);
                if (loadedClip != null)
                {
                    Debug.Log($"使用临时音频源通过路径播放音效: {soundPath}，音量: {volume}");
                    
                    // 创建临时音频源播放声音
                    GameObject audioObj = new GameObject("TempAudio");
                    AudioSource audioSource = audioObj.AddComponent<AudioSource>();
                    audioSource.clip = loadedClip;
                    audioSource.volume = volume;
                    audioSource.Play();
                    
                    // 声音播放完毕后销毁对象
                    Object.Destroy(audioObj, loadedClip.length);
                }
                else
                {
                    Debug.LogWarning($"无法从路径加载音效: {soundPath}");
                }
            }
            else
            {
                Debug.LogWarning("没有指定音效或音效路径");
            }
        }
        
        // 获取下一个节点
        public override BaseNode GetNextNode()
        {
            var port = GetOutputPort("output");
            if (port != null && port.IsConnected)
            {
                return port.Connection.node as BaseNode;
            }
            return null;
        }
        
        // 重写IsEndNode方法
        public override bool IsEndNode()
        {
            return isEndEvent;
        }
        
        // 获取输出端口值
        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "output")
            {
                return this;
            }
            return null;
        }
        
        // 执行任务事件：完成任务目标或处理任务
        private void ExecuteQuestEvent()
        {
            // 检查任务管理器实例
            bool questManagerExists = (QuestManager.Instance != null);
            bool objectiveManagerExists = (QuestObjectiveManager.Instance != null);
            
            // 获取有效的任务ID
            string effectiveQuestID = questSO != null ? questSO.questID : questID;
            
            switch (questActionType)
            {
                case QuestActionType.CompleteObjective:
                    // 处理完成任务目标
                    if (objectiveManagerExists)
                    {
                        if (!string.IsNullOrEmpty(questObjectiveID))
                        {
                            QuestObjectiveManager.Instance.CompleteDialogueObjective(questObjectiveID);
                            Debug.Log($"对话事件触发了任务目标完成: {questObjectiveID}");
                        }
                        else if (questSO != null)
                        {
                            // 如果有Quest SO但没有特定目标ID，则尝试完成该任务的第一个对话目标
                            bool foundObjective = false;
                            foreach (var objective in questSO.objectives)
                            {
                                if (objective is DialogueObjective dialogueObjective && !dialogueObjective.isCompleted)
                                {
                                    QuestObjectiveManager.Instance.CompleteDialogueObjective(dialogueObjective.objectiveID);
                                    Debug.Log($"对话事件触发了任务目标完成: {dialogueObjective.objectiveID}");
                                    foundObjective = true;
                                    break;
                                }
                            }
                            
                            if (!foundObjective)
                            {
                                Debug.LogWarning($"任务 {questSO.questName} 中没有找到可完成的对话目标");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("未指定任务目标ID或任务SO");
                        }
                    }
                    else
                    {
                        Debug.LogError("QuestObjectiveManager实例不存在！无法完成任务目标");
                    }
                    break;
                    
                case QuestActionType.AcceptQuest:
                    // 处理接受任务
                    if (questManagerExists)
                    {
                        if (questSO != null)
                        {
                            // 使用直接SO引用方式 - 优先
                            QuestManager.Instance.AcceptQuestDirectly(questSO);
                            Debug.Log($"对话事件触发了接受任务: {questSO.questName}");
                        }
                        else if (!string.IsNullOrEmpty(questID))
                        {
                            // 兼容旧方式 - ID查找
                            QuestManager.Instance.AcceptQuest(questID);
                            Debug.Log($"对话事件触发了接受任务: {questID}");
                        }
                        else
                        {
                            Debug.LogWarning("未指定任务ID或任务SO");
                        }
                    }
                    else
                    {
                        Debug.LogError("QuestManager实例不存在！无法接受任务");
                    }
                    break;
                    
                case QuestActionType.CompleteQuest:
                    // 处理完成任务
                    if (questManagerExists && !string.IsNullOrEmpty(effectiveQuestID))
                    {
                        QuestManager.Instance.CompleteQuest(effectiveQuestID);
                        Debug.Log($"对话事件触发了完成任务: {(questSO != null ? questSO.questName : effectiveQuestID)}");
                    }
                    else if (!questManagerExists)
                    {
                        Debug.LogError("QuestManager实例不存在！无法完成任务");
                    }
                    else
                    {
                        Debug.LogWarning("未指定任务ID或任务SO");
                    }
                    break;
                    
                case QuestActionType.FailQuest:
                    // 处理任务失败
                    if (questManagerExists && !string.IsNullOrEmpty(effectiveQuestID))
                    {
                        QuestManager.Instance.FailQuest(effectiveQuestID);
                        Debug.Log($"对话事件触发了任务失败: {(questSO != null ? questSO.questName : effectiveQuestID)}");
                    }
                    else if (!questManagerExists)
                    {
                        Debug.LogError("QuestManager实例不存在！无法标记任务失败");
                    }
                    else
                    {
                        Debug.LogWarning("未指定任务ID或任务SO");
                    }
                    break;
            }
        }
    }
} 