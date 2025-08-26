using UnityEngine;

/// <summary>
/// 物品定义 - 所有物品的数据模板
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    [Header("基本信息")]
    public string itemName;
    public int itemID;
    [TextArea(3, 10)]
    public string itemDescription;
    public Sprite itemIcon;
    public ItemType itemType;

   // [Header("自定义数据")]
  //  [Tooltip("可在此添加物品特殊属性")]
   // public string customData;

    /// <summary>
    /// 用于编辑器中显示物品信息的方法
    /// </summary>
    public override string ToString()
    {
        return $"{itemName} (ID: {itemID}, 类型: {itemType})";
    }
} 