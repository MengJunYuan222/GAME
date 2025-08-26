using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// 处理物品详情面板的显示和隐藏
public class ItemDetailManager : MonoBehaviour
{
    [Header("详情面板")]
    [SerializeField] private GameObject itemDetailPanel1;
    [SerializeField] private GameObject itemDetailPanel2;
    [SerializeField] private TextMeshProUGUI itemName1;
    [SerializeField] private TextMeshProUGUI itemName2;
    [SerializeField] private TextMeshProUGUI itemDesc1;
    [SerializeField] private TextMeshProUGUI itemDesc2;
    
    [Header("详情面板外观")]
    [SerializeField] private Sprite detailPanelBackground;
    [SerializeField] private Color detailPanelColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);
    [SerializeField] private Color itemNameColor = Color.white;
    [SerializeField] private Color itemDescColor = Color.white;
    [SerializeField] private int itemNameFontSize = 32;
    [SerializeField] private int itemDescFontSize = 24;

    // 当前悬停的物品
    private Item currentHoverItem = null;
    
    private void Awake()
    {
        InitializeDetailPanels();
    }
    
    // 初始化详情面板
    public void InitializeDetailPanels()
    {
        // 检查第一个详情面板引用
        if (itemDetailPanel1 == null)
        {
            Debug.LogError("[ItemDetailManager] 错误: itemDetailPanel1未设置! 请在编辑器中手动设置引用");
        }
        else
        {
            // 检查文本组件
            if (itemName1 == null)
                Debug.LogError("[ItemDetailManager] 错误: itemName1未设置! 请在编辑器中手动设置引用");
            
            if (itemDesc1 == null)
                Debug.LogError("[ItemDetailManager] 错误: itemDesc1未设置! 请在编辑器中手动设置引用");
            
            // 确保详情面板可见
            itemDetailPanel1.SetActive(true);
            
            // 设置初始文本
            if (itemName1 != null)
            {
                itemName1.text = "请选择第一个物品";
                itemName1.gameObject.SetActive(true);
            }
            
            if (itemDesc1 != null)
            {
                itemDesc1.text = "点击物品查看详情";
                itemDesc1.gameObject.SetActive(true);
            }
        }
        
        // 检查第二个详情面板引用
        if (itemDetailPanel2 == null)
        {
            Debug.LogError("[ItemDetailManager] 错误: itemDetailPanel2未设置! 请在编辑器中手动设置引用");
        }
        else
        {
            // 检查文本组件
            if (itemName2 == null)
                Debug.LogError("[ItemDetailManager] 错误: itemName2未设置! 请在编辑器中手动设置引用");
            
            if (itemDesc2 == null)
                Debug.LogError("[ItemDetailManager] 错误: itemDesc2未设置! 请在编辑器中手动设置引用");
            
            // 确保详情面板可见
            itemDetailPanel2.SetActive(true);
            
            // 设置初始文本
            if (itemName2 != null)
            {
                itemName2.text = "请选择第二个物品";
                itemName2.gameObject.SetActive(true);
            }
            
            if (itemDesc2 != null)
            {
                itemDesc2.text = "点击物品查看详情";
                itemDesc2.gameObject.SetActive(true);
            }
        }
    }

    // 物品详情显示方法
    public void ShowItemDetail(Item item, bool isSecondPanel)
    {
        if (item == null) return;
        
        currentHoverItem = item;
        
        // 获取目标面板组件
        GameObject targetPanel = isSecondPanel ? itemDetailPanel2 : itemDetailPanel1;
        TextMeshProUGUI targetNameText = isSecondPanel ? itemName2 : itemName1;
        TextMeshProUGUI targetDescText = isSecondPanel ? itemDesc2 : itemDesc1;
        
        // 检查引用是否有效
        if (targetPanel == null || targetNameText == null || targetDescText == null)
        {
            Debug.LogError($"[ItemDetailManager] 详情面板组件引用无效! Panel: {targetPanel != null}, Name: {targetNameText != null}, Desc: {targetDescText != null}");
            return;
        }
        
        // 确保面板可见
        targetPanel.SetActive(true);
        
        // 更新内容
        targetNameText.text = item.ItemName;
        targetNameText.fontSize = itemNameFontSize;
        targetNameText.color = itemNameColor;
        
        targetDescText.text = item.ItemDescription;
        targetDescText.fontSize = itemDescFontSize;
        targetDescText.color = itemDescColor;
        
        // 尝试刷新UI
        Canvas canvas = targetPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // 简单的Canvas刷新方法
            canvas.enabled = false;
            canvas.enabled = true;
        }
        
        // 刷新布局
        if (targetPanel.GetComponent<RectTransform>() != null)
        {
            // 告知Unity更新布局
            targetNameText.SetAllDirty();
            targetDescText.SetAllDirty();
            
            // 如果有布局组件，刷新它们
            LayoutGroup layout = targetPanel.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
                layout.enabled = true;
            }
        }
    }
    
    // 隐藏物品详情方法
    public void HideItemDetail(bool isSecondPanel)
    {
        // 不再隐藏详情面板，只清空内容
        GameObject targetPanel = isSecondPanel ? itemDetailPanel2 : itemDetailPanel1;
        TextMeshProUGUI targetNameText = isSecondPanel ? itemName2 : itemName1;
        TextMeshProUGUI targetDescText = isSecondPanel ? itemDesc2 : itemDesc1;
        
        if (targetNameText != null)
        {
            targetNameText.text = isSecondPanel ? "请选择第二个物品" : "请选择第一个物品";
        }
        
        if (targetDescText != null)
        {
            targetDescText.text = "点击物品查看详情";
        }
        
        // 不隐藏面板，保持显示
        // targetPanel.SetActive(false);
    }
}