using UnityEngine;
using UnityEngine.Playables;
using System.Reflection;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景初始化管理器
/// 挂载在每个场景的初始化物体上
/// 负责在场景加载后设置正确的UI状态和Timeline连接
/// </summary>
public class SceneInitializer : MonoBehaviour
{
    [Header("Timeline设置")]
    [Tooltip("是否自动连接Timeline和对话系统")]
    public bool connectTimelineToDialogue = true;
    [Tooltip("场景中的主要Timeline Director")]
    public PlayableDirector mainDirector;
    
    [Header("调试")]
    public bool showDebugLogs = true;
    
    private void Start()
    {
        // 等待一帧确保所有对象都已加载
        Invoke("Initialize", 0.1f);
    }
    
    private void Initialize()
    {
        // 连接Timeline和对话系统
        if (connectTimelineToDialogue)
        {
            ConnectTimelineToDialogue();
        }
        
        // 在这里可以添加其他初始化逻辑
    }
    
    /// <summary>
    /// 连接Timeline和对话系统
    /// </summary>
    private void ConnectTimelineToDialogue()
    {
        // 获取对话管理器
        var dialogueManager = FindObjectOfType<DialogueSystem.DialogueUIManager>();
        if (dialogueManager == null)
        {
            LogWarning("未找到DialogueUIManager实例！");
            return;
        }
        
        // 获取Timeline Director
        if (mainDirector == null)
        {
            // 如果没有指定，尝试查找场景中的PlayableDirector
            mainDirector = FindObjectOfType<PlayableDirector>();
            if (mainDirector == null)
            {
                LogWarning("未找到PlayableDirector实例！");
                return;
            }
        }
        
        // 通过反射设置对话管理器的timelineDirector字段
        try
        {
            var field = dialogueManager.GetType().GetField("timelineDirector", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (field != null)
            {
                field.SetValue(dialogueManager, mainDirector);
                LogMessage($"已连接Timeline Director '{mainDirector.name}' 到对话系统");
            }
            else
            {
                LogWarning("DialogueUIManager中未找到timelineDirector字段");
            }
        }
        catch (System.Exception e)
        {
            LogWarning($"设置Timeline Director失败: {e.Message}");
        }
    }
    
    private void LogMessage(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SceneInitializer] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[SceneInitializer] {message}");
    }
} 