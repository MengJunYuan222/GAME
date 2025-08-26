using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using DialogueSystem;

[Serializable]
public class DialogueTimelineClip : PlayableAsset, ITimelineClipAsset
{
    [Header("对话设置")]
    public DialogueNodeGraph dialogueGraph;
    public ActorSO speaker;
    public string dialogueText;
    
    [Header("选项设置")]
    public bool hasOptions = false;
    public string[] options = new string[0];
    
    [Header("Timeline控制")]
    [Tooltip("对话结束后是否自动恢复Timeline")]
    public bool autoResumeTimeline = true;
    [Tooltip("对话结束后Timeline跳转到的时间点（-1表示继续播放）")]
    public double jumpToTime = -1;
    
    [Header("UI效果")]
    public UIAnimType uiAnimation = UIAnimType.None;
    public string animationName = "";
    public bool waitForAnimation = false;
    
    [Header("音频设置")]
    public bool stopBackgroundMusic = false;
    public AudioClip voiceClip;
    
    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueTimelineBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        
        // 传递设置到行为
        behaviour.dialogueGraph = dialogueGraph;
        behaviour.speaker = speaker;
        behaviour.dialogueText = dialogueText;
        behaviour.hasOptions = hasOptions;
        behaviour.options = options;
        behaviour.autoResumeTimeline = autoResumeTimeline;
        behaviour.jumpToTime = jumpToTime;
        behaviour.uiAnimation = uiAnimation;
        behaviour.animationName = animationName;
        behaviour.waitForAnimation = waitForAnimation;
        behaviour.stopBackgroundMusic = stopBackgroundMusic;
        behaviour.voiceClip = voiceClip;
        
        return playable;
    }
}

