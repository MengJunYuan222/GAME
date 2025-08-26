using UnityEngine;

namespace LawSystem
{
    /// <summary>
    /// 法律子条目
    /// </summary>
    [System.Serializable]
    public class LawSubItem
    {
        [Tooltip("子条目名称")]
        public string subTitle = "子条目";
        
        [Tooltip("子条目内容（简短描述）")]
        [TextArea(2, 4)]
        public string subContent = "在此输入简短的法律条目内容...";
    }
    /// <summary>
    /// 法律数据ScriptableObject
    /// 用于存储法律条目的标题和内容
    /// </summary>
    [CreateAssetMenu(fileName = "New Law Data", menuName = "Law System/Law Data", order = 1)]
    public class LawDataSO : ScriptableObject
    {
        [Header("法律基本信息")]
        [Tooltip("法律条目标题")]
        public string lawTitle = "法律标题";
        
        [Tooltip("法律子条目列表")]
        public LawSubItem[] subItems = new LawSubItem[0];
        
        [Header("显示设置")]
        [Tooltip("在按钮上显示的简短名称")]
        public string buttonDisplayName = "法律";
        
        /// <summary>
        /// 获取格式化的法律内容（包含所有子条目）
        /// </summary>
        public string GetFormattedContent()
        {
            if (subItems == null || subItems.Length == 0)
                return "暂无内容";

            var content = "";
            for (int i = 0; i < subItems.Length; i++)
            {
                var subItem = subItems[i];
                if (subItem != null)
                {
                    content += $"【{subItem.subTitle}】\n{subItem.subContent}";
                    if (i < subItems.Length - 1)
                        content += "\n\n"; // 条目间空行
                }
            }
            
            return content;
        }
        
        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(buttonDisplayName) ? lawTitle : buttonDisplayName;
        }
        
        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(lawTitle) && subItems != null && subItems.Length > 0;
        }
    }
}
