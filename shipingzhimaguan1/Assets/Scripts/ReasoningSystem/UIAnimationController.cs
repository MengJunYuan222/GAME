using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

// 处理UI的动画效果
public class UIAnimationController : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float confirmAnimDuration = 0.5f;
    
    [Header("失败动画设置")]
    [SerializeField] private Color failureFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private int failureFlashCount = 3;
    [SerializeField] private float failureFlashDuration = 0.2f;
    
    // 播放成功动画
    public void PlaySuccessAnimation(GameObject target)
    {
        if (target == null) return;
        
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect != null)
        {
            // 执行缩放动画
            Vector3 originalScale = rect.localScale;
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(rect.DOScale(originalScale * 1.2f, confirmAnimDuration / 2).SetEase(Ease.OutQuad));
            sequence.Append(rect.DOScale(originalScale, confirmAnimDuration / 2).SetEase(Ease.InOutQuad));
        }
    }
    
    [Header("详情面板引用")]
    [SerializeField] private GameObject itemDetailPanel1;  // 物品详情面板1
    [SerializeField] private GameObject itemDetailPanel2;  // 物品详情面板2
    
    // 播放失败动画
    public void PlayFailureAnimation(string message)
    {
        // 只在详情面板上闪烁而不是全屏
        if (itemDetailPanel1 != null)
        {
            StartCoroutine(PlayPanelFlashAnimation(itemDetailPanel1));
        }
        
        if (itemDetailPanel2 != null)
        {
            StartCoroutine(PlayPanelFlashAnimation(itemDetailPanel2));
        }
    }
    
    // 在特定面板上播放闪烁动画
    private IEnumerator PlayPanelFlashAnimation(GameObject targetPanel)
    {
        if (targetPanel == null) yield break;
        
        // 获取面板原始背景颜色
        Image panelImage = targetPanel.GetComponent<Image>();
        if (panelImage == null) yield break;
        
        // 保存原始颜色
        Color originalColor = panelImage.color;
        
        // 闪烁效果
        for (int i = 0; i < failureFlashCount; i++)
        {
            // 渐变为失败颜色
            float timer = 0;
            while (timer < failureFlashDuration / 2)
            {
                timer += Time.deltaTime;
                float t = timer / (failureFlashDuration / 2);
                panelImage.color = Color.Lerp(originalColor, failureFlashColor, t);
                yield return null;
            }
            
            // 渐变回原始颜色
            timer = 0;
            while (timer < failureFlashDuration / 2)
            {
                timer += Time.deltaTime;
                float t = timer / (failureFlashDuration / 2);
                panelImage.color = Color.Lerp(failureFlashColor, originalColor, t);
                yield return null;
            }
        }
        
        // 确保恢复原始颜色
        panelImage.color = originalColor;
    }
    
    // 播放结论动画
    public void PlayConclusionAnimation(GameObject conclusionPanel)
    {
        if (conclusionPanel == null) return;
        
        RectTransform panelRect = conclusionPanel.GetComponent<RectTransform>();
        if (panelRect == null) return;
        
        // 重置动画状态
        panelRect.localScale = Vector3.zero;
        
        // 执行缩放动画
        panelRect.DOScale(Vector3.one, confirmAnimDuration).SetEase(Ease.OutBack);
        
        // 执行透明度动画
        CanvasGroup canvasGroup = conclusionPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, confirmAnimDuration / 2);
        }
    }
    
    // 嫌疑人揭示动画
    public void PlayRevealAnimation(GameObject targetPanel, float duration)
    {
        if (targetPanel == null) return;
        
        RectTransform panelRect = targetPanel.GetComponent<RectTransform>();
        if (panelRect == null) return;
        
        // 重置动画状态
        panelRect.localScale = Vector3.zero;
        
        // 执行动画
        panelRect.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        
        // 执行透明度动画
        CanvasGroup canvasGroup = targetPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, duration / 2);
        }
    }
}