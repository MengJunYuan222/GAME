using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    
    [Header("菜单设置")]
    [SerializeField] private float buttonFadeInTime = 1.0f;
    [SerializeField] private float buttonAnimationDelay = 0.2f;
    
    [Header("视频背景")]
    [SerializeField] private VideoPlayer backgroundVideo;
    [SerializeField] private string gameSceneName = "GameScene"; // 游戏主场景名称
    
    private CanvasGroup[] buttonCanvasGroups;
    private SaveLoadManager saveLoadManager;
    
    private void Awake()
    {
        // 获取SaveLoadManager
        saveLoadManager = GetComponent<SaveLoadManager>();
        if (saveLoadManager == null)
        {
            saveLoadManager = gameObject.AddComponent<SaveLoadManager>();
        }
        
        // 设置按钮Canvas组
        SetupButtonCanvasGroups();
        
        // 初始隐藏加载屏幕
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        
        // 设置按钮事件
        SetupButtonEvents();
        
        // 检查是否有存档
        CheckForSaveGame();
    }
    
    private void Start()
    {
        // 开始播放背景视频
        if (backgroundVideo != null)
        {
            backgroundVideo.Play();
            backgroundVideo.isLooping = true;
        }
        
        // 动画显示按钮
        StartCoroutine(AnimateButtonsIn());
    }
    
    // 设置按钮Canvas组用于动画
    private void SetupButtonCanvasGroups()
    {
        // 获取所有按钮的CanvasGroup组件，如果没有则添加
        buttonCanvasGroups = new CanvasGroup[3];
        
        Button[] buttons = { startGameButton, continueGameButton, exitButton };
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttonCanvasGroups[i] = buttons[i].GetComponent<CanvasGroup>();
                if (buttonCanvasGroups[i] == null)
                {
                    buttonCanvasGroups[i] = buttons[i].gameObject.AddComponent<CanvasGroup>();
                }
                // 初始设置为透明
                buttonCanvasGroups[i].alpha = 0;
            }
        }
    }
    
    // 设置按钮点击事件
    private void SetupButtonEvents()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartNewGame);
        }
        
        if (continueGameButton != null)
        {
            continueGameButton.onClick.AddListener(ContinueGame);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
    }
    
    // 检查是否有存档
    private void CheckForSaveGame()
    {
        bool hasSaveData = saveLoadManager.HasSaveData();
        
        // 如果没有存档，禁用"继续游戏"按钮
        if (continueGameButton != null)
        {
            continueGameButton.interactable = hasSaveData;
        }
    }
    
    // 按钮淡入动画
    private IEnumerator AnimateButtonsIn()
    {
        for (int i = 0; i < buttonCanvasGroups.Length; i++)
        {
            if (buttonCanvasGroups[i] != null)
            {
                // 等待延迟
                yield return new WaitForSeconds(buttonAnimationDelay);
                
                // 淡入动画
                float time = 0;
                while (time < buttonFadeInTime)
                {
                    time += Time.deltaTime;
                    buttonCanvasGroups[i].alpha = Mathf.Lerp(0, 1, time / buttonFadeInTime);
                    yield return null;
                }
                
                buttonCanvasGroups[i].alpha = 1;
            }
        }
    }
    
    // 开始新游戏
    public void StartNewGame()
    {
        StartCoroutine(LoadGameScene(false));
    }
    
    // 继续游戏
    public void ContinueGame()
    {
        StartCoroutine(LoadGameScene(true));
    }
    
    // 异步加载游戏场景
    private IEnumerator LoadGameScene(bool loadSaveData)
    {
        // 显示加载屏幕
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }
        
        // 开始加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        asyncLoad.allowSceneActivation = false;
        
        // 更新加载进度条
        while (asyncLoad.progress < 0.9f)
        {
            if (loadingBar != null)
            {
                loadingBar.value = asyncLoad.progress;
            }
            yield return null;
        }
        
        // 设置进度条为满
        if (loadingBar != null)
        {
            loadingBar.value = 1.0f;
        }
        
        // 设置是否需要加载存档的标志
        PlayerPrefs.SetInt("LoadSaveData", loadSaveData ? 1 : 0);
        
        // 激活场景
        asyncLoad.allowSceneActivation = true;
    }
    
    // 退出游戏
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
} 