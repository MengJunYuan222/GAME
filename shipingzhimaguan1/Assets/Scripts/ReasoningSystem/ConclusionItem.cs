using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class ConclusionItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image conclusionImage;
    [SerializeField] private TextMeshProUGUI conclusionText;
    
    private ItemSO conclusionData;
    private ReasoningUIManager uiManager;
    private Vector3 originalScale;
    private bool isAnimating = false;
    
    // 公开访问结论数据的属性
    public ItemSO ConclusionData => conclusionData;
    
    // 设置动画状态
    public void SetAnimating(bool animating)
    {
        isAnimating = animating;
    }
    
    private void Awake()
    {
        // 保存原始缩放
        originalScale = transform.localScale;
        
        // 确保有CanvasGroup组件以处理交互
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // 确保GameObject有GraphicRaycaster
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            // 添加GraphicRaycaster到Canvas
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // 自动查找组件（不创建和强制设置位置/尺寸）
        if (conclusionImage == null)
        {
            // 尝试获取Image组件
            conclusionImage = GetComponent<Image>();
            if (conclusionImage == null)
            {
                conclusionImage = GetComponentInChildren<Image>();
            }
        }
        
        // 确保Image组件可以接收鼠标事件
        if (conclusionImage != null)
        {
            conclusionImage.raycastTarget = true;
        }
        else
        {
            Debug.LogWarning("[ConclusionItem] 找不到Image组件，某些交互功能可能不可用");
        }
        
        if (conclusionText == null)
        {
            // 尝试获取TextMeshProUGUI组件
            conclusionText = GetComponent<TextMeshProUGUI>();
            if (conclusionText == null)
            {
                conclusionText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        if (conclusionText == null)
        {
            Debug.LogWarning("[ConclusionItem] 找不到TextMeshProUGUI组件，文本显示功能不可用");
        }
        
        // Awake完成
    }
    
    private void Start()
    {
        // 获取UI管理器
        uiManager = FindObjectOfType<ReasoningUIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("未找到ReasoningUIManager组件");
        }
    }
    
    // 设置结论数据
    public void SetConclusion(ItemSO conclusion)
    {
        if (conclusion == null) 
        {
            Debug.LogError("[ConclusionItem] 设置了空的结论数据");
            return;
        }
        
        conclusionData = conclusion;
        // 设置结论数据
        
        // 更新显示
        UpdateDisplay();
    }
    
    // 更新显示
    private void UpdateDisplay()
    {
        if (conclusionData == null) 
        {
            Debug.LogError("[ConclusionItem] 结论数据为空，无法更新显示");
            return;
        }
        
        // 更新结论显示
        
        // 设置图片 - 直接使用物品图标
        if (conclusionImage != null)
        {
            Sprite displaySprite = conclusionData.itemIcon;
            
            if (displaySprite != null)
            {
                conclusionImage.sprite = displaySprite;
                conclusionImage.enabled = true;
            }
            else
            {
                Debug.LogError($"[ConclusionItem] 物品 {conclusionData.itemName} 没有可用的图像");
            }
        }
        else
        {
            Debug.LogError("[ConclusionItem] conclusionImage引用为空，无法显示图像");
        }
        
        // 设置文本
        if (conclusionText != null)
        {
            conclusionText.text = conclusionData.itemName + "\n\n" + conclusionData.itemDescription;
            // 设置结论文本
        }
        else
        {
            Debug.LogError("[ConclusionItem] conclusionText引用为空，无法显示文本");
        }
    }
    
    // 鼠标进入时
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isAnimating) return;
        
        // 鼠标进入结论项，执行放大动画
        transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutQuad);
        
        // 通知UI管理器鼠标正在悬停在结论上
        if (uiManager != null)
        {
            uiManager.OnConclusionItemHover(this);
        }
    }
    
    // 鼠标离开时
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isAnimating) return;
        
        // 鼠标离开结论项，恢复原始大小
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);
        
        // 通知UI管理器鼠标离开了结论
        if (uiManager != null)
        {
            uiManager.OnConclusionItemExit(this);
        }
    }
    
    // 鼠标点击时
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnimating) 
        {
            // 结论正在动画中，忽略点击
            return;
        }
        
        // 点击了结论项
        
        // 检查UI管理器是否为空
        if (uiManager == null)
        {
            Debug.LogError("[ConclusionItem] UI管理器引用为空，尝试重新获取");
            uiManager = FindObjectOfType<ReasoningUIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[ConclusionItem] 无法找到ReasoningUIManager组件");
                return;
            }
        }
        
        // 处理点击
        PlayClickAnimation();
        
        // 检查这个结论是否是已经印在背景上的结论
        bool isStampedConclusion = transform.parent != null && 
                                  transform.parent.name.Contains("Position");
        
        if (isStampedConclusion)
        {
            // 如果是已印章结论，调用UI管理器的处理方法
                // 点击了已印章结论，调用切换位置方法
                uiManager.OnStampedConclusionClick(this.gameObject);
        }
        else if (transform.parent != null && uiManager.GetConclusionResultPanel() != null && 
                transform.parent == uiManager.GetConclusionResultPanel().transform)
        {
            // 如果是普通结论面板中的结论，则调用确认方法
                // 调用UI管理器的确认按钮点击方法
                uiManager.OnConfirmButtonClick();
        }
        else
        {
            // 其他情况，例如在结论显示区域中的结论
            // 点击了其他位置的结论项
            
                // 如果结论已经被确认，可以尝试切换其显示状态
                if (uiManager.IsConfirmedConclusion(conclusionData))
                {
                    uiManager.OnStampedConclusionClick(this.gameObject);
            }
        }
    }
    
    // 播放点击动画
    private void PlayClickAnimation()
    {
        isAnimating = true;
        
        // 点击结论项
        
        // 直接重置状态
        isAnimating = false;
    }
    
    // 更新原始缩放值，在结论固定位置后调用
    public void UpdateOriginalScale()
    {
        // 更新原始缩放值为当前缩放值
        originalScale = transform.localScale;
    }
}