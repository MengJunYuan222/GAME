using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    [Header("传送门配置")]
    public string targetSceneName; // 目标场景名称
    public string portalID; // 传送门唯一标识符
    public bool requireKeyPress = false; // 是否需要按键触发
    public KeyCode activationKey = KeyCode.E; // 触发按键
    
    private bool playerInTrigger = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            
            if (!requireKeyPress)
            {
                TriggerPortal();
            }
            else
            {
                // 显示提示UI
                ShowInteractionPrompt(true);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            
            // 隐藏提示UI
            if (requireKeyPress)
            {
                ShowInteractionPrompt(false);
            }
        }
    }
    
    private void Update()
    {
        // 如果需要按键且玩家在触发区域内
        if (requireKeyPress && playerInTrigger)
        {
            if (Input.GetKeyDown(activationKey))
            {
                TriggerPortal();
            }
        }
    }
    
    private void ShowInteractionPrompt(bool show)
    {
        // 这里可以实现显示/隐藏交互提示UI的逻辑
        // 例如: UIManager.Instance.ShowInteractionPrompt(show, $"按 {activationKey} 进入");
        Debug.Log($"传送门提示: {(show ? "显示" : "隐藏")}");
    }
    
    public void TriggerPortal()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("传送门目标场景未设置!");
            return;
        }
        
        if (string.IsNullOrEmpty(portalID))
        {
            Debug.LogError("传送门ID未设置!");
            return;
        }
        
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log($"触发传送门: {portalID} 前往场景: {targetSceneName}");
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName, portalID);
        }
        else
        {
            Debug.LogError("场景管理器未找到! 无法使用传送门。请确保场景中存在SceneTransitionManager实例。");
        }
    }
} 