using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DialogueSystem;

/// <summary>
/// 确保对话系统在场景切换时保持连续性
/// </summary>
public class DialogueContinuityManager : MonoBehaviour
{
    private static DialogueContinuityManager _instance;
    public static DialogueContinuityManager Instance => _instance;
    
    [Header("配置")]
    [SerializeField] private string uiSceneName = "Ui";
    [SerializeField] private float reconnectDelay = 0.2f;
    [SerializeField] private bool debugMode = true;
    
    // 对话状态缓存
    private class DialogueState
    {
        public DialogueNodeGraph Graph;
        public BaseNode CurrentNode;
        public bool IsActive;
        public bool IsEndNode;
    }
    
    private DialogueState _savedState;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// 在场景加载后恢复对话状态
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果不是UI场景且有保存的对话状态，启动恢复流程
        if (scene.name != uiSceneName && _savedState != null && _savedState.IsActive)
        {
            StartCoroutine(RestoreDialogueAfterDelay());
        }
    }
    
    /// <summary>
    /// 在场景切换前保存当前对话状态
    /// </summary>
    public void SaveDialogueState()
    {
        if (DialogueUIManager.Instance == null) return;
        
        var manager = DialogueUIManager.Instance;
        
        // 检查对话是否正在进行
        bool isDialogueActive = manager.DialogueCanvas != null && 
                               manager.DialogueCanvas.activeInHierarchy;
                               
        if (!isDialogueActive || manager.CurrentGraph == null) return;
        
        // 保存对话状态
        _savedState = new DialogueState
        {
            Graph = manager.CurrentGraph,
            CurrentNode = manager.CurrentGraph.currentNode,
            IsActive = true,
            IsEndNode = manager.IsCurrentNodeEndNode
        };
        
        if (debugMode)
            Debug.Log($"[DialogueContinuity] 已保存对话状态: {_savedState.Graph.name}, 节点: {_savedState.CurrentNode?.name}");
    }
    
    /// <summary>
    /// 延迟恢复对话，确保所有组件已就绪
    /// </summary>
    private IEnumerator RestoreDialogueAfterDelay()
    {
        // 等待所有场景组件初始化
        yield return new WaitForSeconds(reconnectDelay);
        
        // 确保UI场景已加载
        if (!IsSceneLoaded(uiSceneName))
        {
            if (debugMode)
                Debug.Log("[DialogueContinuity] UI场景未加载，正在加载...");
                
            yield return SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
            
            // 额外等待UI场景初始化
            yield return new WaitForSeconds(0.1f);
        }
        
        // 获取DialogueUIManager实例
        var manager = DialogueUIManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[DialogueContinuity] 无法找到DialogueUIManager实例");
            yield break;
        }
        
        if (_savedState != null && _savedState.IsActive && _savedState.Graph != null)
        {
            if (debugMode)
                Debug.Log($"[DialogueContinuity] 恢复对话: {_savedState.Graph.name}");
            
            // 重新连接对话图与UI管理器
            _savedState.Graph.SetUIManager(manager);
            
            // 确保UI显示
            if (manager.DialogueCanvas != null)
                manager.DialogueCanvas.SetActive(true);
            
            // 更新当前对话图引用
            manager.SetDialogueGraph(_savedState.Graph);
            
            // 如果是DialogueNode，直接从该节点恢复对话内容
            if (_savedState.CurrentNode is DialogueNode dialogueNode)
            {
                manager.StartDialogueFromNode(dialogueNode);
                manager.SetCurrentNodeAsEndNode(_savedState.IsEndNode);
            }
            else if (_savedState.Graph.currentNode != null)
            {
                // 直接处理当前节点
                _savedState.Graph.ProcessCurrentNode();
            }
            
            // 清除保存的状态，避免重复恢复
            _savedState = null;
        }
    }
    
    /// <summary>
    /// 检查场景是否已加载
    /// </summary>
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName && scene.isLoaded)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 清除保存的对话状态
    /// </summary>
    public void ClearDialogueState()
    {
        _savedState = null;
        if (debugMode)
            Debug.Log("[DialogueContinuity] 已清除对话状态");
    }
}
