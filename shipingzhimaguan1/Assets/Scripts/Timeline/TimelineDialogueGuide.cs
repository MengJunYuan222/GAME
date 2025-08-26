/*
 * Timeline过场对话系统使用指南
 * 
 * 这个脚本包含了如何制作Timeline过场对话的完整说明和示例
 * 请按照以下步骤来实现Timeline中的对话功能
 */

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using DialogueSystem;

public class TimelineDialogueGuide : MonoBehaviour
{
    /*
     * ============================================
     * 📋 Timeline对话系统使用指南
     * ============================================
     * 
     * 您的对话系统已经支持Timeline集成！以下是完整的使用方法：
     * 
     * 
     * 🎬 方法一：使用DialogueTimelineBehaviour（推荐）
     * ============================================
     * 
     * 1. 在Timeline窗口中右键添加 "Playable Track"
     * 2. 在轨道上右键选择 "Add Playable Asset" -> "DialogueTimelineBehaviour" 
     * 3. 选中Clip，在Inspector中配置：
     *    - DialogueGraph: 拖入您的对话节点图资源
     *    - Speaker: 设置说话的角色
     *    - DialogueText: 输入对话文本（如果不使用DialogueGraph）
     *    - HasOptions: 是否显示选项
     *    - Options: 选项文本数组
     *    - AutoResumeTimeline: 对话结束后是否自动恢复Timeline
     *    - JumpToTime: 对话结束后跳转到的时间点（-1表示继续播放）
     *    - UIAnimation: UI动画类型
     *    - StopBackgroundMusic: 是否停止背景音乐
     *    - VoiceClip: 对话语音音频
     * 
     * 4. Timeline播放到该Clip时会：
     *    - 自动暂停Timeline
     *    - 显示对话界面
     *    - 等待玩家交互
     *    - 对话结束后恢复Timeline播放
     * 
     * 
     * 🎬 方法二：使用CutsceneController的对话序列
     * ============================================
     * 
     * 1. 在包含PlayableDirector的GameObject上添加CutsceneController组件
     * 2. 在DialogueSequence列表中添加对话：
     *    - Dialogue: 拖入对话节点图
     *    - TimelinePositionAfter: 对话后Timeline跳转位置
     *    - Description: 对话描述（方便编辑器识别）
     * 3. 在Timeline中添加Signal Emitter
     * 4. 创建Signal Asset，绑定到CutsceneController.TriggerDialogueSequence()
     * 
     * 
     * 🎬 方法三：使用Signal Receiver触发单个对话
     * ============================================
     * 
     * 1. 在Timeline中添加Signal Track
     * 2. 添加Signal Emitter，创建Signal Asset
     * 3. 在场景中添加Signal Receiver组件
     * 4. 绑定Signal Asset和响应函数：
     *    - CutsceneController.PlaySingleDialogue(DialogueNodeGraph)
     *    - 或自定义对话触发方法
     * 
     * 
     * 🔧 高级功能
     * ============================================
     * 
     * 1. 动态Timeline控制：
     *    - 使用TimelineController.PlayTimeline(string timelineID)
     *    - 支持通过ID播放指定Timeline
     * 
     * 2. 对话与Timeline同步：
     *    - DialogueUIManager.SetActiveTimelineDirector()
     *    - 自动处理Timeline暂停和恢复
     * 
     * 3. 调试工具：
     *    - 使用TimelineDebugTool监控Timeline状态
     *    - 查看实时播放状态和对话触发情况
     * 
     * 
     * 💡 使用技巧
     * ============================================
     * 
     * 1. 对话节点图优先级最高：
     *    如果设置了DialogueGraph，将使用完整的对话系统功能
     * 
     * 2. 简单对话使用文本模式：
     *    直接设置Speaker和DialogueText进行快速对话
     * 
     * 3. 选项对话：
     *    设置HasOptions为true，配置Options数组
     * 
     * 4. Timeline跳转：
     *    使用JumpToTime精确控制对话后的播放位置
     * 
     * 5. UI动画集成：
     *    配置UIAnimation和AnimationName添加视觉效果
     * 
     * 
     * 🚀 最佳实践
     * ============================================
     * 
     * 1. 使用DialogueNodeGraph进行复杂对话
     * 2. 简单对话使用直接文本模式
     * 3. 合理使用Timeline跳转避免播放混乱
     * 4. 测试时使用TimelineDebugTool监控状态
     * 5. 配置AutoResumeTimeline避免Timeline卡死
     * 
     */

    [Header("示例配置")]
    [Tooltip("示例对话节点图")]
    public DialogueNodeGraph exampleDialogue;
    
    [Tooltip("示例Timeline控制器")]
    public PlayableDirector exampleTimeline;
    
    [Tooltip("示例过场控制器")]
    public CutsceneController exampleCutscene;

    // 示例：通过代码触发Timeline对话
    [ContextMenu("示例：播放Timeline对话")]
    public void ExamplePlayTimelineDialogue()
    {
        if (exampleTimeline != null)
        {
            // 方法1：直接播放包含对话的Timeline
            exampleTimeline.Play();
        }
        
        if (exampleCutscene != null)
        {
            // 方法2：触发对话序列
            exampleCutscene.TriggerDialogueSequence();
        }
        
        if (exampleDialogue != null && DialogueUIManager.Instance != null)
        {
            // 方法3：直接启动对话（不通过Timeline）
            DialogueUIManager.Instance.StartDialogue(exampleDialogue);
        }
    }

    // 示例：创建自定义对话触发器
    public void TriggerCustomDialogue(DialogueNodeGraph dialogue)
    {
        if (dialogue == null || DialogueUIManager.Instance == null)
        {
            Debug.LogError("对话图或对话管理器为空！");
            return;
        }

        // 如果有活跃的Timeline，暂停它
        if (exampleTimeline != null && exampleTimeline.state == PlayState.Playing)
        {
            exampleTimeline.Pause();
            
            // 设置对话管理器的Timeline引用
            DialogueUIManager.Instance.SetActiveTimelineDirector(exampleTimeline);
            
            // 订阅对话结束事件来恢复Timeline
            DialogueUIManager.Instance.OnDialogueEndedEvent += OnCustomDialogueEnded;
        }

        // 开始对话
        DialogueUIManager.Instance.StartDialogue(dialogue);
    }

    private void OnCustomDialogueEnded()
    {
        // 取消订阅事件
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.OnDialogueEndedEvent -= OnCustomDialogueEnded;
        }

        // 恢复Timeline播放
        if (exampleTimeline != null && exampleTimeline.state == PlayState.Paused)
        {
            exampleTimeline.Play();
            Debug.Log("自定义对话结束，恢复Timeline播放");
        }
    }

    private void OnDestroy()
    {
        // 清理事件订阅
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.OnDialogueEndedEvent -= OnCustomDialogueEnded;
        }
    }
}

