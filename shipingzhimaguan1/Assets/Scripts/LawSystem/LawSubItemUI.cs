using UnityEngine;
using TMPro;

namespace LawSystem
{
    /// <summary>
    /// 法律子条目UI组件
    /// 用于子条目预制体的标准化管理
    /// </summary>
    public class LawSubItemUI : MonoBehaviour
    {
        [Header("UI组件引用")]
        [SerializeField] private TextMeshProUGUI subTitleText;     // 子标题文本
        [SerializeField] private TextMeshProUGUI subContentText;   // 子内容文本
        
        /// <summary>
        /// 设置子条目数据
        /// </summary>
        /// <param name="subItem">子条目数据</param>
        public void SetSubItemData(LawSubItem subItem)
        {
            if (subItem == null) return;
            
            if (subTitleText != null)
            {
                subTitleText.text = subItem.subTitle;
            }
            
            if (subContentText != null)
            {
                subContentText.text = subItem.subContent;
            }
        }
        
        /// <summary>
        /// 自动查找组件（如果未手动赋值）
        /// </summary>
        private void Awake()
        {
            if (subTitleText == null)
            {
                subTitleText = transform.Find("SubTitle")?.GetComponent<TextMeshProUGUI>();
            }
            
            if (subContentText == null)
            {
                subContentText = transform.Find("SubContent")?.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
