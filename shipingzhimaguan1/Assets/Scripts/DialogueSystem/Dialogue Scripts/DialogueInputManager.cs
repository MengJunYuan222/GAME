using UnityEngine;
using UnityEngine.SceneManagement;
using DialogueSystem;

/// <summary>
/// 全局对话输入管理器，负责处理与对话系统相关的所有输入
/// </summary>
public class DialogueInputManager : MonoBehaviour
{
    private static DialogueInputManager _instance;
    public static DialogueInputManager Instance => _instance;
    
    [Header("输入设置")]
    [SerializeField] private float inputCooldown = 0.3f;
    [SerializeField] private KeyCode advanceDialogueKey = KeyCode.Space;
    [SerializeField] private bool debugMode = false; // 设为false禁用所有调试日志
    
    // 输入状态
    private float lastInputTime = 0f;
    
    // 对话状态
    public bool IsGlobalDialogueActive { get; private set; } = false;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        LogDebug("对话输入管理器初始化");
    }
    
    private void OnEnable()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 订阅对话结束事件
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.OnDialogueEndedEvent += OnDialogueEnded;
        }
    }
    
    private void OnDisable()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // 取消订阅对话结束事件
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.OnDialogueEndedEvent -= OnDialogueEnded;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载时重新订阅对话事件
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.OnDialogueEndedEvent -= OnDialogueEnded; // 防止重复订阅
            DialogueUIManager.Instance.OnDialogueEndedEvent += OnDialogueEnded;
            
            LogDebug($"场景 {scene.name} 加载，已连接对话系统事件");
        }
    }
    
    private void Update()
    {
        // 处理对话推进输入
        if (Input.GetKeyDown(advanceDialogueKey) && CanProcessInput())
        {
            lastInputTime = Time.time;
            
            // 如果当前在对话中，推进对话
            if (IsGlobalDialogueActive && DialogueUIManager.Instance != null)
            {
                LogDebug("检测到推进对话输入");
                DialogueUIManager.Instance.Next();
            }
            // 不在对话中时不处理，留给其他系统响应
        }
    }
    
    /// <summary>
    /// 设置全局对话状态
    /// </summary>
    public void SetDialogueActive(bool active)
    {
        if (IsGlobalDialogueActive != active)
        {
            IsGlobalDialogueActive = active;
            LogDebug($"全局对话状态设置为: {(active ? "活跃" : "不活跃")}");
        }
    }
    
    /// <summary>
    /// 对话结束事件处理
    /// </summary>
    private void OnDialogueEnded()
    {
        SetDialogueActive(false);
    }
    
    /// <summary>
    /// 判断是否可以处理输入（基于冷却时间）
    /// </summary>
    public bool CanProcessInput()
    {
        return Time.time > lastInputTime + inputCooldown;
    }
    
    /// <summary>
    /// 记录输入时间，防止短时间内重复输入
    /// </summary>
    public void RecordInputTime()
    {
        lastInputTime = Time.time;
    }
    
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[DialogueInput] {message}");
        }
    }
}
