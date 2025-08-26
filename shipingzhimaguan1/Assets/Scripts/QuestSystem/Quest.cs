using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    [TextArea(3, 10)]
    public string questDescription;
    
    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed
    }
    
    public QuestStatus status = QuestStatus.NotStarted;
    
    // 任务目标列表
    public List<QuestObjective> objectives = new List<QuestObjective>();
    
    // 是否需要完成所有目标才能完成任务
    public bool requireAllObjectives = true;
    
    // 前置任务 (必须先完成这些任务才能开始当前任务)
    public List<Quest> prerequisites = new List<Quest>();
    
    // 任务奖励
    [Serializable]
    public class QuestReward
    {
        public string rewardID;
        public string rewardName;
        public int rewardAmount = 1;
    }
    
    public List<QuestReward> rewards = new List<QuestReward>();
    
    // 初始化任务目标
    public void InitializeObjectives()
    {
        foreach (var objective in objectives)
        {
            objective.Initialize();
            
            // 订阅目标完成事件
            objective.OnObjectiveCompleted += OnObjectiveCompleted;
        }
    }
    
    // 当任何一个目标完成时检查任务是否应该完成
    private void OnObjectiveCompleted(QuestObjective objective)
    {
        // 检查是否应该完成整个任务
        CheckCompletion();
    }
    
    // 检查任务是否完成
    public bool CheckCompletion()
    {
        if (status != QuestStatus.Active) return false;
        
        bool shouldComplete = false;
        
        if (requireAllObjectives)
        {
            // 需要完成所有非可选目标
            shouldComplete = true;
            foreach (var objective in objectives)
            {
                if (!objective.isCompleted && !objective.isOptional)
                {
                    shouldComplete = false;
                    break;
                }
            }
        }
        else
        {
            // 只需要完成任意一个目标
            foreach (var objective in objectives)
            {
                if (objective.isCompleted)
                {
                    shouldComplete = true;
                    break;
                }
            }
        }
        
        return shouldComplete;
    }
    
    // 获取任务进度百分比
    public float GetCompletionPercentage()
    {
        if (objectives.Count == 0) return 0;
        
        int completedCount = 0;
        int totalRequired = 0;
        
        foreach (var objective in objectives)
        {
            if (!objective.isOptional)
            {
                totalRequired++;
                if (objective.isCompleted)
                {
                    completedCount++;
                }
            }
        }
        
        return totalRequired > 0 ? (float)completedCount / totalRequired : 0;
    }
    
    // 检查是否可以开始此任务 (所有前置任务已完成)
    public bool CanBeStarted()
    {
        foreach (var prereq in prerequisites)
        {
            if (prereq.status != QuestStatus.Completed)
            {
                return false;
            }
        }
        return true;
    }
}