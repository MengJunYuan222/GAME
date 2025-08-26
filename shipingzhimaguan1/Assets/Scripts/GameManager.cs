using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("游戏管理")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private bool isPaused = false;
    
    [Header("UI引用")]
    [SerializeField] private PauseMenuController pauseMenuController;
    
    // 组件引用
    private SaveLoadManager saveLoadManager;
    
    // 游戏状态
    private SaveData currentGameData;
    private float gamePlayTime = 0f;
    
    // 公开暂停状态供其他脚本使用
    public bool IsPaused => isPaused;
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化存档管理器
            saveLoadManager = GetComponent<SaveLoadManager>();
            if (saveLoadManager == null)
            {
                saveLoadManager = gameObject.AddComponent<SaveLoadManager>();
            }
            
            // 订阅场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 检查是否需要加载存档
        CheckLoadSaveData();
        
        // 查找并初始化暂停菜单控制器
        if (pauseMenuController == null)
        {
            pauseMenuController = FindObjectOfType<PauseMenuController>();
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每次加载场景时检查是否需要加载存档
        if (scene.name != menuSceneName)
        {
            CheckLoadSaveData();
            
            // 重新查找暂停菜单控制器
            if (pauseMenuController == null)
            {
                pauseMenuController = FindObjectOfType<PauseMenuController>();
            }
        }
    }
    
    private void Update()
    {
        // 游戏未暂停时更新游戏时间
        if (!isPaused && currentGameData != null)
        {
            gamePlayTime += Time.deltaTime;
            currentGameData.playTime = gamePlayTime;
        }
    }
    
    // 检查是否需要加载存档
    private void CheckLoadSaveData()
    {
        // 通过PlayerPrefs检查是否需要加载存档
        bool shouldLoadSave = PlayerPrefs.GetInt("LoadSaveData", 0) == 1;
        
        if (shouldLoadSave)
        {
            // 加载存档
            LoadGame();
            // 重置标志
            PlayerPrefs.SetInt("LoadSaveData", 0);
        }
        else
        {
            // 创建新游戏
            NewGame();
        }
    }
    
    // 创建新游戏
    public void NewGame()
    {
        saveLoadManager.CreateNewGame();
        currentGameData = saveLoadManager.GetCurrentSaveData();
        gamePlayTime = 0f;
        
        Debug.Log("开始新游戏");
    }
    
    // 加载游戏
    public void LoadGame()
    {
        if (saveLoadManager.LoadGame())
        {
            currentGameData = saveLoadManager.GetCurrentSaveData();
            gamePlayTime = currentGameData.playTime;
            
            // 应用加载的游戏数据
            ApplyLoadedGameData();
            
            Debug.Log("加载游戏成功");
        }
        else
        {
            Debug.LogWarning("加载游戏失败，创建新游戏");
            NewGame();
        }
    }
    
    // 应用加载的游戏数据到场景
    private void ApplyLoadedGameData()
    {
        // 获取玩家并设置位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && currentGameData != null)
        {
            player.transform.position = currentGameData.playerPosition;
        }
        
        // 在这里应用其他游戏数据
        // 例如：任务进度、收集的物品等
    }
    
    // 保存游戏
    public void SaveGame()
    {
        if (currentGameData == null) return;
        
        // 更新玩家位置等信息
        UpdateGameDataBeforeSave();
        
        // 保存
        saveLoadManager.UpdateSaveData(currentGameData);
        saveLoadManager.SaveGame();
        
        Debug.Log("游戏已保存");
    }
    
    // 在保存前更新游戏数据
    private void UpdateGameDataBeforeSave()
    {
        // 获取并保存玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && currentGameData != null)
        {
            currentGameData.playerPosition = player.transform.position;
        }
        
        // 在这里更新其他需要保存的游戏数据
        // 例如：任务进度、收集的物品等
    }
    
    // 返回主菜单
    public void ReturnToMainMenu()
    {
        // 保存游戏
        SaveGame();
        
        // 恢复时间缩放
        Time.timeScale = 1f;
        isPaused = false;
        
        // 加载主菜单场景
        SceneManager.LoadScene(menuSceneName);
    }
    
    // 切换暂停状态
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        // 设置时间缩放
        Time.timeScale = isPaused ? 0f : 1f;
        
        // 如果有暂停菜单控制器，同步其状态
        if (pauseMenuController != null)
        {
            pauseMenuController.SetPauseState(isPaused);
        }
        
        Debug.Log(isPaused ? "游戏已暂停" : "游戏已恢复");
    }
    
    private void OnDestroy()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
} 