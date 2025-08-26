using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using System.Collections.Generic;
using System;

public class TimelineController : MonoBehaviour
{
    public static TimelineController Instance { get; private set; }
    
    [System.Serializable]
    public class TimelineData
    {
        public string timelineID;
        public PlayableDirector playableDirector;
        [Tooltip("Timeline结束后是否自动恢复对话")]
        public bool autoResumeDialogue = true;
    }
    
    [Header("Timeline配置")]
    public List<TimelineData> timelineList = new List<TimelineData>();
    
    // 当Timeline播放完成时触发
    public event Action OnTimelineCompleted;
    
    private PlayableDirector activeDirector;
    private TimelineData activeTimelineData;
    private bool isWaitingForCompletion = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 根据ID播放Timeline
    /// </summary>
    public void PlayTimeline(string timelineID)
    {
        foreach (var timeline in timelineList)
        {
            if (timeline.timelineID == timelineID && timeline.playableDirector != null)
            {
                Debug.Log($"[TimelineController] 播放Timeline: {timelineID}");
                
                // 停止当前正在播放的Timeline
                if (activeDirector != null && activeDirector.state == PlayState.Playing)
                {
                    activeDirector.Stop();
                }
                
                // 设置新的活动Timeline
                activeDirector = timeline.playableDirector;
                activeTimelineData = timeline;
                
                // 重置Timeline
                activeDirector.time = 0;
                
                // 注册Timeline完成回调
                activeDirector.stopped -= OnTimelineStopped;
                activeDirector.stopped += OnTimelineStopped;
                
                // 播放Timeline
                activeDirector.Play();
                isWaitingForCompletion = true;
                return;
            }
        }
        
        Debug.LogWarning($"[TimelineController] 未找到ID为 {timelineID} 的Timeline");
    }
    
    /// <summary>
    /// Timeline播放完成回调
    /// </summary>
    private void OnTimelineStopped(PlayableDirector director)
    {
        if (!isWaitingForCompletion || director != activeDirector) return;
        
        Debug.Log($"[TimelineController] Timeline '{activeTimelineData.timelineID}' 已播放完成");
        isWaitingForCompletion = false;
        
        if (activeTimelineData.autoResumeDialogue)
        {
            OnTimelineCompleted?.Invoke();
        }
        
        // 取消注册回调以避免内存泄漏
        director.stopped -= OnTimelineStopped;
    }
    
    /// <summary>
    /// 停止当前Timeline播放
    /// </summary>
    public void StopCurrentTimeline()
    {
        if (activeDirector != null)
        {
            activeDirector.Stop();
            isWaitingForCompletion = false;
        }
    }
    
    /// <summary>
    /// 停止所有Timeline播放
    /// </summary>
    public void StopAllTimelines()
    {
        foreach (var timeline in timelineList)
        {
            if (timeline.playableDirector != null)
            {
                timeline.playableDirector.Stop();
            }
        }
        
        isWaitingForCompletion = false;
    }
} 