using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject bagPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject itemDetailPanel;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;

    // 事件系统
    public Action<Item> OnItemAdded;
    public Action<int> OnItemRemoved;
    public Action OnInventoryChanged;
    public Action<ItemSlot> OnSlotSelected;

    private ItemSlot[] itemSlots;
    private ItemSlot currentSelectedSlot;
    private Dictionary<int, ItemSlot> itemIdToSlot = new Dictionary<int, ItemSlot>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        if (slotsParent == null) return;

        itemSlots = slotsParent.GetComponentsInChildren<ItemSlot>(true);
        foreach (var slot in itemSlots)
        {
            if (slot != null)
            {
                slot.OnSlotClicked -= HandleSlotClicked;
                slot.OnSlotClicked += HandleSlotClicked;
            }
        }
        
        itemIdToSlot.Clear();
        ResetInventory();
    }

    public void HandleSlotClicked(ItemSlot clickedSlot)
    {
        if (clickedSlot.GetItem() == null) return;
        
        // 获取点击的物品数据
        Item item = clickedSlot.GetItem();
        
        // 设置详情面板内容并显示
        if (itemDetailPanel != null)
        {
            Debug.Log($"显示物品详情: {item.ItemName}");
            
            itemDetailPanel.SetActive(true);
            
            if (detailNameText != null) detailNameText.text = item.ItemName;
            if (detailDescriptionText != null) 
            {
                detailDescriptionText.text = item.ItemDescription;
            }
        }
        else
        {
            Debug.LogError("物品详情面板引用未设置!");
        }

        // 处理物品槽的选中状态
        SelectSlot(clickedSlot);
    }

    public void SelectSlot(ItemSlot slot)
    {
        if (slot == null || slot.IsEmpty())
        {
            Debug.LogWarning("[InventoryManager] 尝试选中空槽位或空物品槽位");
            return;
        }

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(false);
        }

        Debug.Log($"[InventoryManager] 选中槽位: {slot.GetItemName()}");
        currentSelectedSlot = slot;
        slot.SetSelected(true);
        OnSlotSelected?.Invoke(slot);
    }

    public void DeselectCurrentSlot()
    {
        if (currentSelectedSlot != null)
        {
            Debug.Log($"[InventoryManager] 取消选中槽位: {currentSelectedSlot.GetItemName()}");
            currentSelectedSlot.SetSelected(false);
            currentSelectedSlot = null;
        }
    }

    public ItemSlot GetSelectedSlot() => currentSelectedSlot;

    private void OnDestroy()
    {
        if (itemSlots != null)
        {
            foreach (var slot in itemSlots)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= HandleSlotClicked;
                }
            }
        }
        
        itemIdToSlot.Clear();
    }

    public bool AddItem(Item item, bool showPrompt = true)
    {
        if (item == null || itemSlots == null || itemSlots.Length == 0) return false;

        // 找一个空槽位放置物品
        int emptySlotIndex = FindEmptySlot();
        if (emptySlotIndex >= 0)
        {
            itemSlots[emptySlotIndex].gameObject.SetActive(true);
            itemSlots[emptySlotIndex].SetItem(item);

            // 添加到字典
            itemIdToSlot[item.ItemID] = itemSlots[emptySlotIndex];

            // 只有在showPrompt为true时才显示提示
            if (showPrompt && GetProp.Instance != null)
            {
                GetProp.Instance.ShowGetItemPrompt(item.ItemName, item.ItemIcon);
            }

            OnItemAdded?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    private int FindEmptySlot()
    {
        if (itemSlots == null) return -1;
        
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null && itemSlots[i].IsEmpty())
            {
                return i;
            }
        }
        return -1;
    }

    public bool RemoveItem(int slotIndex)
    {
        if (itemSlots == null || slotIndex < 0 || slotIndex >= itemSlots.Length) return false;

        ItemSlot slot = itemSlots[slotIndex];
        if (slot != null && !slot.IsEmpty())
        {
            int itemId = slot.GetItemID();
            if (itemIdToSlot.ContainsKey(itemId))
            {
                itemIdToSlot.Remove(itemId);
            }
            
            slot.ClearSlot();
            slot.gameObject.SetActive(false);
            
            RearrangeItems();
            
            OnItemRemoved?.Invoke(slotIndex);
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }
    
    private void RearrangeItems()
    {
        List<Item> activeItems = new List<Item>();
        
        foreach (var slot in itemSlots)
        {
            if (slot != null && !slot.IsEmpty() && slot.gameObject.activeSelf)
            {
                activeItems.Add(slot.GetItem());
                slot.ClearSlot();
                slot.gameObject.SetActive(false);
            }
        }
        
        itemIdToSlot.Clear();
        
        for (int i = 0; i < activeItems.Count; i++)
        {
            itemSlots[i].gameObject.SetActive(true);
            itemSlots[i].SetItem(activeItems[i]);
            
            int itemId = activeItems[i].ItemID;
            if (!itemIdToSlot.ContainsKey(itemId))
            {
                itemIdToSlot[itemId] = itemSlots[i];
            }
        }
    }

    public void ResetInventory()
    {
        DeselectCurrentSlot();
        if (itemSlots != null)
        {
            foreach (var slot in itemSlots)
            {
                if (slot != null)
                {
                    slot.ClearSlot();
                    slot.gameObject.SetActive(false);
                }
            }
        }
        
        itemIdToSlot.Clear();
        OnInventoryChanged?.Invoke();
    }

    public ItemSlot[] GetAllItemSlots() => itemSlots;
    
    public int GetItemCount()
    {
        if (itemSlots == null) return 0;
        return itemSlots.Count(slot => slot != null && !slot.IsEmpty() && slot.gameObject.activeSelf);
    }
    
    public bool IsFull() => GetItemCount() >= itemSlots.Length;

    public bool AddItemByItemSO(ItemSO itemSO, bool showPrompt = true)
    {
        if (itemSO == null) return false;
        
        GameObject itemObject = new GameObject($"ItemData_{itemSO.itemName}");
        itemObject.transform.SetParent(transform);
        
        Item item = itemObject.AddComponent<Item>();
        item.SetItemSO(itemSO);
        
        return AddItem(item, showPrompt);
    }

    public bool AddItemByID(int itemID)
    {
        if (ItemDatabase.Instance == null) return false;
        
        ItemSO itemSO = ItemDatabase.Instance.GetItemByID(itemID);
        return itemSO != null && AddItemByItemSO(itemSO);
    }

    public List<Item> GetAllItems()
    {
        if (itemSlots == null) return new List<Item>();
            
        return itemSlots.Where(slot => slot != null && !slot.IsEmpty() && slot.gameObject.activeSelf)
                       .Select(slot => slot.GetItem())
                       .Where(item => item != null)
                       .ToList();
    }
    
    public ItemSlot GetSlotByItemId(int itemId)
    {
        if (itemIdToSlot.TryGetValue(itemId, out ItemSlot slot))
        {
            return slot;
        }
        return null;
    }
}
