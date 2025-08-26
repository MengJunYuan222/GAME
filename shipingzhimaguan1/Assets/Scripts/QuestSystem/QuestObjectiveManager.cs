using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 负责管理任务目标的更新和完成
public class QuestObjectiveManager : MonoBehaviour
{
    // 单例模式
    public static QuestObjectiveManager Instance { get; private set; }

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
    }

    // 更新对话类目标
    public void CompleteDialogueObjective(string dialogueID)
    {
        // 遍历所有活跃任务中的对话类目标
        if (QuestManager.Instance != null)
        {
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective is DialogueObjective dialogueObjective && 
                        dialogueObjective.dialogueID == dialogueID &&
                        !dialogueObjective.isCompleted)
                    {
                        dialogueObjective.CompleteObjective();
                        CheckQuestCompletion(quest);
                    }
                }
            }
        }
    }

    // 更新收集物品类目标
    public void UpdateItemCollectionObjective(string itemID, int amount = 1)
    {
        if (QuestManager.Instance != null)
        {
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective is CollectItemObjective collectObjective && 
                        collectObjective.itemID == itemID &&
                        !collectObjective.isCompleted)
                    {
                        collectObjective.UpdateProgress(amount);
                        CheckQuestCompletion(quest);
                    }
                }
            }
        }
    }

    // 更新击败敌人类目标
    public void UpdateDefeatEnemyObjective(string enemyID, int amount = 1)
    {
        if (QuestManager.Instance != null)
        {
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective is DefeatEnemyObjective defeatObjective && 
                        defeatObjective.enemyID == enemyID &&
                        !defeatObjective.isCompleted)
                    {
                        defeatObjective.UpdateProgress(amount);
                        CheckQuestCompletion(quest);
                    }
                }
            }
        }
    }

    // 更新到达位置类目标
    public void CompleteLocationObjective(string locationID)
    {
        if (QuestManager.Instance != null)
        {
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective is LocationObjective locationObjective && 
                        locationObjective.locationID == locationID &&
                        !locationObjective.isCompleted)
                    {
                        locationObjective.CompleteObjective();
                        CheckQuestCompletion(quest);
                    }
                }
            }
        }
    }

    // 更新交互类目标
    public void CompleteInteractObjective(string interactableID)
    {
        if (QuestManager.Instance != null)
        {
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective is InteractObjective interactObjective && 
                        interactObjective.interactableID == interactableID &&
                        !interactObjective.isCompleted)
                    {
                        interactObjective.CompleteObjective();
                        CheckQuestCompletion(quest);
                    }
                }
            }
        }
    }

    // 检查任务是否应该完成
    private void CheckQuestCompletion(Quest quest)
    {
        // 如果任务已完成所有必要目标，自动完成任务
        if (quest.CheckCompletion())
        {
            QuestManager.Instance.CompleteQuest(quest.questID);
        }
    }
}