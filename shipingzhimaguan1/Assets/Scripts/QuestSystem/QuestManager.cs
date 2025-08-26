using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class QuestManager : MonoBehaviour
{
    // 单例模式
    public static QuestManager Instance { get; private set; }
    
    // 所有任务的字典
    private Dictionary<string, Quest> allQuests = new Dictionary<string, Quest>();
    
    // 当前活跃任务列表
    public List<Quest> activeQuests = new List<Quest>();
    
    // 已完成任务列表
    public List<Quest> completedQuests = new List<Quest>();
    
    // 事件
    public event Action<Quest> OnQuestAccepted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestFailed;
    public event Action<Quest> OnQuestUpdated;
    
    private void Awake()
    {
        // 单例实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 初始化任务系统
        InitializeQuests();
    }
    
    private void Start()
    {
        // 确保Inspector中手动添加的任务状态正确
        foreach (Quest quest in activeQuests)
        {
            if (quest != null && quest.status != Quest.QuestStatus.Active)
            {
                quest.status = Quest.QuestStatus.Active;
            }
            
            // 初始化任务目标
            quest.InitializeObjectives();
            
            // 通知UI系统
            OnQuestUpdated?.Invoke(quest);
        }
    }
    
    // 初始化所有任务
    private void InitializeQuests()
    {
        // 加载所有任务的ScriptableObject
        Quest[] questObjects = Resources.LoadAll<Quest>("Quests");
        
        foreach (Quest quest in questObjects)
        {
            allQuests.Add(quest.questID, quest);
        }
    }
    
    // 接受任务 (通过ID)
    public void AcceptQuest(string questID)
    {
        Debug.Log($"[QuestManager] AcceptQuest被调用: questID = {questID}");
        
        if (allQuests.TryGetValue(questID, out Quest quest))
        {
            Debug.Log($"[QuestManager] 找到任务: {quest.questName}, 当前状态: {quest.status}");
            
            if (quest.status == Quest.QuestStatus.NotStarted && quest.CanBeStarted())
            {
                Debug.Log($"[QuestManager] 任务 {quest.questName} 符合接受条件，准备添加到活跃任务");
                
                quest.status = Quest.QuestStatus.Active;
                quest.InitializeObjectives();
                activeQuests.Add(quest);
                
                Debug.Log($"[QuestManager] 准备触发任务接受事件，事件订阅者数量: {(OnQuestAccepted == null ? 0 : OnQuestAccepted.GetInvocationList().Length)}");
                OnQuestAccepted?.Invoke(quest);
                
                Debug.Log($"[QuestManager] 准备触发任务更新事件，事件订阅者数量: {(OnQuestUpdated == null ? 0 : OnQuestUpdated.GetInvocationList().Length)}");
                OnQuestUpdated?.Invoke(quest);
                
                Debug.Log($"[QuestManager] 任务 {quest.questName} 已成功添加到活跃任务列表，当前活跃任务数量: {activeQuests.Count}");
            }
            else if (!quest.CanBeStarted())
            {
                Debug.LogWarning($"[QuestManager] 无法接受任务 {quest.questName}：前置任务未完成");
            }
            else
            {
                Debug.LogWarning($"[QuestManager] 无法接受任务 {quest.questName}：当前状态 {quest.status} 不允许接受");
            }
        }
        else
        {
            Debug.LogError($"[QuestManager] 未找到ID为 {questID} 的任务，请检查任务ID是否正确");
        }
    }
    
    // 直接接受任务 (通过SO对象)
    public void AcceptQuestDirectly(Quest questSO)
    {
        if (questSO == null)
        {
            Debug.LogError("[QuestManager] AcceptQuestDirectly: 传入的任务SO为null");
            return;
        }
        
        Debug.Log($"[QuestManager] AcceptQuestDirectly被调用: {questSO.questName}");
        
        // 如有必要，先注册到字典
        if (!allQuests.ContainsKey(questSO.questID))
        {
            Debug.Log($"[QuestManager] 任务 {questSO.questName} 不在管理器中，正在添加...");
            allQuests.Add(questSO.questID, questSO);
        }
        
        // 检查任务状态
        if (questSO.status == Quest.QuestStatus.NotStarted && questSO.CanBeStarted())
        {
            Debug.Log($"[QuestManager] 任务 {questSO.questName} 符合接受条件，准备添加到活跃任务");
            
            questSO.status = Quest.QuestStatus.Active;
            questSO.InitializeObjectives();
            activeQuests.Add(questSO);
            
            Debug.Log($"[QuestManager] 准备触发任务接受事件，事件订阅者数量: {(OnQuestAccepted == null ? 0 : OnQuestAccepted.GetInvocationList().Length)}");
            OnQuestAccepted?.Invoke(questSO);
            
            Debug.Log($"[QuestManager] 准备触发任务更新事件，事件订阅者数量: {(OnQuestUpdated == null ? 0 : OnQuestUpdated.GetInvocationList().Length)}");
            OnQuestUpdated?.Invoke(questSO);
            
            Debug.Log($"[QuestManager] 任务 {questSO.questName} 已成功添加到活跃任务列表，当前活跃任务数量: {activeQuests.Count}");
        }
        else if (!questSO.CanBeStarted())
        {
            Debug.LogWarning($"[QuestManager] 无法接受任务 {questSO.questName}：前置任务未完成");
        }
        else
        {
            Debug.LogWarning($"[QuestManager] 无法接受任务 {questSO.questName}：当前状态 {questSO.status} 不允许接受");
        }
    }
    
    // 完成任务
    public void CompleteQuest(string questID)
    {
        if (allQuests.TryGetValue(questID, out Quest quest))
        {
            if (quest.status == Quest.QuestStatus.Active)
            {
                // 检查是否所有必要目标都已完成
                if (quest.CheckCompletion())
                {
                    quest.status = Quest.QuestStatus.Completed;
                    activeQuests.Remove(quest);
                    completedQuests.Add(quest);
                    OnQuestCompleted?.Invoke(quest);
                    OnQuestUpdated?.Invoke(quest);
                    
                    // 处理任务奖励
                    GiveQuestRewards(quest);
                }
                else
                {
                    Debug.LogWarning($"无法完成任务 {quest.questName}：未满足所有完成条件");
                }
            }
        }
    }
    
    // 给予任务奖励
    private void GiveQuestRewards(Quest quest)
    {
        foreach (var reward in quest.rewards)
        {
            Debug.Log($"获得奖励: {reward.rewardName} x{reward.rewardAmount}");
            // 这里可以实现将奖励添加到玩家背包等功能
        }
    }
    
    // 失败任务
    public void FailQuest(string questID)
    {
        if (allQuests.TryGetValue(questID, out Quest quest))
        {
            if (quest.status == Quest.QuestStatus.Active)
            {
                quest.status = Quest.QuestStatus.Failed;
                activeQuests.Remove(quest);
                OnQuestFailed?.Invoke(quest);
                OnQuestUpdated?.Invoke(quest);
            }
        }
    }
    
    // 获取任务状态
    public Quest.QuestStatus GetQuestStatus(string questID)
    {
        if (allQuests.TryGetValue(questID, out Quest quest))
        {
            return quest.status;
        }
        
        return Quest.QuestStatus.NotStarted;
    }
}