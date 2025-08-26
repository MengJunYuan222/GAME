using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string questID;
    
    // 这里可以扩展不同类型的任务触发器
    // 比如进入区域触发、与NPC对话触发、收集物品触发等
    
    // 基础触发方法
    public void TriggerQuest()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AcceptQuest(questID);
        }
        else
        {
            Debug.LogError("QuestManager not found in the scene!");
        }
    }
}