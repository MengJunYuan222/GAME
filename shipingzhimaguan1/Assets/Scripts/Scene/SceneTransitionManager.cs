using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using DialogueSystem;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    
    [System.Serializable]
    public class PortalData
    {
        public string fromScene;
        public string toScene;
        public string portalID;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
    }
    
    [Header("场景传送门配置")]
    public List<PortalData> portals = new List<PortalData>();
    
    [Header("UI重置设置")]
    [Tooltip("是否在场景切换后重置所有UI状态")]
    public bool resetUIOnSceneChange = true;
    [Tooltip("是否在控制台输出UI重置日志")]
    public bool logUIReset = true;
    
    [Header("UI场景设置")]
    [Tooltip("UI场景的名称")]
    public string uiSceneName = "Ui";
    [Tooltip("是否始终保持UI场景加载")]
    public bool keepUISceneLoaded = true;
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private string lastSceneName;
    private string lastPortalID;
    private Vector3 lastPosition;
    
    // 加载状态控制
    private bool isLoadingUIScene = false;
    private bool isTransitioning = false;
    
    private void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 确保游戏对象名称正确
        gameObject.name = "SceneTransitionManager";
    }
    
    private void Start()
    {
        // 初始化
        if (enableDebugLogs)
            Debug.Log($"[SceneManager] 初始化在场景: {SceneManager.GetActiveScene().name}");
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果是UI场景，重置加载状态并返回
        if (scene.name == uiSceneName)
        {
            isLoadingUIScene = false;
            if (enableDebugLogs)
                Debug.Log($"[SceneManager] UI场景加载完成: {scene.name}");
            return;
        }
            
        // 记录当前场景和上一个场景
        string previousSceneName = lastSceneName;
        lastSceneName = scene.name;
        
        // 仅在调试模式下输出日志
        if (enableDebugLogs)
            Debug.Log($"[SceneManager] 加载场景: {lastSceneName}, 上一个场景: {previousSceneName}, 传送门ID: {lastPortalID}");
        
        // 重置所有UI状态
        ResetAllUIState();
        
        // 如果UI场景未加载且没有正在加载，则加载它
        if (!IsSceneLoaded(uiSceneName) && !isLoadingUIScene && keepUISceneLoaded)
        {
            StartCoroutine(LoadUISceneAsync());
        }
        else
        {
            // 延迟重连对话系统引用
            StartCoroutine(DelayedDialogueReconnect());
        }
        
        // 重置切换状态
        isTransitioning = false;
    }
    
    // 异步加载UI场景
    private IEnumerator LoadUISceneAsync()
    {
        // 如果已经在加载中，直接返回
        if (isLoadingUIScene)
        {
            if (enableDebugLogs)
                Debug.Log("[SceneManager] UI场景正在加载中，跳过重复加载");
            yield break;
        }
        
        // 检查UI场景是否已经存在
        if (IsSceneLoaded(uiSceneName))
        {
            // UI场景已加载，无需重复加载
            if (enableDebugLogs)
                Debug.Log("[SceneManager] UI场景已加载，跳过加载过程");
                
            // 更新UI引用
            if (UIManager.Instance != null)
            {
                UIManager.Instance.FindAndUpdateReferences();
            }
            
            // 延迟重连对话系统引用
            StartCoroutine(DelayedDialogueReconnect());
            yield break;
        }
        
        // 双重检查：确保没有多个UI场景正在加载
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == uiSceneName)
            {
                if (enableDebugLogs)
                    Debug.Log($"[SceneManager] 发现已存在的UI场景: {scene.name}，跳过加载");
                yield break;
            }
        }
        
        // 设置加载状态
        isLoadingUIScene = true;
        
        if (enableDebugLogs)
            Debug.Log($"[SceneManager] 开始加载UI场景: {uiSceneName}");
            
        // 异步加载UI场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = true;
        
        // 等待加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // 输出日志
        if (enableDebugLogs)
            Debug.Log("[SceneManager] UI场景加载完成");
            
        // 更新UI引用
        if (UIManager.Instance != null)
        {
            UIManager.Instance.FindAndUpdateReferences();
        }
        
        // 延迟重连对话系统引用
        StartCoroutine(DelayedDialogueReconnect());
        
        // 重置加载状态
        isLoadingUIScene = false;
    }
    
    // 延迟重连对话系统引用
    private IEnumerator DelayedDialogueReconnect()
    {
        // 等待一帧，确保所有对象都已初始化
        yield return null;
        
        // 查找场景中的DialogueReferenceManager并触发重连
        var refManager = FindObjectOfType<DialogueReferenceManager>();
        if (refManager != null)
        {
            // 使用SendMessage避免直接依赖
            refManager.SendMessage("ReconnectDialogueReferences", null, SendMessageOptions.DontRequireReceiver);
        }
    }
    
    // 重置所有UI状态
    private void ResetAllUIState()
    {
        // 重置对话UI
        ResetDialogueUI();
        
        // 重置主UI
        ResetMainUI();
        
        // 输出日志
        if (enableDebugLogs)
            Debug.Log("[SceneManager] 已重置所有UI状态");
    }
    
    // 重置对话UI
    private void ResetDialogueUI()
    {
        try
        {
            // 查找对话UI管理器
            var dialogueUIManager = FindObjectOfType<DialogueSystem.DialogueUIManager>();
            
            // 如果找到了对话UI管理器
            if (dialogueUIManager != null)
            {
                // 尝试调用ResetUI方法
                if (dialogueUIManager.GetType().GetMethod("ResetUI") != null)
                {
                    dialogueUIManager.SendMessage("ResetUI", null, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    // 尝试隐藏对话画布
                    Transform dialogueCanvas = dialogueUIManager.transform.Find("DialogueCanvas");
                    if (dialogueCanvas != null)
                    {
                        dialogueCanvas.gameObject.SetActive(false);
                    }
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[SceneManager] 未找到DialogueUIManager实例");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SceneManager] 重置对话UI时出错: {e.Message}");
        }
    }
    
    // 重置主UI
    private void ResetMainUI()
    {
        try
        {
            // 查找UI管理器
            UIManager uiManager = UIManager.Instance;
            
            // 如果找到了UI管理器
            if (uiManager != null)
            {
                // 关闭所有面板
                uiManager.CloseAllPanels();
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[SceneManager] 未找到UIManager实例");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SceneManager] 重置主UI时出错: {e.Message}");
        }
    }
    
    // 通过传送门切换场景
    public void TransitionToScene(string targetScene, string portalID)
    {
        // 防止重复切换
        if (isTransitioning)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[SceneManager] 正在切换场景中，忽略新的切换请求: {targetScene}");
            return;
        }
        
        // 设置切换状态
        isTransitioning = true;
        
        // 保存当前对话状态
        if (DialogueContinuityManager.Instance != null)
        {
            DialogueContinuityManager.Instance.SaveDialogueState();
        }
        
        // 保存玩家当前位置信息
        lastSceneName = SceneManager.GetActiveScene().name;
        lastPortalID = portalID;
        
        // 如果是切换到UI场景，使用叠加模式
        if (targetScene == uiSceneName)
        {
            StartCoroutine(LoadUISceneAsync());
            isTransitioning = false; // UI场景加载不算场景切换
            return;
        }
        
        // 保存玩家位置
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            lastPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
            
            if (enableDebugLogs)
                Debug.Log($"[SceneManager] 保存玩家位置: {lastPosition}, 准备切换到场景: {targetScene}");
        }
        
        // 保存物品数据
        if (InventorySaver.Instance != null)
        {
            if (enableDebugLogs)
                Debug.Log("[SceneManager] 保存物品数据");
                
            InventorySaver.Instance.SaveInventory();
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("[SceneManager] 未找到 InventorySaver 实例，无法保存物品数据");
        }
        
        // 切换场景 - 保留UI场景
        StartCoroutine(TransitionToSceneCoroutine(targetScene, portalID));
    }
    
    private IEnumerator TransitionToSceneCoroutine(string targetScene, string portalID)
    {
        if (enableDebugLogs)
            Debug.Log($"[SceneManager] 正在切换到场景: {targetScene}, 传送门ID: {portalID}");
        
        // 如果UI场景存在且需要保留，先卸载当前游戏场景，再加载新场景
        if (keepUISceneLoaded && IsSceneLoaded(uiSceneName))
        {
            // 获取当前活动场景
            Scene currentScene = SceneManager.GetActiveScene();
            
            // 如果当前场景不是UI场景，先卸载它
            if (currentScene.name != uiSceneName)
            {
                // 异步卸载当前场景
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentScene);
                while (!unloadOp.isDone)
                {
                    yield return null;
                }
            }
            
            // 使用叠加模式加载目标场景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
            
            // 等待加载完成
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            // 设置新场景为活动场景
            Scene newScene = SceneManager.GetSceneByName(targetScene);
            if (newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
            }
        }
        else
        {
            // 常规场景加载（不保留UI场景）
            SceneManager.LoadScene(targetScene);
            
            // 重新加载UI场景
            if (keepUISceneLoaded)
            {
                yield return StartCoroutine(LoadUISceneAsync());
            }
        }
    }
    
    // 找到对应的传送点并放置玩家
    private void PlacePlayerAtPortalDestination(string fromScene, string toScene, string portalID)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) 
        {
            if (enableDebugLogs)
                Debug.LogWarning("[SceneManager] 未找到标签为 'Player' 的游戏对象，无法设置位置");
            return;
        }
        
        // 查找匹配的传送门
        foreach (var portal in portals)
        {
            if (portal.fromScene == fromScene && portal.toScene == toScene && portal.portalID == portalID)
            {
                player.transform.position = portal.spawnPosition;
                player.transform.eulerAngles = portal.spawnRotation;
                
                if (enableDebugLogs)
                    Debug.Log($"[SceneManager] 放置玩家到位置: {portal.spawnPosition}，来自传送门: {portalID}");
                return;
            }
        }
        
        if (enableDebugLogs)
            Debug.LogWarning($"[SceneManager] 未找到匹配的传送门: 从{fromScene}到{toScene}，ID:{portalID}");
    }
    
    // 检查场景是否已加载
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
} 