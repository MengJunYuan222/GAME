using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class ReasoningSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("文本组件")]
    [SerializeField] private TextMeshProUGUI itemNameText;  // 物品名称文本
    [SerializeField] private TextMeshProUGUI itemDescText;  // 物品描述文本
    
    // 当前物品
    private Item currentItem;
    
    // 是否是克隆的物品（如果是克隆的，则不应该在移除时返回到背包）
    private bool isClonedItem = false;
    
    // 原始物品在背包中的索引
    private int originalItemIndex = -1;
    
    // 事件
    public event Action<Item> OnItemDropped;    // 物品放入事件
    public event Action OnItemRemoved;          // 物品移除事件
    public event Action<Item> OnItemClicked;    // 物品点击事件
    public event Action OnSlotClicked;          // 槽位点击事件

    private void Awake()
    {
        // 查找文本组件
        if (itemNameText == null)
        {
            itemNameText = transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            if (itemNameText == null)
                Debug.LogWarning($"未找到 ItemName 文本组件");
        }
        
        if (itemDescText == null)
        {
            itemDescText = transform.Find("ItemDescription")?.GetComponent<TextMeshProUGUI>();
            if (itemDescText == null)
                Debug.LogWarning($"未找到 ItemDescription 文本组件");
        }
        
        // 清空文本
        if (itemNameText != null)
        {
            itemNameText.text = "";
            itemNameText.gameObject.SetActive(false);
        }
        
        if (itemDescText != null)
        {
            itemDescText.text = "";
            itemDescText.gameObject.SetActive(false);
        }
    }

    // 实现点击接口
    public void OnPointerClick(PointerEventData eventData)
    {
        // 如果是右键点击，则移除物品
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!IsEmpty())
            {
                // 如果有物品，则移除它
                Item removedItem = currentItem;
                ClearSlot();
                
                // 如果是克隆的物品，不需要返回背包
                if (!isClonedItem && InventoryManager.Instance != null && removedItem != null)
                {
                    InventoryManager.Instance.AddItem(removedItem);
                }
            }
        }
        // 如果是左键点击
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 如果槽位有物品，触发物品点击事件
            if (!IsEmpty())
            {
                OnItemClicked?.Invoke(currentItem);
            }
            else
            {
                // 检查槽位名称，如果以"ReasoningItem_"开头，说明这是物品面板中的项，而不是推理槽
                if (!name.StartsWith("ReasoningItem_"))
                {
                    // 确保触发槽位点击事件，即使为空槽位，也需要尝试放置物品
                    OnSlotClicked?.Invoke();
                }
            }
        }
    }

    // 供ReasoningItem使用的方法，直接触发OnItemDropped事件
    public void TriggerItemDropped(Item item)
    {
        if (item != null)
        {
            OnItemDropped?.Invoke(item);
        }
    }

    // 设置物品
    public void SetItem(Item item)
    {
        if (item == null) return;

        currentItem = item;

        // 更新UI显示
        UpdateUI();
        
        // 触发OnItemDropped事件，以便通知系统进行自动合成检查
        OnItemDropped?.Invoke(item);
    }
    
    // 设置物品（克隆版本 - 不会从背包中移除原物品）
    public void SetItemAsClone(Item originalItem, int backpackIndex)
    {
        if (originalItem == null) return;
        
        // 创建物品克隆
        GameObject cloneObj = new GameObject($"ClonedItem_{originalItem.ItemName}");
        cloneObj.transform.SetParent(transform);
        
        Item clonedItem = cloneObj.AddComponent<Item>();
        clonedItem.SetItemSO(originalItem.ItemData);
        
        currentItem = clonedItem;
        isClonedItem = true;
        originalItemIndex = backpackIndex;
        
        // 更新UI显示
        UpdateUI();
        
        // 触发OnItemDropped事件，以便通知系统进行自动合成检查
        OnItemDropped?.Invoke(currentItem);
    }
    
    // 获取原始物品索引
    public int GetOriginalItemIndex()
    {
        return originalItemIndex;
    }
    
    // 是否是克隆物品
    public bool IsClonedItem()
    {
        return isClonedItem;
    }
    
    // 更新显示
    private void UpdateDisplay()
    {
        if (currentItem == null)
        {
            // 清空显示
            if (itemNameText != null)
            {
                itemNameText.text = "";
                itemNameText.gameObject.SetActive(false);
            }
            
            if (itemDescText != null)
            {
                itemDescText.text = "";
                itemDescText.gameObject.SetActive(false);
            }
            
            return;
        }
        
        // 更新名称文本
        if (itemNameText != null)
        {
            itemNameText.text = currentItem.ItemName;
            itemNameText.gameObject.SetActive(true);
        }
        
        // 更新描述文本
        if (itemDescText != null && !string.IsNullOrEmpty(currentItem.ItemDescription))
        {
            string shortDesc = currentItem.ItemDescription;
            if (shortDesc.Length > 50)
            {
                shortDesc = shortDesc.Substring(0, 50) + "...";
            }
            itemDescText.text = shortDesc;
            itemDescText.gameObject.SetActive(true);
        }
        else if (itemDescText != null)
        {
            itemDescText.text = "无描述";
            itemDescText.gameObject.SetActive(true);
        }
    }

    // 清空槽
    public void ClearSlot()
    {
        Item oldItem = currentItem;
        currentItem = null;
        
        // 如果是克隆的物品，在清除时销毁游戏对象
        if (isClonedItem && oldItem != null)
        {
            Destroy(oldItem.gameObject);
        }
        
        // 重置克隆状态
        isClonedItem = false;
        originalItemIndex = -1;
        
        // 清空文本
        if (itemNameText != null)
        {
            itemNameText.text = "";
            itemNameText.gameObject.SetActive(false);
        }
        
        if (itemDescText != null)
        {
            itemDescText.text = "";
            itemDescText.gameObject.SetActive(false);
        }

        // 触发物品移除事件
        OnItemRemoved?.Invoke();
    }

    // 高亮显示 - 文本模式下不需要高亮框
    public void Highlight(bool state)
    {
        // 在纯文本模式下，可以通过改变文本颜色来实现高亮
        if (itemNameText != null && currentItem != null)
        {
            itemNameText.color = state ? Color.yellow : Color.white;
        }
    }

    // 获取当前物品
    public Item GetItem() => currentItem;

    // 判断槽是否为空
    public bool IsEmpty() => currentItem == null;
    
    // 更新显示（当有物品放入时调用）
    public void UpdateUI()
    {
        UpdateDisplay();
    }
    
    // 重置所有事件
    public void ResetEvents()
    {
        // 清空所有事件订阅者
        OnItemDropped = null;
        OnItemRemoved = null;
        OnItemClicked = null;
        OnSlotClicked = null;
    }
} 