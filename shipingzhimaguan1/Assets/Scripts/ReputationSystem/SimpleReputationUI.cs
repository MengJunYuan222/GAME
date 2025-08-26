using UnityEngine;
using TMPro;

namespace ReputationSystem
{
    public class SimpleReputationUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI reputationText;
        
        private void Start()
        {
            // 确保有引用
            if (reputationText == null)
            {
                Debug.LogError("声望文本引用为空！请设置引用。");
                return;
            }
            
            // 注册事件
            if (SimpleReputationManager.Instance != null)
            {
                // 显示初始值
                UpdateReputationText(0, SimpleReputationManager.Instance.CurrentReputation);
                
                // 注册声望变化事件
                SimpleReputationManager.Instance.OnReputationChanged += UpdateReputationText;
            }
            else
            {
                Debug.LogError("SimpleReputationManager实例不存在！请确保场景中存在该组件。");
            }
        }
        
        private void OnDestroy()
        {
            // 取消事件注册
            if (SimpleReputationManager.Instance != null)
            {
                SimpleReputationManager.Instance.OnReputationChanged -= UpdateReputationText;
            }
        }
        
        // 更新声望文本显示
        private void UpdateReputationText(int oldValue, int newValue)
        {
            if (reputationText != null)
            {
                reputationText.text = newValue.ToString();
            }
        }
    }
} 