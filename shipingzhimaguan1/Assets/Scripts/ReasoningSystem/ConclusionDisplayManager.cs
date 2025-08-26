using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

// 处理结论的展示和管理
public class ConclusionDisplayManager : MonoBehaviour
{
    [Header("结论面板")]
    [SerializeField] private GameObject conclusionPanel;
    [SerializeField] private Image conclusionImage;   // 物品图标
    [SerializeField] private Image backgroundImage;   // 新增：底板图片
    [SerializeField] private TextMeshProUGUI conclusionText;
    [SerializeField] private Button confirmConclusionButton;
    
    [Header("结论印章设置")]
    [SerializeField] private Transform[] conclusionTargetPositions;
    
    [Header("动画设置")]
    [SerializeField] private float confirmAnimDuration = 0.5f;
    
    // 保存的结论列表
    private List<GameObject> stampedConclusions = new List<GameObject>();
    private ItemSO currentPendingConclusion = null;
    private int currentConclusionIndex = 0;
    
    // 用于记录已确认的结论
    private List<ItemSO> confirmedConclusions = new List<ItemSO>();
    
    // 引用其他管理器
    private UIAnimationController animationController;
    private SuspectDisplayManager suspectManager;
    
    private void Awake()
    {
        animationController = GetComponent<UIAnimationController>();
        if (animationController == null)
        {
            animationController = FindObjectOfType<UIAnimationController>();
        }
        
        suspectManager = GetComponent<SuspectDisplayManager>();
        if (suspectManager == null)
        {
            suspectManager = FindObjectOfType<SuspectDisplayManager>();
        }
        
        // 初始隐藏结论面板
        if (conclusionPanel != null)
        {
            conclusionPanel.SetActive(false);
        }
    }
    
    // 显示新结论
    public void ShowConclusion(ItemSO resultItem)
    {
        if (resultItem == null) 
        {
            Debug.LogError("[ConclusionDisplayManager] 错误: 结论物品为空!");
            return;
        }
        
        // 设置当前待确认结论
        currentPendingConclusion = resultItem;
        
        // 激活结论面板
        if (conclusionPanel != null)
        {
            conclusionPanel.SetActive(true);
            
            // 设置结论图片
            if (conclusionImage != null)
            {
                conclusionImage.sprite = resultItem.itemIcon;
                conclusionImage.enabled = true;
            }
            
            // 确保底板图片显示
            if (backgroundImage != null)
            {
                backgroundImage.enabled = true;
            }
            
            // 设置结论文本
            if (conclusionText != null)
            {
                conclusionText.text = resultItem.itemName + "\n\n" + resultItem.itemDescription;
            }
            
            // 确保确认按钮显示
            if (confirmConclusionButton != null)
            {
                confirmConclusionButton.gameObject.SetActive(true);
                // 绑定确认按钮点击事件
                confirmConclusionButton.onClick.RemoveAllListeners();
                confirmConclusionButton.onClick.AddListener(OnConfirmButtonClick);
            }
            else
            {
                Debug.LogError("[ConclusionDisplayManager] 确认按钮引用为空!");
            }
            
            // 播放结论动画
            PlayConclusionAnimation();
        }
        else
        {
            Debug.LogError("[ConclusionDisplayManager] 错误: conclusionPanel未设置!");
        }
    }
    
    // 播放结论动画
    private void PlayConclusionAnimation()
    {
        if (animationController != null)
        {
            animationController.PlayConclusionAnimation(conclusionPanel);
        }
        else
        {
            // 备用动画逻辑，如果animationController不可用
            if (conclusionPanel == null) return;
            
            // 获取结论面板的RectTransform
            RectTransform panelRect = conclusionPanel.GetComponent<RectTransform>();
            if (panelRect == null) return;
            
            // 直接设置正常大小和透明度，不使用动画
            panelRect.localScale = Vector3.one;
            CanvasGroup canvasGroup = conclusionPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
    }
    
    // 确认按钮点击处理
    public void OnConfirmButtonClick()
    {
        if (currentPendingConclusion != null)
        {
            ConfirmConclusion(currentPendingConclusion);
        }
    }
    
    // 确认结论
    public void ConfirmConclusion(ItemSO conclusion)
    {
        if (conclusion == null) return;
        
        // 如果结论已经存在，不再重复添加
        if (IsConfirmedConclusion(conclusion)) return;
        
        // 添加到已确认结论列表
        confirmedConclusions.Add(conclusion);
        
        // 创建结论UI并移动到目标位置
        MoveToNextPosition();
        
        // 隐藏结论面板
        if (conclusionPanel != null)
        {
            conclusionPanel.SetActive(false);
        }
        
        // 检查是否应该显示嫌疑人
        if (suspectManager != null)
        {
            suspectManager.CheckShowSuspect(confirmedConclusions);
        }
    }
    
    // 移动到下一个位置
    private void MoveToNextPosition()
    {
        if (currentPendingConclusion == null || conclusionPanel == null) return;
        
        Transform targetPosition = GetNextPosition();
        if (targetPosition == null) return;
        
        // 首先保存当前结论面板的原始设置
        Vector2 originalSize = conclusionPanel.GetComponent<RectTransform>().sizeDelta;
        Vector3 originalScale = conclusionPanel.transform.localScale;
        
        // 创建结论显示实例（基于ConclusionPanel克隆）
        GameObject conclusionObj = Instantiate(conclusionPanel);
        conclusionObj.name = currentPendingConclusion.itemName + "_Conclusion";
        
        // 设置为激活状态
        conclusionObj.SetActive(true);
        
        // 设置父级为Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            conclusionObj.transform.SetParent(canvas.transform, false);
        }
        
        // 恢复结论面板的原始尺寸和缩放
        RectTransform rt = conclusionObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = originalSize;
            rt.localScale = originalScale;
        }
        
        // 查找结论面板上的组件
        // 在复制的Panel中找到与原始物品图标相同名称的组件
        Transform itemIconTransform = null;
        if (conclusionImage != null)
        {
            // 在复制的面板中查找同名组件
            itemIconTransform = FindChildWithSameName(conclusionObj.transform, conclusionImage.gameObject.name);
        }
        
        // 设置物品图像
        if (itemIconTransform != null)
        {
            Image itemImageCopy = itemIconTransform.GetComponent<Image>();
            if (itemImageCopy != null)
            {
                itemImageCopy.sprite = currentPendingConclusion.itemIcon;
                itemImageCopy.preserveAspect = true;
            }
        }
        else
        {
            Debug.LogWarning("[ConclusionDisplayManager] 无法在复制的面板中找到物品图标组件");
        }
        
        // 设置文本
        // 在复制的面板中找到文本组件
        Transform textTransform = null;
        if (conclusionText != null)
        {
            textTransform = FindChildWithSameName(conclusionObj.transform, conclusionText.gameObject.name);
        }
        
        if (textTransform != null)
        {
            TextMeshProUGUI textCopy = textTransform.GetComponent<TextMeshProUGUI>();
            if (textCopy != null)
            {
                textCopy.text = currentPendingConclusion.itemName;
            }
        }
        
        // 添加结论项组件
        ConclusionItem conclusionItem = conclusionObj.GetComponent<ConclusionItem>();
        if (conclusionItem == null)
        {
            conclusionItem = conclusionObj.AddComponent<ConclusionItem>();
        }
        conclusionItem.SetConclusion(currentPendingConclusion);
        
        // 移除确认按钮（如果存在）
        Button confirmButton = conclusionObj.GetComponentInChildren<Button>();
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }
        
        // 添加到列表
        stampedConclusions.Add(conclusionObj);
        
        // 移动到位置
        MoveToPosition(targetPosition, currentConclusionIndex, currentPendingConclusion);
        
        // 增加索引
        currentConclusionIndex++;
    }
    
    // 获取下一个可用位置
    private Transform GetNextPosition()
    {
        if (conclusionTargetPositions == null || conclusionTargetPositions.Length == 0)
        {
            Debug.LogError("[ConclusionDisplayManager] 未设置结论目标位置!");
            return null;
        }
        
        // 循环使用位置
        int index = currentConclusionIndex % conclusionTargetPositions.Length;
        return conclusionTargetPositions[index];
    }
    
    // 移动到指定位置
    private void MoveToPosition(Transform targetPosition, int positionIndex, ItemSO conclusionItem)
    {
        if (targetPosition == null || stampedConclusions.Count == 0) return;
        
        // 获取最近添加的结论对象
        GameObject stamp = stampedConclusions[stampedConclusions.Count - 1];
        if (stamp == null) return;
        
        RectTransform stampRect = stamp.GetComponent<RectTransform>();
        if (stampRect == null) return;
        
        // 保存原始尺寸
        Vector2 originalSizeDelta = stampRect.sizeDelta;
        
        // 设置位置
        stampRect.SetParent(targetPosition, false);
        stampRect.anchoredPosition = Vector2.zero;
        
        // 保持原始尺寸
        stampRect.sizeDelta = originalSizeDelta;
        
        // 保持所有预设的锚点和对齐方式
        stampRect.anchorMin = new Vector2(0.5f, 0.5f);
        stampRect.anchorMax = new Vector2(0.5f, 0.5f);
        stampRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 设置适合的缩放
        float targetScale = 0.8f; // 缩小一点
        
        // 动画准备
        stampRect.localScale = Vector3.zero;
        
        // 使用DoTween执行动画
        stampRect.DOScale(Vector3.one * targetScale, confirmAnimDuration).SetEase(Ease.OutBack);
        
        // 添加点击处理组件
        ConclusionItem conclusionComp = stamp.GetComponent<ConclusionItem>();
        if (conclusionComp != null)
        {
            conclusionComp.SetAnimating(true);
            
            // 动画完成后停止动画状态并更新原始缩放值
            DOTween.Sequence()
                .AppendInterval(confirmAnimDuration)
                .OnComplete(() => {
                    conclusionComp.SetAnimating(false);
                    // 更新ConclusionItem的原始缩放值
                    conclusionComp.UpdateOriginalScale();
                });
        }
    }
    
    // 点击印章结论处理
    public void OnStampedConclusionClick(GameObject stamp)
    {
        // 不执行任何操作，结论的悬停效果已经在ConclusionItem类中实现
        return;
    }
    
    // 检查结论是否已确认
    public bool IsConfirmedConclusion(ItemSO conclusion)
    {
        if (conclusion == null) return false;
        
        return confirmedConclusions.Contains(conclusion);
    }
    
    // 获取结论面板
    public GameObject GetConclusionPanel()
    {
        return conclusionPanel;
    }
    
    // 获取已确认的结论列表
    public List<ItemSO> GetConfirmedConclusions()
    {
        return confirmedConclusions;
    }
    
    // 只显示结论详情，不显示确认按钮
    public void ShowConclusionDetail(ItemSO resultItem)
    {
        if (resultItem == null) 
        {
            Debug.LogError("[ConclusionDisplayManager] 错误: 结论物品为空!");
            return;
        }
        
        // 激活结论面板
        if (conclusionPanel != null)
        {
            conclusionPanel.SetActive(true);
            
            // 设置结论图片
            if (conclusionImage != null)
            {
                conclusionImage.sprite = resultItem.itemIcon;
                conclusionImage.enabled = true;
            }
            
            // 确保底板图片显示
            if (backgroundImage != null)
            {
                backgroundImage.enabled = true;
            }
            
            // 设置结论文本
            if (conclusionText != null)
            {
                conclusionText.text = resultItem.itemName + "\n\n" + resultItem.itemDescription;
            }
            
            // 隐藏确认按钮，因为这是已确认的结论
            if (confirmConclusionButton != null)
            {
                confirmConclusionButton.gameObject.SetActive(false);
            }
            
            // 播放结论动画
            PlayConclusionAnimation();
        }
        else
        {
            Debug.LogError("[ConclusionDisplayManager] 错误: conclusionPanel未设置!");
        }
    }
    
    // 辅助方法：查找相同名称的子对象
    private Transform FindChildWithSameName(Transform parent, string name)
    {
        if (parent == null) return null;
        
        // 直接检查父对象
        if (parent.name == name)
            return parent;
        
        // 遍历所有子对象
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            // 递归搜索
            Transform found = FindChildWithSameName(child, name);
            if (found != null)
                return found;
        }
        
        return null;
    }
}