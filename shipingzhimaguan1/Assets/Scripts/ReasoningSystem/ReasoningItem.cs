using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

// 实现鼠标悬停接口
public class ReasoningItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI组件")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject highlightObj;
    [SerializeField] private GameObject emptySlotImage; // 空槽图像
    
    private Item currentItem;
    private ReasoningUIManager uiManager;  // UI管理器引用
    private bool isConfirmed = false;      // 是否已确认
    
    // 点击选中物品事件
    public event Action<Item> OnItemSelected;

    private void Awake()
    {
        // 自动查找组件（如果未手动设置）
        if (itemIcon == null)
        {
            itemIcon = transform.Find("Icon")?.GetComponent<Image>();
            if (itemIcon == null)
            {
                itemIcon = GetComponentInChildren<Image>();
            }
            
            if (itemIcon == null)
                Debug.LogWarning($"未找到 Icon 组件");
        }

        if (highlightObj == null)
        {
            highlightObj = transform.Find("Highlight")?.gameObject;
            
            if (highlightObj == null)
                Debug.LogWarning($"未找到 Highlight 组件");
        }
        
        if (emptySlotImage == null)
        {
            emptySlotImage = transform.Find("EmptySlot")?.gameObject;
        }

        // 初始化状态
        SetHighlight(false);
        // 默认显示空槽
        UpdateDisplay();
    }
    
    private void Start()
    {
        // 查找UI管理器
        uiManager = FindObjectOfType<ReasoningUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("严重错误：未找到ReasoningUIManager组件！物品详情将无法正常显示。");
        }
        else
        {
            // 已成功找到ReasoningUIManager
        }
    }
    
    // 手动设置UI管理器引用，以防自动查找失败
    public void SetUIManager(ReasoningUIManager manager)
    {
        if (manager != null)
        {
            uiManager = manager;
            // 已手动设置UIManager引用
        }
    }

    // 确保UI管理器引用有效
    private bool EnsureUIManagerValid()
    {
        if (uiManager == null)
        {
            // 再次尝试查找
            uiManager = FindObjectOfType<ReasoningUIManager>();
            
            if (uiManager == null)
            {
                Debug.LogError($"[ReasoningItem] {name}：无法找到ReasoningUIManager，物品交互将无效");
                return false;
            }
        }
        return true;
    }

    public void SetItem(Item item)
    {
        currentItem = item;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (currentItem == null)
        {
            // 清空显示
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }
            
            // 显示空槽图像
            if (emptySlotImage != null)
            {
                emptySlotImage.SetActive(true);
            }
            
            return;
        }
        
        // 隐藏空槽图像
        if (emptySlotImage != null)
        {
            emptySlotImage.SetActive(false);
        }

        // 更新图标
        if (itemIcon != null)
        {
            itemIcon.sprite = currentItem.ItemIcon;
            itemIcon.enabled = true;
        }
        else
        {
            Debug.LogError($"物品 {name} 的 itemIcon 为空，无法显示物品图标");
        }
        
        // 强制Canvas重绘
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = false;
            canvas.enabled = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) 
            return;

        // 确保UI管理器有效
        if (!EnsureUIManagerValid())
            return;

        // 左键点击确认物品
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 设置为已确认状态
            isConfirmed = true;

        // 先设置高亮显示
        SetHighlight(true);

        // 触发物品选中事件
        OnItemSelected?.Invoke(currentItem);
            
            // 通知UI管理器确认选择物品
            if (uiManager != null)
            {
                // 点击选中物品，显示详情
                
                // 显示物品详情
                uiManager.ShowItemDetail(currentItem);
                
                // 确认选择
                uiManager.ConfirmItemSelection(currentItem);
            }
        }
        // 右键点击取消确认
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 如果已经确认，则取消确认
            if (isConfirmed)
            {
                isConfirmed = false;
                SetHighlight(false);
                
                // 通知UI管理器取消选择
                if (uiManager != null)
                {
                    uiManager.CancelItemSelection();
                    // 清空详情面板
                    uiManager.HideItemDetail(false);
                }
            }
        }
    }
    
    // 鼠标进入时显示物品详情
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null)
            return;
            
        // 如果已经确认，则不再显示悬停效果
        if (isConfirmed)
            return;
            
        // 设置高亮效果
        SetHighlight(true);
        
        // 通知UI管理器显示物品详情
        if (uiManager != null)
        {
            // 不再传递鼠标位置，使用固定位置
            uiManager.ShowItemDetail(currentItem);
        }
    }
    
    // 鼠标离开时恢复状态，但延迟处理详情面板的隐藏
    // 这样可以允许从一个物品移动到另一个物品时平滑切换
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentItem == null)
            return;
            
        // 如果已确认，则不隐藏高亮和详情
        if (isConfirmed)
            return;
            
        // 取消高亮效果
        SetHighlight(false);
        
        // 通知UI管理器隐藏物品详情
        // 延迟一小段时间，以便从一个物品移动到另一个物品时不闪烁
        if (uiManager != null)
        {
            // 判断是应该隐藏哪个面板
            bool isSecondPanel = uiManager.IsFirstItemConfirmed();
            StartCoroutine(DelayHideItemDetail(isSecondPanel));
        }
    }
    
    // 延迟隐藏物品详情的协程
    private IEnumerator DelayHideItemDetail(bool isSecondPanel)
    {
        // 等待一小段时间
        yield return new WaitForSeconds(0.05f);
        
        // 隐藏详情面板
        if (uiManager != null)
        {
            uiManager.HideItemDetail(isSecondPanel);
        }
    }

    public void SetHighlight(bool state)
    {
        if (highlightObj != null)
        {
            highlightObj.SetActive(state);
        }
        else
        {
            Debug.LogWarning($"物品 {name} 的 highlightObj 为空，无法设置高亮");
        }
    }

    public Item GetItem()
    {
        return currentItem;
    }
    
    // 重置物品选择事件
    public void ResetItemSelectedEvent()
    {
        // 清除事件订阅
        OnItemSelected = null;
    }
} 