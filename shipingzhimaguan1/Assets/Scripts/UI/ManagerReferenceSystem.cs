using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 管理器引用系统 - 用于在多场景架构中动态获取Manager引用
/// 避免交叉场景引用问题
/// </summary>
public class ManagerReferenceSystem : MonoBehaviour
{
    public static ManagerReferenceSystem Instance { get; private set; }
    
    [Header("UI场景设置")]
    [SerializeField] private string uiSceneName = "Ui";
    
    [Header("管理器缓存")]
    private SettingsManager _settingsManager;
    private GameManager _gameManager;
    private UIManager _uiManager;
    private AudioManager _audioManager;
    
    // 缓存有效性标记
    private bool _cacheValid = false;
    
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
        // 监听场景加载事件，在场景切换时清除缓存
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshManagerReferences();
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载后，延迟刷新引用
        StartCoroutine(DelayedRefresh());
    }
    
    private IEnumerator DelayedRefresh()
    {
        // 等待几帧确保所有Manager都已初始化
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        RefreshManagerReferences();
    }
    
    /// <summary>
    /// 刷新所有管理器引用
    /// </summary>
    public void RefreshManagerReferences()
    {
        _cacheValid = false;
        
        // 查找各种Manager
        _settingsManager = FindManagerInScene<SettingsManager>();
        _gameManager = GameManager.Instance; // GameManager使用单例
        _uiManager = UIManager.Instance; // UIManager使用单例
        _audioManager = FindManagerInScene<AudioManager>();
        
        _cacheValid = true;
        Debug.Log("[ManagerReferenceSystem] 管理器引用已刷新");
    }
    
    /// <summary>
    /// 在指定场景中查找Manager
    /// </summary>
    private T FindManagerInScene<T>() where T : MonoBehaviour
    {
        // 首先在UI场景中查找
        Scene uiScene = SceneManager.GetSceneByName(uiSceneName);
        if (uiScene.isLoaded)
        {
            GameObject[] rootObjects = uiScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                T manager = obj.GetComponentInChildren<T>();
                if (manager != null)
                    return manager;
            }
        }
        
        // 如果UI场景中没找到，在所有场景中查找
        return FindObjectOfType<T>();
    }
    
    /// <summary>
    /// 获取SettingsManager引用
    /// </summary>
    public SettingsManager GetSettingsManager()
    {
        if (!_cacheValid || _settingsManager == null)
        {
            _settingsManager = FindManagerInScene<SettingsManager>();
        }
        return _settingsManager;
    }
    
    /// <summary>
    /// 获取GameManager引用
    /// </summary>
    public GameManager GetGameManager()
    {
        if (!_cacheValid || _gameManager == null)
        {
            _gameManager = GameManager.Instance;
        }
        return _gameManager;
    }
    
    /// <summary>
    /// 获取UIManager引用
    /// </summary>
    public UIManager GetUIManager()
    {
        if (!_cacheValid || _uiManager == null)
        {
            _uiManager = UIManager.Instance;
        }
        return _uiManager;
    }
    
    /// <summary>
    /// 获取AudioManager引用
    /// </summary>
    public AudioManager GetAudioManager()
    {
        if (!_cacheValid || _audioManager == null)
        {
            _audioManager = FindManagerInScene<AudioManager>();
        }
        return _audioManager;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
