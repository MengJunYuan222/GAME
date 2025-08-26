using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PauseMenuController : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button settingsButton;   // 系统设置按钮
    [SerializeField] private Button mainMenuButton;   // 返回主页按钮
    [SerializeField] private Button exitButton;       // 退出游戏按钮

    [Header("系统引用")]
    // 不再使用SerializeField，改为动态查找
    private SettingsManager settingsManager; // 设置管理器引用
    
    [Header("保存提示")]
    [SerializeField] private GameObject saveConfirmationPanel;
    [SerializeField] private float confirmationDisplayTime = 2.0f;
    [SerializeField] private SaveConfirmationPanel saveConfirmationController;
    
    // GameManager引用
    private GameManager gameManager;
    
    // UIManager引用
    private UIManager uiManager;
    
    // 公开属性，供GameManager检查暂停状态
    public bool IsPaused { get; private set; }
    
    private void Awake()
    {
        // 先不获取GameManager引用，等到Start中获取
        
        // 使用ManagerReferenceSystem获取引用，避免交叉场景引用问题
        
        // 设置按钮事件
        SetupButtonEvents();
        
        // 隐藏保存确认面板
        if (saveConfirmationPanel != null)
        {
            saveConfirmationPanel.SetActive(false);
        }
        
        // 获取SaveConfirmationPanel控制器
        if (saveConfirmationController == null && saveConfirmationPanel != null)
        {
            saveConfirmationController = saveConfirmationPanel.GetComponent<SaveConfirmationPanel>();
        }
    }
    
    private void Start()
    {
        // 使用ManagerReferenceSystem获取所有管理器引用
        StartCoroutine(InitializeManagerReferences());
    }
    
    private System.Collections.IEnumerator InitializeManagerReferences()
    {
        // 等待ManagerReferenceSystem初始化
        while (ManagerReferenceSystem.Instance == null)
        {
            yield return null;
        }
        
        // 等待几帧确保所有Manager都已初始化
        for (int i = 0; i < 3; i++)
        {
            yield return null;
        }
        
        // 获取各种Manager引用
        gameManager = ManagerReferenceSystem.Instance.GetGameManager();
        uiManager = ManagerReferenceSystem.Instance.GetUIManager();
        settingsManager = ManagerReferenceSystem.Instance.GetSettingsManager();
        
        // 检查引用是否成功获取
        if (gameManager == null)
            Debug.LogWarning("[PauseMenuController] 无法找到GameManager！");
        if (uiManager == null)
            Debug.LogWarning("[PauseMenuController] 无法找到UIManager！");
        if (settingsManager == null)
            Debug.LogWarning("[PauseMenuController] 无法找到SettingsManager！");
        
        Debug.Log("[PauseMenuController] 管理器引用初始化完成");
    }
    

    
    private void SetupButtonEvents()
    {
        // 系统设置按钮
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }
        
        // 返回主页按钮
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        // 退出游戏按钮
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }
    }
    
    private void Update()
    {
        // 移除ESC键处理，现在由UIManager处理
    }
    
    // 打开暂停菜单
    public void OpenPauseMenu()
    {
        if (!IsPaused)
        {
            IsPaused = true;
            
            // 使用UIManager显示暂停菜单面板
            if (uiManager != null && !uiManager.IsPauseMenuOpen())
            {
                uiManager.TogglePauseMenu();
        }
        
            // 暂停游戏
            Time.timeScale = 0f;
        
        // 如果GameManager存在，同步其状态
            if (gameManager != null && !gameManager.IsPaused)
            {
                gameManager.TogglePause();
            }
        }
    }
    
    // 关闭暂停菜单
    public void ClosePauseMenu()
    {
        if (IsPaused)
    {
            IsPaused = false;
            
            // 使用UIManager隐藏暂停菜单面板
            if (uiManager != null && uiManager.IsPauseMenuOpen())
        {
                uiManager.TogglePauseMenu();
    }
    
            // 恢复游戏
            Time.timeScale = 1f;
            
            // 如果GameManager存在，同步其状态
            if (gameManager != null && gameManager.IsPaused)
            {
                gameManager.TogglePause();
            }
        }
    }
    
    // 设置暂停状态（供GameManager调用）
    public void SetPauseState(bool paused)
    {
        if (paused != IsPaused)
        {
            if (paused)
            {
                OpenPauseMenu();
            }
            else
            {
                ClosePauseMenu();
            }
        }
    }
    
    // 打开系统设置面板
    public void OpenSettings()
    {
        // 打开设置面板
        if (settingsManager != null)
        {
            settingsManager.gameObject.SetActive(true);
            settingsManager.OpenSettingsPanel();
        }
        else
        {
            Debug.LogWarning("无法打开设置面板：SettingsManager不可用");
        }
    }
    
    // 返回主页
    public void ReturnToMainMenu()
    {
        if (gameManager != null)
        {
            // 先保存游戏，然后返回主菜单
            gameManager.SaveGame();
            
            // 显示提示信息，然后返回主菜单
            ShowSaveConfirmation("返回主页...", () => {
                gameManager.ReturnToMainMenu();
            });
        }
        else
        {
            // 如果GameManager不可用，直接加载主菜单场景
            Time.timeScale = 1f; // 确保时间恢复正常
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
    
    // 退出游戏
    public void ExitGame()
    {
        if (gameManager != null)
        {
            // 先保存游戏，然后退出
            gameManager.SaveGame();
            ShowSaveConfirmation("正在退出游戏...", () => {
                Application.Quit();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            });
        }
        else
        {
            // 直接退出
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
    
    // 显示保存确认提示
    private void ShowSaveConfirmation(string message = "游戏已保存", System.Action onComplete = null)
    {
        if (saveConfirmationPanel != null)
        {
            // 如果有SaveConfirmationPanel控制器，使用其功能
            if (saveConfirmationController != null)
            {
                saveConfirmationController.SetMessage(message);
                saveConfirmationPanel.SetActive(true);
                
                // 延迟执行完成回调
                StartCoroutine(DelayedAction(confirmationDisplayTime, () => {
                    onComplete?.Invoke();
                }));
            }
            else
            {
                // 否则使用普通的激活/延迟隐藏方式
                saveConfirmationPanel.SetActive(true);
                StartCoroutine(HideSaveConfirmation(onComplete));
            }
        }
        else
        {
            // 如果没有保存确认面板，直接执行回调
            onComplete?.Invoke();
        }
    }

    private IEnumerator HideSaveConfirmation(System.Action onComplete = null)
    {
        // 使用RealTime确保在暂停状态下也正常计时
        yield return new WaitForSecondsRealtime(confirmationDisplayTime);
        
        if (saveConfirmationPanel != null)
        {
            saveConfirmationPanel.SetActive(false);
        }
        
        // 执行完成回调
        onComplete?.Invoke();
    }
    
    // 延迟执行操作
    private IEnumerator DelayedAction(float delay, System.Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }
} 