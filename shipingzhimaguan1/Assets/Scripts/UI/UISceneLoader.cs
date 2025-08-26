using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 负责UI场景的加载和管理
/// </summary>
public class UISceneLoader : MonoBehaviour
{
    public static UISceneLoader Instance { get; private set; }
    
    [Header("配置")]
    [SerializeField] private string uiSceneName = "Ui";
    [SerializeField] private bool loadUISceneOnStart = true;
    [SerializeField] private bool logUISceneStatus = true;
    
    private bool _isUISceneLoaded = false;
    
    /// <summary>
    /// UI场景是否已加载
    /// </summary>
    public bool IsUISceneLoaded => _isUISceneLoaded;
    
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
    }
    
    private void Start()
    {
        if (loadUISceneOnStart)
        {
            LoadUIScene();
        }
    }
    
    private void OnEnable()
    {
        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// 场景加载完成时检查UI场景
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果加载的不是UI场景，确保UI场景仍然加载
        if (scene.name != uiSceneName)
        {
            EnsureUISceneLoaded();
        }
    }
    
    /// <summary>
    /// 加载UI场景
    /// </summary>
    public void LoadUIScene()
    {
        StartCoroutine(LoadUISceneAsync());
    }
    
    /// <summary>
    /// 确保UI场景已加载
    /// </summary>
    public void EnsureUISceneLoaded()
    {
        if (!IsSceneLoaded(uiSceneName))
        {
            LoadUIScene();
        }
    }
    
    /// <summary>
    /// 异步加载UI场景
    /// </summary>
    private IEnumerator LoadUISceneAsync()
    {
        if (IsSceneLoaded(uiSceneName))
        {
            if (logUISceneStatus)
                Debug.Log("[UISceneLoader] UI场景已经加载");
            
            _isUISceneLoaded = true;
            yield break;
        }
        
        if (logUISceneStatus)
            Debug.Log("[UISceneLoader] 开始加载UI场景");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        _isUISceneLoaded = true;
        
        if (logUISceneStatus)
            Debug.Log("[UISceneLoader] UI场景加载完成");
        
        // 通知UIManager更新引用
        UIManager manager = UIManager.Instance;
        if (manager != null)
        {
            manager.FindAndUpdateReferences();
        }
        else if (logUISceneStatus)
        {
            Debug.LogWarning("[UISceneLoader] 未找到UIManager实例");
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
}
