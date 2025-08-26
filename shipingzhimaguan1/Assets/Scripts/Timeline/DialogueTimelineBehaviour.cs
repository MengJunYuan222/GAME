using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using DialogueSystem;

public class DialogueTimelineBehaviour : PlayableBehaviour
{
    [Header("对话设置")]
    public DialogueNodeGraph dialogueGraph;
    public DialogueSystem.ActorSO speaker;
    public string dialogueText;
    
    [Header("选项设置")]
    public bool hasOptions;
    public string[] options;
    
    [Header("Timeline控制")]
    public bool autoResumeTimeline = true;
    public double jumpToTime = -1; // -1表示继续播放
    
    [Header("UI效果")]
    public DialogueSystem.UIAnimType uiAnimation = DialogueSystem.UIAnimType.None;
    public string animationName = "";
    public bool waitForAnimation = false;
    
    [Header("音频设置")]
    public bool stopBackgroundMusic = false;
    public AudioClip voiceClip;

    private bool triggered = false;
    private PlayableDirector currentDirector;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        ProcessDialogue(DialogueUIManager.Instance, playable);
    }
    
    public void ProcessDialogue(DialogueUIManager uiManager, Playable playable)
    {
        if (triggered) return;
        
        // 获取Timeline Director
        if (currentDirector == null && playable.GetGraph().GetResolver() != null)
        {
            currentDirector = playable.GetGraph().GetResolver() as PlayableDirector;
        }

        if (uiManager != null && currentDirector != null)
        {
            // 设置当前活跃的Director
            uiManager.SetActiveTimelineDirector(currentDirector);
            
            Debug.Log($"[DialogueTimelineBehaviour] 触发对话，设置Timeline: {currentDirector.gameObject.name}");
            
            // 处理音频
            if (stopBackgroundMusic && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBackgroundMusic();
            }
            
            if (voiceClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVoice(voiceClip);
            }
            
            // 处理UI动画
            if (uiAnimation != DialogueSystem.UIAnimType.None && !string.IsNullOrEmpty(animationName))
            {
                if (waitForAnimation)
                {
                    // 等待动画完成后再显示对话
                    uiManager.PlayAnimation(uiAnimation, animationName, true);
                    // 动画完成后会自动调用ProcessNextNode，这时再显示对话
                    StartCoroutineWrapper(() => ShowDialogueOrOptionsDelayed(uiManager));
                }
                else
                {
                    // 播放动画的同时显示对话
                    uiManager.PlayAnimation(uiAnimation, animationName, false);
                    ShowDialogueOrOptions(uiManager);
                }
            }
            else
            {
                ShowDialogueOrOptions(uiManager);
            }
        }
        triggered = true;
    }
    
    private void StartCoroutineWrapper(System.Action action)
    {
        // 简单的延迟执行，可以根据需要调整
        if (action != null)
        {
            action.Invoke();
        }
    }
    
    private void ShowDialogueOrOptionsDelayed(DialogueUIManager uiManager)
    {
        // 延迟一帧确保动画已开始
        ShowDialogueOrOptions(uiManager);
    }

    private void ShowDialogueOrOptions(DialogueUIManager uiManager)
    {
        // 订阅对话结束事件
        uiManager.OnDialogueEndedEvent -= OnDialogueFinished;
        uiManager.OnDialogueEndedEvent += OnDialogueFinished;
        
        if (dialogueGraph != null)
        {
            // 使用对话图
            uiManager.StartDialogue(dialogueGraph);
        }
        else if (hasOptions && options != null && options.Length > 0)
        {
            // 显示选项并暂停Timeline
            uiManager.ShowOptions(new System.Collections.Generic.List<string>(options), OnOptionSelected);
            PauseTimeline();
        }
        else
        {
            // 普通对话
            uiManager.ShowDialogue(speaker, dialogueText);
            PauseTimeline();
        }
    }
    
    private void OnOptionSelected(int selectedIndex)
    {
        Debug.Log($"[DialogueTimelineBehaviour] 选择了选项: {selectedIndex}");
        ResumeTimeline();
    }
    
    private void OnDialogueFinished()
    {
        Debug.Log("[DialogueTimelineBehaviour] 对话结束");
        
        // 取消订阅事件
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.OnDialogueEndedEvent -= OnDialogueFinished;
        }
        
        ResumeTimeline();
    }
    
    private void PauseTimeline()
    {
        if (currentDirector != null && currentDirector.state == PlayState.Playing)
        {
            currentDirector.Pause();
            Debug.Log($"[DialogueTimelineBehaviour] 暂停Timeline: {currentDirector.gameObject.name}");
        }
    }
    
    private void ResumeTimeline()
    {
        if (!autoResumeTimeline) return;
        
        if (currentDirector != null && currentDirector.state == PlayState.Paused)
        {
            // 如果设置了跳转时间，先跳转再播放
            if (jumpToTime >= 0)
            {
                currentDirector.time = jumpToTime;
                Debug.Log($"[DialogueTimelineBehaviour] Timeline跳转到时间: {jumpToTime}");
            }
            
            currentDirector.Play();
            Debug.Log($"[DialogueTimelineBehaviour] 恢复Timeline播放: {currentDirector.gameObject.name}");
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        triggered = false;
    }
}
