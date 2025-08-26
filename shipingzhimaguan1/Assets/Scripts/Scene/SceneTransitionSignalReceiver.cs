using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 场景切换信号接收器
/// 挂载在包含Timeline Director的游戏对象上
/// 用于接收Timeline信号并触发场景切换
/// </summary>
public class SceneTransitionSignalReceiver : MonoBehaviour
{
    [Header("目标场景设置")]
    public string targetSceneName; // 要切换到的场景名称
    public string portalID = "Timeline"; // 传送门ID，用于定位玩家位置
    
    [Header("切换选项")]
    [Tooltip("延迟几秒后执行场景切换")]
    public float transitionDelay = 0f;
    [Tooltip("是否在切换前淡出屏幕")]
    public bool fadeOutBeforeTransition = false;
    [Tooltip("淡出动画时长")]
    public float fadeOutDuration = 1f;
    
    [Header("调试信息")]
    [Tooltip("是否在控制台输出调试信息")]
    public bool showDebugLog = true;
    
    // 此方法将被Timeline信号调用
    public void OnTransitionSignal()
    {
        if (showDebugLog)
        {
            Debug.Log($"[SceneTransitionSignalReceiver] 收到场景切换信号，准备切换到: {targetSceneName}");
        }
        
        if (transitionDelay > 0 || fadeOutBeforeTransition)
        {
            // 如果需要延迟或淡出，使用协程
            StartCoroutine(DelayedTransition());
        }
        else
        {
            // 直接切换
            ExecuteTransition();
        }
    }
    
    private System.Collections.IEnumerator DelayedTransition()
    {
        // 如果需要淡出
        if (fadeOutBeforeTransition)
        {
            // 查找场景中的淡入淡出控制器（如果有）
            ScreenFader fader = FindObjectOfType<ScreenFader>();
            if (fader != null)
            {
                fader.FadeOut(fadeOutDuration);
                yield return new WaitForSeconds(fadeOutDuration);
            }
            else if (showDebugLog)
            {
                Debug.LogWarning("[SceneTransitionSignalReceiver] 未找到ScreenFader组件，无法执行淡出效果");
            }
        }
        
        // 等待延迟时间
        if (transitionDelay > 0)
        {
            yield return new WaitForSeconds(transitionDelay);
        }
        
        // 执行场景切换
        ExecuteTransition();
    }
    
    private void ExecuteTransition()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SceneTransitionSignalReceiver] 目标场景名称为空，无法切换场景");
            return;
        }
        
        if (SceneTransitionManager.Instance != null)
        {
            if (showDebugLog)
            {
                Debug.Log($"[SceneTransitionSignalReceiver] 执行场景切换到: {targetSceneName}, 传送门ID: {portalID}");
            }
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName, portalID);
        }
        else
        {
            Debug.LogError("[SceneTransitionSignalReceiver] 未找到SceneTransitionManager实例，无法切换场景");
        }
    }
    
    // 用于从编辑器中手动测试（Inspector中的按钮）
    [ContextMenu("测试场景切换")]
    public void TestTransition()
    {
        OnTransitionSignal();
    }
} 