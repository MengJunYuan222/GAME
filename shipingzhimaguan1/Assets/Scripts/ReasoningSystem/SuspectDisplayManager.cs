using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

// 处理嫌疑人的显示
public class SuspectDisplayManager : MonoBehaviour
{
    [SerializeField] private GameObject suspectPanel;
    [SerializeField] private Image suspectImage;
    [SerializeField] private TextMeshProUGUI suspectName;
    // 已移除描述功能
    [SerializeField] private Button closeButton;
    
    [Header("场景跳转设置")]
    [SerializeField] private string targetSceneName = "NextScene"; // 目标场景名称
    
    [Header("嫌疑人配置")]
    [SerializeField] private List<SuspectSO> allSuspects = new List<SuspectSO>();
    
    // 调试方法
    // 用于编辑器中调试，不在运行时使用
    [ContextMenu("检查嫌疑人配置")]
    public void DebugCheckSuspects()
    {
        // 调试代码已移除
    }
    
    [Header("动画设置")]
    
    private UIAnimationController animationController;
    
    private void Awake()
    {
        // 获取动画控制器引用
        animationController = GetComponent<UIAnimationController>();
        if (animationController == null)
        {
            animationController = FindObjectOfType<UIAnimationController>();
        }
        
        // 初始隐藏嫌疑人面板
        if (suspectPanel != null)
        {
            suspectPanel.SetActive(false);
        }
        
        // 设置按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(LoadTargetScene);
        }
    }
    
    // 检查是否应该显示嫌疑人
    public void CheckShowSuspect(List<ItemSO> confirmedConclusions)
    {
        if (confirmedConclusions == null || confirmedConclusions.Count < 2) 
        {
            return;
        }
        
        foreach (var suspect in allSuspects)
        {
            if (suspect == null)
            {
                continue;
            }
            
            bool canBeRevealed = suspect.CanBeRevealed(confirmedConclusions);
            
            if (canBeRevealed)
            {
                ShowSuspect(suspect);
                break;
            }
        }
    }
    
    // 显示嫌疑人
    public void ShowSuspect(SuspectSO suspect)
    {
        Debug.Log($"[嫌疑人系统] 尝试显示嫌疑人: {(suspect != null ? suspect.suspectName : "空")}");
        
        if (suspect == null || suspectPanel == null) 
        {
            return;
        }
        
        // 激活面板
        suspectPanel.SetActive(true);
        
        // 设置嫌疑人信息
        if (suspectImage != null)
        {
            if (suspect.suspectPortrait != null)
            {
                suspectImage.sprite = suspect.suspectPortrait;
                suspectImage.enabled = true;
            }
            else
            {
                suspectImage.enabled = false;
            }
        }
        
        if (suspectName != null)
        {
            suspectName.text = suspect.suspectName;
        }
        
        // 不再显示描述文本
        
        // 播放动画
        PlayRevealAnimation();
    }
    
    // 显示面板，不使用动画
    private void PlayRevealAnimation()
    {
        // 确保面板激活
        if (suspectPanel != null)
        {
            suspectPanel.SetActive(true);
            
            // 确保正常缩放
            RectTransform panelRect = suspectPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one;
            }
            
            // 确保完全不透明
            CanvasGroup canvasGroup = suspectPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }
    
    // 隐藏嫌疑人面板
    public void HideSuspect()
    {
        if (suspectPanel != null)
        {
            suspectPanel.SetActive(false);
        }
    }
    
    // 跳转到目标场景（使用场景替换，保持UI场景不变）
    public void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SuspectDisplayManager] 目标场景名称为空，无法跳转！");
            return;
        }
        
        Debug.Log($"[SuspectDisplayManager] 准备替换场景到: {targetSceneName}");
        
        try
        {
            StartCoroutine(ReplaceMainScene());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SuspectDisplayManager] 场景替换失败: {e.Message}");
            Debug.LogError($"请确保场景 '{targetSceneName}' 已添加到Build Settings中");
        }
    }
    
    // 场景替换协程 - 保持UI场景不变，只替换主游戏场景
    private System.Collections.IEnumerator ReplaceMainScene()
    {
        Debug.Log("[SuspectDisplayManager] 开始场景替换流程...");
        
        // 1. 找到当前的主游戏场景（非UI场景）
        Scene currentMainScene = FindMainGameScene();
        
        if (!currentMainScene.IsValid())
        {
            Debug.LogError("[SuspectDisplayManager] 找不到当前主游戏场景！");
            yield break;
        }
        
        Debug.Log($"[SuspectDisplayManager] 当前主场景: {currentMainScene.name}");
        
        // 2. 异步加载新场景（作为附加场景）
        Debug.Log($"[SuspectDisplayManager] 开始加载新场景: {targetSceneName}");
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        
        // 等待场景加载完成
        while (!loadOperation.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"[SuspectDisplayManager] 新场景 {targetSceneName} 加载完成");
        
        // 3. 设置新场景为活动场景
        Scene newScene = SceneManager.GetSceneByName(targetSceneName);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
            Debug.Log($"[SuspectDisplayManager] 设置 {targetSceneName} 为活动场景");
        }
        
        // 4. 卸载旧的主游戏场景
        if (currentMainScene.IsValid() && currentMainScene.name != targetSceneName)
        {
            Debug.Log($"[SuspectDisplayManager] 开始卸载旧场景: {currentMainScene.name}");
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentMainScene);
            
            // 等待卸载完成
            while (!unloadOperation.isDone)
            {
                yield return null;
            }
            
            Debug.Log($"[SuspectDisplayManager] 旧场景 {currentMainScene.name} 卸载完成");
        }
        
        // 5. 强制垃圾回收
        System.GC.Collect();
        
        Debug.Log("[SuspectDisplayManager] 场景替换完成！");
    }
    
    // 查找主游戏场景（排除UI场景和DontDestroyOnLoad场景）
    private Scene FindMainGameScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            
            // 跳过UI场景
            if (scene.name.Equals("Ui", System.StringComparison.OrdinalIgnoreCase) || 
                scene.name.Equals("UI", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            // 跳过DontDestroyOnLoad场景
            if (scene.name == "DontDestroyOnLoad")
            {
                continue;
            }
            
            // 跳过目标场景（如果已经加载）
            if (scene.name == targetSceneName)
            {
                continue;
            }
            
            // 找到主游戏场景
            return scene;
        }
        
        return new Scene(); // 返回无效场景
    }
    
    // 添加嫌疑人
    public void AddSuspect(SuspectSO suspect)
    {
        if (suspect != null && !allSuspects.Contains(suspect))
        {
            allSuspects.Add(suspect);
        }
    }
}