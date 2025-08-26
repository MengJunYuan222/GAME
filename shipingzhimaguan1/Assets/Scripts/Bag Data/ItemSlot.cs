using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI组件")]
    [SerializeField] private TextMeshProUGUI itemNameText; // 替换原来的Image组件
    [SerializeField] private GameObject highlightObj;
    [SerializeField] private GameObject emptySlotImage;
    
    private Item currentItem;
    private bool isSelected = false;
    
    public event Action<ItemSlot> OnSlotClicked;

    private void Awake()
    {
        // 自动获取组件
        if (itemNameText == null)
        {
            itemNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (itemNameText == null)
            {
                Debug.LogError($"ItemSlot {gameObject.name} 缺少 TextMeshProUGUI 组件");
            }
        }

        if (highlightObj == null)
        {
            highlightObj = transform.Find("Select")?.gameObject;
        }

        // 初始化状态
        SetSelected(false);
        UpdateDisplay();
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
            if (itemNameText != null)
            {
                itemNameText.text = "";
            }
            
            if (emptySlotImage != null)
            {
                emptySlotImage.SetActive(true);
            }
            
            gameObject.SetActive(false);
            return;
        }

        // 显示物品名称
        if (itemNameText != null)
        {
            itemNameText.text = currentItem.ItemName;
        }
        
        if (emptySlotImage != null)
        {
            emptySlotImage.SetActive(false);
        }
        
        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (highlightObj != null)
        {
            highlightObj.SetActive(selected);
        }
    }

    public bool IsSelected() => isSelected;
    
    public bool IsEmpty() => currentItem == null;
    
    public Item GetItem() => currentItem;
    
    public string GetItemName() => currentItem?.ItemName ?? "";
    
    public int GetItemID() => currentItem?.ItemID ?? -1;
    
    public int GetStackCount() => currentItem?.StackCount ?? 0;
    
    public void AddStack(int amount)
    {
        if (currentItem != null)
        {
            currentItem.StackCount += amount;
            UpdateDisplay();
        }
    }
    
    public void RemoveStack(int amount)
    {
        if (currentItem != null)
        {
            currentItem.StackCount -= amount;
            if (currentItem.StackCount <= 0)
            {
                ClearSlot();
            }
            else
            {
                UpdateDisplay();
            }
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        SetSelected(false);
        UpdateDisplay();
    }
}
