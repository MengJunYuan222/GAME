using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    
    private void Start()
    {
        // 订阅任务变更事件
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += UpdateQuestDisplay;
            QuestManager.Instance.OnQuestCompleted += UpdateQuestDisplay;
            QuestManager.Instance.OnQuestFailed += UpdateQuestDisplay;
            QuestManager.Instance.OnQuestUpdated += UpdateQuestDisplay;
            
            // 检查是否有活跃任务，如果有则显示第一个
            if (QuestManager.Instance.activeQuests.Count > 0)
            {
                ShowQuestPanel(QuestManager.Instance.activeQuests[0]);
            }
            else
            {
                // 初始时隐藏面板
                HideQuestPanel();
            }
        }
        else
        {
            // 初始时隐藏面板
            HideQuestPanel();
        }
    }
    
    // 当任务状态变化时更新UI
    private void UpdateQuestDisplay(Quest quest)
    {
        // 这里可以根据当前任务状态更新UI显示
        if (quest.status == Quest.QuestStatus.Active)
        {
            ShowQuestPanel(quest);
        }
        else if (quest.status == Quest.QuestStatus.Completed)
        {
            // 可以显示完成动画或其他效果
            StartCoroutine(ShowCompletionMessage(quest));
        }
    }
    
    // 显示任务面板
    public void ShowQuestPanel(Quest quest)
    {
        questPanel.SetActive(true);
        questNameText.text = quest.questName;
        questDescriptionText.text = quest.questDescription;
    }
    
    // 隐藏任务面板
    public void HideQuestPanel()
    {
        questPanel.SetActive(false);
    }
    
    // 显示任务完成消息
    private IEnumerator ShowCompletionMessage(Quest quest)
    {
        // 这里可以添加任务完成的UI效果
        questNameText.text = quest.questName + " - 已完成!";
        
        yield return new WaitForSeconds(3f);
        
        // 如果没有其他活跃任务，隐藏面板
        if (QuestManager.Instance.activeQuests.Count == 0)
        {
            HideQuestPanel();
        }
        else
        {
            // 显示下一个活跃任务
            ShowQuestPanel(QuestManager.Instance.activeQuests[0]);
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= UpdateQuestDisplay;
            QuestManager.Instance.OnQuestCompleted -= UpdateQuestDisplay;
            QuestManager.Instance.OnQuestFailed -= UpdateQuestDisplay;
            QuestManager.Instance.OnQuestUpdated -= UpdateQuestDisplay;
        }
    }
}