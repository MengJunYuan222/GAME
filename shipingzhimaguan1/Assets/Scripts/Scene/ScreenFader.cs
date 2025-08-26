using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 屏幕淡入淡出控制器
/// 用于场景切换等时刻平滑过渡
/// </summary>
public class ScreenFader : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("用于淡入淡出的图像组件，通常是一个覆盖全屏的黑色面板")]
    public Image fadeImage;
    
    [Header("设置")]
    [Tooltip("淡入淡出曲线，控制透明度变化")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("是否在启动时自动淡入")]
    public bool fadeInOnStart = true;
    [Tooltip("启动时淡入的持续时间")]
    public float startFadeInDuration = 1f;
    
    private Coroutine currentFadeCoroutine = null;
    
    private void Start()
    {
        // 确保组件引用正确
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
            if (fadeImage == null)
            {
                Debug.LogError("[ScreenFader] 未指定淡入淡出图像，且当前物体没有Image组件");
                return;
            }
        }
        
        // 确保初始状态为全黑
        Color initialColor = fadeImage.color;
        initialColor.a = 1f;
        fadeImage.color = initialColor;
        
        // 如果设置了启动时淡入，执行淡入
        if (fadeInOnStart)
        {
            FadeIn(startFadeInDuration);
        }
    }
    
    /// <summary>
    /// 执行淡入效果（从黑色到透明）
    /// </summary>
    /// <param name="duration">淡入持续时间（秒）</param>
    public void FadeIn(float duration)
    {
        if (fadeImage == null) return;
        
        // 停止当前正在执行的淡入淡出
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        // 启动新的淡入协程
        currentFadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, duration));
    }
    
    /// <summary>
    /// 执行淡出效果（从透明到黑色）
    /// </summary>
    /// <param name="duration">淡出持续时间（秒）</param>
    public void FadeOut(float duration)
    {
        if (fadeImage == null) return;
        
        // 停止当前正在执行的淡入淡出
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        // 启动新的淡出协程
        currentFadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, duration));
    }
    
    /// <summary>
    /// 淡入淡出协程
    /// </summary>
    private IEnumerator FadeCoroutine(float startAlpha, float targetAlpha, float duration)
    {
        // 确保图像是启用的
        fadeImage.gameObject.SetActive(true);
        
        // 设置初始透明度
        Color currentColor = fadeImage.color;
        currentColor.a = startAlpha;
        fadeImage.color = currentColor;
        
        float elapsedTime = 0f;
        
        // 逐帧调整透明度
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            
            // 使用动画曲线计算当前透明度
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, fadeCurve.Evaluate(normalizedTime));
            
            // 更新图像颜色
            currentColor.a = currentAlpha;
            fadeImage.color = currentColor;
            
            yield return null;
        }
        
        // 确保最终透明度精确匹配目标值
        currentColor.a = targetAlpha;
        fadeImage.color = currentColor;
        
        // 如果完全透明，可以禁用图像以节省性能
        if (targetAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }
        
        // 清除协程引用
        currentFadeCoroutine = null;
    }
} 