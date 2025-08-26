using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TimelineDebugTool : MonoBehaviour
{
    [Header("监控设置")]
    public List<PlayableDirector> directors = new List<PlayableDirector>();
    public bool logStateChanges = true;
    
    private Dictionary<PlayableDirector, PlayState> lastStates = new Dictionary<PlayableDirector, PlayState>();
    
    public static PlayableDirector currentDialogueDirector;
    
    void Start()
    {
        // 初始化状态记录
        foreach (var director in directors)
        {
            if (director != null)
            {
                lastStates[director] = director.state;
            }
        }
    }
    
    void Update()
    {
        foreach (var director in directors)
        {
            if (director == null) continue;
            
            // 检测状态变化
            if (lastStates.TryGetValue(director, out PlayState lastState))
            {
                if (lastState != director.state)
                {
                    if (logStateChanges)
                    {
                        Debug.Log($"[TimelineDebug] Timeline '{director.gameObject.name}' 状态从 {lastState} 变为 {director.state}");
                    }
                    lastStates[director] = director.state;
                }
            }
            else
            {
                lastStates[director] = director.state;
            }
        }
        
        CheckStuckTimeline();
    }
    
    // 在运行时添加Director到监控列表
    public void AddDirector(PlayableDirector director)
    {
        if (director != null && !directors.Contains(director))
        {
            directors.Add(director);
            lastStates[director] = director.state;
            Debug.Log($"[TimelineDebug] 添加Timeline '{director.gameObject.name}' 到监控");
        }
    }

    public static void PauseForDialogue(PlayableDirector director)
    {
        if (director != null && director.state == PlayState.Playing)
        {
            currentDialogueDirector = director;
            director.Pause();
            Debug.Log($"[TimelineDebug] 暂停Timeline '{director.gameObject.name}' 用于对话");
        }
    }

    public static void ResumeAfterDialogue()
    {
        if (currentDialogueDirector != null && currentDialogueDirector.state == PlayState.Paused)
        {
            currentDialogueDirector.Play();
            Debug.Log($"[TimelineDebug] 恢复Timeline '{currentDialogueDirector.gameObject.name}' 播放");
            currentDialogueDirector = null;
        }
    }

    void CheckStuckTimeline()
    {
        if (currentDialogueDirector != null && currentDialogueDirector.state == PlayState.Paused)
        {
            // 检测是否长时间暂停，可能表明对话结束但没有恢复
            // 可以添加计时器代码实现
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TimelineDebugTool))]
public class TimelineDebugToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TimelineDebugTool tool = (TimelineDebugTool)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前Timeline状态", EditorStyles.boldLabel);
        
        foreach (var director in tool.directors)
        {
            if (director != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(director.gameObject.name);
                
                string stateColor = "black";
                switch(director.state)
                {
                    case PlayState.Playing:
                        stateColor = "green";
                        break;
                    case PlayState.Paused:
                        stateColor = "orange";
                        break;
                    // 已弃用的状态，使用自定义颜色
                    case (PlayState)2: // PlayState.Delayed 已弃用
                        stateColor = "red";
                        break;
                    default:
                        stateColor = "gray";
                        break;
                }
                
                EditorGUILayout.LabelField($"<color={stateColor}>{director.state}</color>", new GUIStyle(EditorStyles.label) { richText = true });
                EditorGUILayout.EndHorizontal();
            }
        }
        
        if (Application.isPlaying && GUILayout.Button("查找所有Timeline"))
        {
            FindAllTimelines(tool);
        }
    }
    
    private void FindAllTimelines(TimelineDebugTool tool)
    {
        PlayableDirector[] directors = FindObjectsOfType<PlayableDirector>();
        
        foreach (var director in directors)
        {
            tool.AddDirector(director);
        }
        
        Debug.Log($"[TimelineDebug] 找到 {directors.Length} 个Timeline对象");
    }
}
#endif 