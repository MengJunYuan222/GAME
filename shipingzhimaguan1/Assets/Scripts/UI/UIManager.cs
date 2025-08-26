using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System;

public class UIManager : MonoBehaviour
{
    // 单例实例
    public static UIManager Instance { get; private set; }

    [Header("UI动画配置")]
    [Tooltip("预定义的UI动画名称列表，可在对话编辑器中直接选择")]
    public string[] predefinedAnimationNames = new string[] { "元素0", "元素1", "元素2" };

    [Header("UI场景配置")]
    [SerializeField] private string uiSceneName = "Ui";
    [SerializeField] private bool logUIOperations = true;

    [Header("提示UI")]
    public GameObject inventoryHint;
    public GameObject reasoningHint;

    [Header("面板引用")]
    [SerializeField] private GameObject inventoryPanel;  // 背包面板
    [SerializeField] private GameObject reasoningPanel;  // 推理面板
    [SerializeField] private GameObject pauseMenuPanel;  // 暂停菜单面板
    // 法律查看器管理器通过单例获取，无需序列化引用

    [Header("按钮引用")]
    [SerializeField] private Button inventoryButton;  // 背包按钮
    [SerializeField] private Button reasoningButton;  // 推理按钮
    [SerializeField] private Button settingsButton;   // 设置按钮

    [Header("快捷键设置")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.I;  // 背包快捷键
    [SerializeField] private KeyCode reasoningKey = KeyCode.R;  // 推理快捷键

    [Header("系统引用")]
    [SerializeField] private InventoryInput inventoryInput;  // 背包输入控制器
    [SerializeField] private ReasoningPanel reasoningPanelController;  // 推理面板控制器
    [SerializeField] private SettingsManager settingsManager;  // 设置管理器
    [SerializeField] private PauseMenuController pauseMenuController;  // 暂停菜单控制器

    [Header("UI音效设置")]
    [SerializeField] private bool enableUISounds = true;  // 是否启用UI音效

    // 面板状态
    private bool isInventoryOpen = false;
    private bool isReasoningOpen = false;
    private bool isPauseMenuOpen = false;
    private bool isLawViewerOpen = false;
    private bool isSentencingOpen = false;
    
    // 背包锁定状态（用于出示对话时禁止关闭背包）
    private bool isInventoryLocked = false;

    // 对话系统交互
    private bool isInDialogue = false;

    // 音频管理器引用
    private AudioManager audioManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // 确保UIManager是根游戏对象
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        // 将UIManager对象移动到UI场景中
        Scene uiScene = SceneManager.GetSceneByName(uiSceneName);
        if (uiScene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(gameObject, uiScene);
            if (logUIOperations)
                Debug.Log($"[UIManager] 已将UIManager移动到 {uiSceneName} 场景");
        }
        
        DontDestroyOnLoad(gameObject);
        gameObject.name = "UIManager (DontDestroy)";
        
        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 获取AudioManager引用
        audioManager = AudioManager.Instance;
        if (audioManager == null && enableUISounds)
        {
            Debug.LogWarning("[UIManager] 无法找到AudioManager实例，UI音效将无法播放");
        }

        InitializeReferences();
        SetupButtons();
        
        // 初始化UI状态 - 确保游戏开始时所有面板都是隐藏的
        if (inventoryPanel) inventoryPanel.SetActive(false);
        if (reasoningPanel) reasoningPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        isInventoryOpen = false;
        isReasoningOpen = false;
        isPauseMenuOpen = false;
        
        UpdateHints();

        if (InventoryManager.Instance != null)
        {
            if (logUIOperations)
                Debug.Log($"[UIManager] 初始化背包，找到 {InventoryManager.Instance.GetAllItemSlots()?.Length ?? 0} 个物品槽");
        }
        else
        {
            Debug.LogWarning("[UIManager] InventoryManager 实例不存在");
        }
        
        // 确保UI场景已加载
        EnsureUISceneLoaded();

        // 添加全局按钮点击事件监听
        AddGlobalButtonClickListener();
    }

    /// <summary>
    /// 添加全局按钮点击事件监听
    /// </summary>
    private void AddGlobalButtonClickListener()
    {
        // 获取EventSystem
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            // 添加组件以监听按钮点击
            if (eventSystem.gameObject.GetComponent<UIButtonSoundHandler>() == null)
            {
                eventSystem.gameObject.AddComponent<UIButtonSoundHandler>();
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] 无法找到EventSystem，无法添加全局按钮点击监听");
        }
    }

    /// <summary>
    /// 播放UI音效
    /// </summary>
    /// <param name="volumeScale">音量缩放（0-1）</param>
    public void PlayUISound()
    {
        if (enableUISounds && audioManager != null)
        {
            audioManager.PlayUISound();
        }
    }

    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (enableUISounds && audioManager != null)
        {
            audioManager.PlayButtonClickSound();
        }
    }

    /// <summary>
    /// 播放开关切换音效
    /// </summary>
    public void PlayToggleSound()
    {
        if (enableUISounds && audioManager != null)
        {
            audioManager.PlayToggleSound();
        }
    }

    /// <summary>
    /// 播放滑块拖动音效
    /// </summary>
    public void PlaySliderSound()
    {
        if (enableUISounds && audioManager != null)
        {
            audioManager.PlaySliderSound();
        }
    }

    /// <summary>
    /// 播放面板打开音效
    /// </summary>
    public void PlayPanelOpenSound()
    {
        if (enableUISounds && audioManager != null)
        {
            audioManager.PlayPanelOpenSound();
        }
    }

    /// <summary>
    /// 播放面板关闭音效
    /// </summary>
    public void PlayPanelCloseSound()
    {
        if (enableUISounds && audioManager != null)
        {
            audioManager.PlayPanelCloseSound();
        }
    }
    
    /// <summary>
    /// 确保UI场景已加载
    /// </summary>
    public void EnsureUISceneLoaded()
    {
        if (UISceneLoader.Instance != null)
        {
            UISceneLoader.Instance.EnsureUISceneLoaded();
        }
        else
        {
            // 如果没有UISceneLoader，尝试自己加载UI场景
            if (!IsSceneLoaded(uiSceneName))
            {
                if (logUIOperations)
                    Debug.Log("[UIManager] 尝试加载UI场景");
                    
                SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
            }
        }
    }

    private void InitializeReferences()
    {
        // 如果没有指定背包输入控制器，尝试查找
        if (inventoryInput == null)
        {
            inventoryInput = FindObjectOfType<InventoryInput>();
        }
        
        // 如果没有指定推理面板控制器，尝试查找
        if (reasoningPanelController == null)
        {
            reasoningPanelController = FindObjectOfType<ReasoningPanel>();
        }
        
        // 如果没有指定设置管理器，尝试查找
        if (settingsManager == null)
        {
            settingsManager = FindObjectOfType<SettingsManager>();
        }
        
        // 如果没有指定暂停菜单控制器，尝试查找
        if (pauseMenuController == null)
        {
            pauseMenuController = FindObjectOfType<PauseMenuController>();
        }
    }

    private void SetupButtons()
    {
        // 设置背包按钮点击事件
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(ToggleInventory);
        }
        
        // 设置推理按钮点击事件
        if (reasoningButton != null)
        {
            reasoningButton.onClick.RemoveAllListeners();
            reasoningButton.onClick.AddListener(ToggleReasoning);
        }
        
        // 设置设置按钮点击事件
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(TogglePauseMenu);
        }
    }

    private void Update()
    {
        // 检查是否在调查模式
        bool isInDetectiveMode = DetectiveMode.Instance != null && DetectiveMode.Instance.IsInDetectiveMode;
        
        // 检查背包快捷键
        if (Input.GetKeyDown(inventoryKey) && !isInDialogue && !isInDetectiveMode)
        {
            if (logUIOperations)
                Debug.Log($"[UIManager] 检测到背包快捷键 {inventoryKey}");
            ToggleInventory();
        }
        
        // 检查推理快捷键
        if (Input.GetKeyDown(reasoningKey) && !isInDialogue && !isInDetectiveMode)
        {
            if (logUIOperations)
                Debug.Log($"[UIManager] 检测到推理快捷键 {reasoningKey}");
            ToggleReasoning();
        }
        
        // 检查ESC键 - 打开/关闭暂停菜单（调查模式时不响应）
        if (Input.GetKeyDown(KeyCode.Escape) && !isInDialogue && !isInDetectiveMode)
        {
            if (logUIOperations)
                Debug.Log($"[UIManager] 检测到ESC键");
            TogglePauseMenu();
        }
    }

    // 更新提示显示
    private void UpdateHints()
    {
        if (inventoryHint) inventoryHint.SetActive(!isInventoryOpen);
        if (reasoningHint) reasoningHint.SetActive(!isReasoningOpen);
    }
    
    // 获取背包是否打开
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }
    
    // 锁定背包（禁止关闭）
    public void LockInventory(bool locked = true)
    {
        isInventoryLocked = locked;
        if (logUIOperations)
            Debug.Log($"[UIManager] 背包{(locked ? "已锁定" : "已解锁")}");
    }
    
    // 获取背包是否锁定
    public bool IsInventoryLocked()
    {
        return isInventoryLocked;
    }
    
    // 获取推理面板是否打开
    public bool IsReasoningOpen()
    {
        return isReasoningOpen;
    }
    
    // 获取暂停菜单是否打开
    public bool IsPauseMenuOpen()
    {
        return isPauseMenuOpen;
    }
    
    // 设置是否处于对话状态（对话期间可能需要禁用某些UI交互）
    public void SetDialogueState(bool inDialogue)
    {
        isInDialogue = inDialogue;
    }
    
    // 切换背包显示状态
    public void ToggleInventory()
    {
        // 确保UI场景已加载
        EnsureUISceneLoaded();
        
        if (EventSystem.current != null)
        {
            // 清除当前选中的UI元素
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // 如果是关闭操作，检查是否由空格键触发
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.Space))
        {
            return; // 忽略空格键触发的关闭操作
        }
        
        // 如果背包已经打开，关闭它
        if (isInventoryOpen)
        {
            // 如果背包处于锁定状态（如出示对话），不允许关闭
            if (isInventoryLocked)
            {
                if (logUIOperations)
                    Debug.Log("[UIManager] 背包已锁定，不允许关闭");
                return;
            }
            
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
                PlayPanelCloseSound(); // 播放面板关闭音效
            }
            
            isInventoryOpen = false;
            
            // 如果有背包输入控制器，通知它背包已关闭
            if (inventoryInput != null)
            {
                if (inventoryInput.OnBagClosed != null)
                {
                    inventoryInput.OnBagClosed.Invoke();
                }
            }
        }
        // 否则打开背包
        else
        {
            // 先关闭其他面板
            CloseAllPanels();
            
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
                PlayPanelOpenSound(); // 播放面板打开音效
            }
            
            isInventoryOpen = true;
            
            // 如果有背包输入控制器，通知它背包已打开
            if (inventoryInput != null)
            {
                if (inventoryInput.OnBagOpened != null)
                {
                    inventoryInput.OnBagOpened.Invoke();
                }
            }
        }
        
        // 更新提示显示
        UpdateHints();
        
        if (logUIOperations)
            Debug.Log($"[UIManager] 背包状态: {(isInventoryOpen ? "已打开" : "已关闭")}");
    }
    
    // 切换推理面板显示状态
    public void ToggleReasoning()
    {
        // 确保UI场景已加载
        EnsureUISceneLoaded();
        
        if (EventSystem.current != null)
        {
            // 清除当前选中的UI元素
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // 如果推理面板已经打开，关闭它
        if (isReasoningOpen)
        {
            if (reasoningPanel != null)
            {
                reasoningPanel.SetActive(false);
                PlayPanelCloseSound(); // 播放面板关闭音效
            }
            
            isReasoningOpen = false;
        }
        // 否则打开推理面板
        else
        {
            // 先关闭其他面板
            CloseAllPanels();
            
            if (reasoningPanel != null)
            {
                reasoningPanel.SetActive(true);
                PlayPanelOpenSound(); // 播放面板打开音效
            }
            
            isReasoningOpen = true;
        }
        
        // 更新提示显示
        UpdateHints();
        
        if (logUIOperations)
            Debug.Log($"[UIManager] 推理面板状态: {(isReasoningOpen ? "已打开" : "已关闭")}");
    }
    
    // 切换暂停菜单显示状态
    public void TogglePauseMenu()
    {
        // 确保UI场景已加载
        EnsureUISceneLoaded();
        
        if (EventSystem.current != null)
        {
            // 清除当前选中的UI元素
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // 如果暂停菜单已经打开，关闭它
        if (isPauseMenuOpen)
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
                PlayPanelCloseSound(); // 播放面板关闭音效
            }
            
            isPauseMenuOpen = false;
            
            // 恢复游戏时间
            Time.timeScale = 1f;
            
            // 如果有暂停菜单控制器，通知它菜单已关闭
            if (pauseMenuController != null)
            {
                pauseMenuController.ClosePauseMenu();
            }
        }
        // 否则打开暂停菜单
        else
        {
            // 先关闭其他面板
            CloseAllPanels();
            
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
                PlayPanelOpenSound(); // 播放面板打开音效
            }
            
            isPauseMenuOpen = true;
            
            // 暂停游戏时间
            Time.timeScale = 0f;
            
            // 如果有暂停菜单控制器，通知它菜单已打开
            if (pauseMenuController != null)
            {
                pauseMenuController.OpenPauseMenu();
            }
        }
        
        if (logUIOperations)
            Debug.Log($"[UIManager] 暂停菜单状态: {(isPauseMenuOpen ? "已打开" : "已关闭")}");
    }
    
    // 关闭所有面板
    public void CloseAllPanels()
    {
        // 关闭背包面板
        if (isInventoryOpen)
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
            
            isInventoryOpen = false;
            
            // 如果有背包输入控制器，通知它背包已关闭
            if (inventoryInput != null)
            {
                if (inventoryInput.OnBagClosed != null)
                {
                    inventoryInput.OnBagClosed.Invoke();
                }
            }
        }
        
        // 关闭推理面板
        if (isReasoningOpen)
        {
            if (reasoningPanel != null)
            {
                reasoningPanel.SetActive(false);
            }
            
            isReasoningOpen = false;
        }
        
        // 关闭暂停菜单
        if (isPauseMenuOpen)
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
            
            isPauseMenuOpen = false;
            
            // 恢复游戏时间
            Time.timeScale = 1f;
            
            // 如果有暂停菜单控制器，通知它菜单已关闭
            if (pauseMenuController != null)
            {
                pauseMenuController.ClosePauseMenu();
            }
        }
        
        // 关闭法律查看器
        if (isLawViewerOpen)
        {
            var lawViewerManager = LawSystem.LawViewerManager.Instance;
            if (lawViewerManager != null)
            {
                lawViewerManager.CloseLawViewer();
            }
            
            isLawViewerOpen = false;
        }
        
        // 关闭判刑界面
        if (isSentencingOpen)
        {
            var sentencingManager = LawSystem.SentencingManager.Instance;
            if (sentencingManager != null)
            {
                sentencingManager.CloseSentencing();
            }
            
            isSentencingOpen = false;
        }
        
        // 更新提示显示
        UpdateHints();
    }

    // 场景加载事件处理
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (logUIOperations)
            Debug.Log($"[UIManager] 场景切换到: {scene.name}");
        
        // 当新场景加载时，查找并更新UI引用
        FindAndUpdateReferences();
        
        // 确保UI场景已加载
        EnsureUISceneLoaded();
    }

    // 查找并更新UI引用
    public void FindAndUpdateReferences()
    {
        // 查找所有已加载场景中的UI元素
        FindUIElementsInAllScenes();
        
        // 确保必要的UI引用存在
        EnsureUIReferences();
    }

    // 在指定场景中查找UI元素
    private void FindUIElementsInScene(Scene scene)
    {
        if (!scene.isLoaded) return;
        
        // 获取场景中的所有根游戏对象
        GameObject[] rootObjects = scene.GetRootGameObjects();
        
        foreach (GameObject root in rootObjects)
        {
            // 查找背包面板
            if (inventoryPanel == null)
            {
                Transform invPanel = root.transform.Find("Canvas/InventoryPanel");
                if (invPanel != null)
                {
                    inventoryPanel = invPanel.gameObject;
                    if (logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到背包面板");
                }
            }
            
            // 查找推理面板
            if (reasoningPanel == null)
            {
                Transform reasonPanel = root.transform.Find("Canvas/ReasoningPanel");
                if (reasonPanel != null)
                {
                    reasoningPanel = reasonPanel.gameObject;
                    if (logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到推理面板");
                }
            }
            
            // 查找暂停菜单面板
            if (pauseMenuPanel == null)
            {
                Transform pausePanel = root.transform.Find("Canvas/PauseMenuPanel");
                if (pausePanel != null)
                {
                    pauseMenuPanel = pausePanel.gameObject;
                    if (logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到暂停菜单面板");
                }
            }
            
            // 查找背包按钮
            if (inventoryButton == null)
            {
                Transform invButton = root.transform.Find("Canvas/BottomPanel/InventoryButton");
                if (invButton != null)
                {
                    inventoryButton = invButton.GetComponent<Button>();
                    if (inventoryButton != null && logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到背包按钮");
                }
            }
            
            // 查找推理按钮
            if (reasoningButton == null)
            {
                Transform reasonButton = root.transform.Find("Canvas/BottomPanel/ReasoningButton");
                if (reasonButton != null)
                {
                    reasoningButton = reasonButton.GetComponent<Button>();
                    if (reasoningButton != null && logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到推理按钮");
                }
            }
            
            // 查找设置按钮
            if (settingsButton == null)
            {
                Transform setButton = root.transform.Find("Canvas/BottomPanel/SettingsButton");
                if (setButton != null)
                {
                    settingsButton = setButton.GetComponent<Button>();
                    if (settingsButton != null && logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到设置按钮");
                }
            }
            
            // 查找提示UI
            if (inventoryHint == null)
            {
                Transform invHint = root.transform.Find("Canvas/BottomPanel/InventoryButton/Hint");
                if (invHint != null)
                {
                    inventoryHint = invHint.gameObject;
                    if (logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到背包提示");
                }
            }
            
            if (reasoningHint == null)
            {
                Transform reasonHint = root.transform.Find("Canvas/BottomPanel/ReasoningButton/Hint");
                if (reasonHint != null)
                {
                    reasoningHint = reasonHint.gameObject;
                    if (logUIOperations)
                        Debug.Log($"[UIManager] 在场景 {scene.name} 中找到推理提示");
                }
            }
        }
    }

    // 在所有已加载场景中查找UI元素
    private void FindUIElementsInAllScenes()
    {
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            FindUIElementsInScene(SceneManager.GetSceneAt(i));
        }
    }

    // 确保必要的UI引用存在
    private void EnsureUIReferences()
    {
        // 如果没有找到背包面板，记录警告
        if (inventoryPanel == null)
        {
            Debug.LogWarning("[UIManager] 未找到背包面板，部分功能可能无法正常工作");
        }
        
        // 如果没有找到推理面板，记录警告
        if (reasoningPanel == null)
        {
            Debug.LogWarning("[UIManager] 未找到推理面板，部分功能可能无法正常工作");
        }
        
        // 如果没有找到暂停菜单面板，记录警告
        if (pauseMenuPanel == null)
        {
            Debug.LogWarning("[UIManager] 未找到暂停菜单面板，部分功能可能无法正常工作");
        }
        
        // 重新设置按钮点击事件
        SetupButtons();
        
        // 更新提示显示
        UpdateHints();
    }

    // 检查场景是否已加载
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    
    #region 法律查看器管理
    
    /// <summary>
    /// 打开法律查看器
    /// </summary>
    public void OpenLawViewer()
    {
        var lawViewerManager = LawSystem.LawViewerManager.Instance;
        if (lawViewerManager == null)
        {
            Debug.LogError("[UIManager] 法律查看器管理器未找到！请确保场景中有LawViewerManager组件");
            return;
        }
        
        // 关闭其他面板
        CloseAllPanels();
        
        // 打开法律查看器
        lawViewerManager.OpenLawViewer();
        isLawViewerOpen = true;
        
        if (logUIOperations)
            Debug.Log("[UIManager] 法律查看器已打开");
    }
    
    /// <summary>
    /// 关闭法律查看器
    /// </summary>
    public void CloseLawViewer()
    {
        var lawViewerManager = LawSystem.LawViewerManager.Instance;
        if (lawViewerManager == null) return;
        
        lawViewerManager.CloseLawViewer();
        isLawViewerOpen = false;
        
        if (logUIOperations)
            Debug.Log("[UIManager] 法律查看器已关闭");
    }
    
    /// <summary>
    /// 检查法律查看器是否打开
    /// </summary>
    public bool IsLawViewerOpen()
    {
        var lawViewerManager = LawSystem.LawViewerManager.Instance;
        return isLawViewerOpen && lawViewerManager != null && lawViewerManager.IsLawViewerOpen();
    }
    
    /// <summary>
    /// 获取法律查看器管理器
    /// </summary>
    public LawSystem.LawViewerManager GetLawViewerManager()
    {
        return LawSystem.LawViewerManager.Instance;
    }
    
    #endregion
    
    #region 判刑系统相关方法
    
    /// <summary>
    /// 打开判刑界面
    /// </summary>
    public void OpenSentencing(SuspectSO suspect = null)
    {
        var sentencingManager = LawSystem.SentencingManager.Instance;
        if (sentencingManager == null)
        {
            Debug.LogError("[UIManager] 判刑管理器未找到！请确保场景中有SentencingManager组件");
            return;
        }
        
        // 关闭其他面板
        CloseAllPanels();
        
        // 打开判刑界面
        sentencingManager.OpenSentencing(suspect);
        isSentencingOpen = true;
        
        if (logUIOperations)
            Debug.Log("[UIManager] 判刑界面已打开");
    }
    
    /// <summary>
    /// 关闭判刑界面
    /// </summary>
    public void CloseSentencing()
    {
        var sentencingManager = LawSystem.SentencingManager.Instance;
        if (sentencingManager == null) return;
        
        sentencingManager.CloseSentencing();
        isSentencingOpen = false;
        
        if (logUIOperations)
            Debug.Log("[UIManager] 判刑界面已关闭");
    }
    
    /// <summary>
    /// 检查判刑界面是否打开
    /// </summary>
    public bool IsSentencingOpen()
    {
        var sentencingManager = LawSystem.SentencingManager.Instance;
        return isSentencingOpen && sentencingManager != null && sentencingManager.IsSentencingOpen();
    }
    
    /// <summary>
    /// 获取判刑管理器实例
    /// </summary>
    public LawSystem.SentencingManager GetSentencingManager()
    {
        return LawSystem.SentencingManager.Instance;
    }
    
    #endregion

    
    private void OnDestroy()
    {
        // 取消场景加载事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
