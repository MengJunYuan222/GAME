using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class QuestObjective
{
    public string objectiveID;
    public string description;
    public bool isCompleted;
    public bool isOptional;

    // 当前进度和目标值
    public int currentAmount;
    public int requiredAmount = 1;

    public event Action<QuestObjective> OnObjectiveCompleted;
    public event Action<QuestObjective> OnObjectiveUpdated;

    // 初始化目标
    public virtual void Initialize()
    {
        isCompleted = false;
        currentAmount = 0;
    }

    // 更新目标进度
    public virtual void UpdateProgress(int amount)
    {
        if (isCompleted) return;

        currentAmount += amount;
        OnObjectiveUpdated?.Invoke(this);

        // 检查是否完成
        if (currentAmount >= requiredAmount)
        {
            CompleteObjective();
        }
    }

    // 完成目标
    public virtual void CompleteObjective()
    {
        if (isCompleted) return;

        isCompleted = true;
        currentAmount = requiredAmount;
        OnObjectiveCompleted?.Invoke(this);
    }

    // 获取进度文本
    public virtual string GetProgressText()
    {
        return $"{description}: {currentAmount}/{requiredAmount}";
    }
}

// 对话目标 - 完成特定对话
[Serializable]
public class DialogueObjective : QuestObjective
{
    public string dialogueID; // 需要完成的对话ID

    public override string GetProgressText()
    {
        return isCompleted ? 
            $"{description}: 已完成" : 
            $"{description}: 未完成";
    }
}

// 收集物品目标
[Serializable]
public class CollectItemObjective : QuestObjective
{
    public string itemID; // 需要收集的物品ID
}

// 击败敌人目标
[Serializable]
public class DefeatEnemyObjective : QuestObjective
{
    public string enemyID; // 需要击败的敌人类型
}

// 到达位置目标
[Serializable]
public class LocationObjective : QuestObjective
{
    public string locationID; // 需要到达的位置ID
}

// 交互目标 - 与物体交互
[Serializable]
public class InteractObjective : QuestObjective
{
    public string interactableID; // 需要交互的物体ID
}