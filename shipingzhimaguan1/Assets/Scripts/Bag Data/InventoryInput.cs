using UnityEngine;
using UnityEngine.UI;
using System;

public class InventoryInput : MonoBehaviour
{
    [Header("背包系统")]
    [SerializeField] private GameObject bagPanel;
    
    [Header("子界面")]
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private GameObject recordPanel;
    
    [Header("切换按钮")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Button recordButton;
    
    private bool isBagVisible = false;
    private bool isFromDialogue = false; // 标记是否是从对话系统打开的
    
    // 事件
    public Action OnBagOpened;
    public Action OnBagClosed;
    public Action OnInventoryCancelled; // 取消选择时的事件

    private UIManager uiManager;

    private void Start()
    {
        // 查找UIManager
        uiManager = UIManager.Instance;
        
        InitializeUI();
        RegisterButtonListeners();
    }
    
    private void InitializeUI()
    {
        // 初始状态：隐藏背包
        if (bagPanel != null)
        {
            bagPanel.SetActive(false);
        }
        isBagVisible = false;
        
        // 默认显示物品界面
        SwitchToItemPanel();
    }
    
    private void RegisterButtonListeners()
    {
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(SwitchToItemPanel);
        }
        
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(SwitchToRecordPanel);
        }
    }

    private void Update()
    {
        // 快捷键检测移至UIManager，避免冲突
        // 此处不再检测toggleKey
    }

    // 切换背包显示状态
    public void ToggleBag()
    {
        if (uiManager != null)
        {
            // 让UIManager处理背包的显示/隐藏
            uiManager.ToggleInventory();
            
            // 同步本地状态
            isBagVisible = uiManager.IsInventoryOpen();
            
            // 保留对话系统相关逻辑
            if (!isBagVisible && isFromDialogue)
            {
                isFromDialogue = false;
                if (OnInventoryCancelled != null)
                {
                    OnInventoryCancelled.Invoke();
                }
            }
        }
        else
        {
            // 当UIManager不可用时的回退逻辑
            isBagVisible = !isBagVisible;
            bagPanel.SetActive(isBagVisible);
            
            // 触发相应事件
            if (isBagVisible)
            {
                if (OnBagOpened != null)
                {
                    OnBagOpened.Invoke();
                }
            }
            else
            {
                if (OnBagClosed != null)
                {
                    OnBagClosed.Invoke();
                }
                
                if (isFromDialogue)
                {
                    isFromDialogue = false;
                    if (OnInventoryCancelled != null)
                    {
                        OnInventoryCancelled.Invoke();
                    }
                }
            }
        }
    }
    
    // 由对话系统调用，打开背包以选择物品
    public void OpenBagForItemSelection()
    {
        isFromDialogue = true;
        
        // 通知UIManager当前处于对话状态
        if (uiManager != null)
        {
            uiManager.SetDialogueState(true);
        }
        
        // 如果背包没有打开，打开背包
        if (!isBagVisible)
        {
            ToggleBag();
        }
        
        // 确保显示物品面板
        SwitchToItemPanel();
    }
    
    // 对话结束时调用
    public void DialogueEnded()
    {
        if (uiManager != null)
        {
            uiManager.SetDialogueState(false);
        }
        
        isFromDialogue = false;
    }
    
    // 切换到物品界面
    public void SwitchToItemPanel()
    {
        SetPanelActive(itemPanel, true);
        SetPanelActive(recordPanel, false);
        UpdateButtonState(itemButton, recordButton);
    }
    
    // 切换到记录界面
    public void SwitchToRecordPanel()
    {
        SetPanelActive(itemPanel, false);
        SetPanelActive(recordPanel, true);
        UpdateButtonState(recordButton, itemButton);
    }
    
    // 设置面板激活状态
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
    
    // 更新按钮状态
    private void UpdateButtonState(Button selectedButton, Button otherButton)
    {
        if (selectedButton != null && otherButton != null)
        {
            selectedButton.interactable = false; // 当前选中按钮禁用交互
            otherButton.interactable = true;
        }
    }
    
    // 获取背包是否可见
    public bool IsBagVisible() => isBagVisible;
    
    // 获取是否是从对话系统打开的
    public bool IsFromDialogue() => isFromDialogue;
}
