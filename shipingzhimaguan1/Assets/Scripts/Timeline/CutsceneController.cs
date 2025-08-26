using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using DialogueSystem;

public class CutsceneController : MonoBehaviour
{
    public PlayableDirector director;

    // 添加变量记录暂停时的播放时间
    private double pauseTime = 0;
    private bool hasActivePause = false;

    // 对话序列相关定义
    [System.Serializable]
    public class DialogueSequence
    {
        public DialogueNodeGraph dialogue;
        public float timelinePositionAfter = -1f;  // 对话后Timeline应该跳到的位置，-1表示继续播放
        public string description; // 用于编辑器中标识对话
    }
    
    [Header("对话序列设置")]
    [SerializeField] private List<DialogueSequence> dialogueSequence = new List<DialogueSequence>();
    private int currentDialogueIndex = -1;
    private bool isPlayingSequence = false;

    private void Awake()
    {
        // 如果没有指定director，使用自身的PlayableDirector
        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }
        
        // 订阅对话完成事件
        DialogueTimelineController.OnDialogueCompleted += OnDialogueFinished;
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        DialogueTimelineController.OnDialogueCompleted -= OnDialogueFinished;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 供Signal Receiver调用
    public void PauseTimeline()
    {
        if (GetComponent<PlayableDirector>() != null)
        {
            PlayableDirector director = GetComponent<PlayableDirector>();
            
            // 记录当前时间点
            pauseTime = director.time;
            hasActivePause = true;
            
            // 暂停播放
            director.Pause();
            
            Debug.Log($"Timeline暂停，记录时间点: {pauseTime}");
            
            // 通知对话系统
            if (DialogueUIManager.Instance != null)
            {
                DialogueUIManager.Instance.SetActiveTimelineDirector(director);
            }
            
            if (TimelineDebugTool.currentDialogueDirector == null)
            {
                TimelineDebugTool.currentDialogueDirector = director;
            }
        }
    }

    // 修改恢复方法，先设置位置再播放
    public void ResumeTimeline()
    {
        if (GetComponent<PlayableDirector>() != null)
        {
            PlayableDirector director = GetComponent<PlayableDirector>();
            
            // 如果有记录的暂停点，先回到该时间点
            if (hasActivePause)
            {
                director.time = pauseTime;
                hasActivePause = false;
                Debug.Log($"恢复Timeline到记录的时间点: {pauseTime}");
            }
            
            director.Play();
            Debug.Log($"Timeline恢复播放");
        }
    }

    // 原HandleDialogueCompleted已不需要，由OnDialogueFinished替代
    private void HandleDialogueCompleted()
    {
        PlayableDirector director = GetComponent<PlayableDirector>();
        if (director != null && hasActivePause && !isPlayingSequence)
        {
            Debug.Log($"单对话完成，CutsceneController准备恢复Timeline到: {pauseTime}");
            ResumeTimeline();
        }
    }

    // 开始播放对话序列
    public void StartDialogueSequence()
    {
        // 如果没有对话序列或序列为空，则直接返回
        if (dialogueSequence == null || dialogueSequence.Count == 0)
        {
            Debug.LogWarning("尝试播放空的对话序列");
            return;
        }

        // 暂停Timeline
        if (director != null)
        {
            // 记录当前时间点
            pauseTime = director.time;
            hasActivePause = true;
            
            // 暂停播放
            director.Pause();
            Debug.Log($"对话序列开始，Timeline暂停，记录时间点: {pauseTime}");
        }

        // 重置序列索引并标记为开始播放序列
        currentDialogueIndex = 0;
        isPlayingSequence = true;

        // 开始第一段对话
        PlayCurrentDialogue();
    }

    // 播放当前对话
    private void PlayCurrentDialogue()
    {
        // 检查索引是否有效
        if (currentDialogueIndex < 0 || currentDialogueIndex >= dialogueSequence.Count)
        {
            Debug.LogError($"对话索引无效: {currentDialogueIndex}, 序列长度: {dialogueSequence.Count}");
            return;
        }

        // 获取当前对话
        DialogueNodeGraph currentDialogue = dialogueSequence[currentDialogueIndex].dialogue;
        if (currentDialogue == null)
        {
            Debug.LogError($"序列中索引 {currentDialogueIndex} 的对话为空");
            return;
        }

        // 使用DialogueUIManager播放对话
        if (DialogueUIManager.Instance != null)
        {
            Debug.Log($"开始播放序列中的对话 {currentDialogueIndex + 1}/{dialogueSequence.Count}: {dialogueSequence[currentDialogueIndex].description}");
            
            // 确保DialogueUIManager知道当前使用的Timeline
            DialogueUIManager.Instance.SetActiveTimelineDirector(director);
            
            // 开始对话
            DialogueUIManager.Instance.StartDialogue(currentDialogue);
        }
        else
        {
            Debug.LogError("DialogueUIManager.Instance为空，无法播放对话");
            isPlayingSequence = false;
        }
    }

    // 对话完成事件处理
    private void OnDialogueFinished()
    {
        // 如果不是在播放序列，使用原先的单对话完成逻辑
        if (!isPlayingSequence)
        {
            HandleDialogueCompleted();
            return;
        }

        Debug.Log($"对话序列中的对话 {currentDialogueIndex + 1}/{dialogueSequence.Count} 完成");

        // 获取当前对话的后续设置
        float nextPosition = dialogueSequence[currentDialogueIndex].timelinePositionAfter;

        // 前往下一个对话
        currentDialogueIndex++;

        // 检查是否还有下一段对话
        if (currentDialogueIndex < dialogueSequence.Count)
        {
            // 如果上一段对话设置了特定时间点，并且有效
            if (nextPosition >= 0 && director != null)
            {
                director.time = nextPosition;
                Debug.Log($"设置Timeline位置到: {nextPosition}");
            }

            // 播放下一段对话
            PlayCurrentDialogue();
        }
        else
        {
            // 所有对话都已完成
            isPlayingSequence = false;
            currentDialogueIndex = -1;

            // 如果有设置最后一段对话后的时间点
            if (nextPosition >= 0 && director != null)
            {
                director.time = nextPosition;
                Debug.Log($"对话序列完成，设置Timeline最终位置到: {nextPosition}");
            }
            else if (hasActivePause && director != null)
            {
                // 如果没有特定位置，使用记录的暂停位置
                director.time = pauseTime;
                Debug.Log($"对话序列完成，恢复Timeline到原暂停位置: {pauseTime}");
            }

            // 恢复Timeline播放
            hasActivePause = false;
            if (director != null)
            {
                director.Play();
                Debug.Log("对话序列完成，恢复Timeline播放");
            }
        }
    }

    // 供Signal Receiver调用的方法，触发完整对话序列
    public void TriggerDialogueSequence()
    {
        Debug.Log("信号触发对话序列");
        StartDialogueSequence();
    }

    // 供Signal Receiver调用，播放单个对话
    public void PlaySingleDialogue(DialogueNodeGraph dialogue)
    {
        if (dialogue == null)
        {
            Debug.LogError("尝试播放空对话");
            return;
        }

        // 暂停Timeline
        PauseTimeline();

        // 播放单个对话
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.StartDialogue(dialogue);
            Debug.Log("开始播放单个对话");
        }
    }

    // 控制物体的显示/隐藏
    public void SetObjectVisibility(GameObject targetObject, bool isVisible)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(isVisible);
            Debug.Log($"物体 {targetObject.name} 已{(isVisible ? "显示" : "隐藏")}");
        }
        else
        {
            Debug.LogError("SetObjectVisibility: 目标物体为空");
        }
    }

    // 供Signal Receiver直接调用的便捷方法
    public void ShowObject(GameObject targetObject)
    {
        SetObjectVisibility(targetObject, true);
    }

    // 供Signal Receiver直接调用的便捷方法  
    public void HideObject(GameObject targetObject)
    {
        SetObjectVisibility(targetObject, false);
    }
}
