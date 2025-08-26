using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("面板引用")]
    [SerializeField] private GameObject settingsPanel;

    [Header("系统设置控件")]
    [SerializeField] private Slider volumeSlider;        // 音量滑块
    [SerializeField] private TMP_Dropdown resolutionDropdown; // 分辨率下拉菜单
    [SerializeField] private Toggle fullscreenToggle;    // 全屏切换

    [Header("按钮引用")]
    [SerializeField] private Button closeButton;         // 关闭按钮
    [SerializeField] private Button applyButton;         // 应用按钮
    [SerializeField] private Button resetButton;         // 重置按钮

    [Header("音频设置")]
    [SerializeField] private AudioMixer audioMixer;      // 音频混合器
    [SerializeField] private string volumeParam = "MasterVolume"; // 音量参数名

    // AudioManager引用
    private AudioManager audioManager;

    // 默认设置值
    private float defaultVolume = 0.75f;                 // 默认音量
    private bool defaultFullscreen = true;               // 默认全屏

    // 保存设置的PlayerPrefs键
    private const string VOLUME_KEY = "MasterVolume";    // 音量键
    private const string RESOLUTION_INDEX_KEY = "ResolutionIndex"; // 分辨率索引键
    private const string FULLSCREEN_KEY = "Fullscreen";  // 全屏键

    // 分辨率选项
    private Resolution[] resolutions;

    // 是否已初始化
    private bool initialized = false;

    private void Awake()
    {
        // 初始化UI引用
        InitializeUIReferences();
        
        // 默认隐藏设置面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        // 获取AudioManager引用
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning("无法找到AudioManager实例，音量设置可能无法正确应用到所有音频源。");
        }
    }

    private void Start()
    {
        // 初始化设置
        InitializeSettings();
        
        // 设置已初始化
        initialized = true;
    }
    
    // 初始化UI引用
    private void InitializeUIReferences()
    {
        // 设置按钮点击事件
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);
            
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);
            
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefaults);
            
        // 设置控件事件
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);
            
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
            
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    // 初始化设置
    private void InitializeSettings()
    {
        // 初始化分辨率选项
        InitializeResolutions();
        
        // 加载保存的设置
        LoadSettings();
    }
    
    // 初始化分辨率选项
    private void InitializeResolutions()
    {
        // 获取可用分辨率
        resolutions = Screen.resolutions;
        
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            
            // 创建分辨率选项
            var options = new System.Collections.Generic.List<string>();
            int currentResolutionIndex = 0;
            
            for (int i = 0; i < resolutions.Length; i++)
            {
                // 使用refreshRateRatio代替refreshRate，并转换为float
                float refreshRate = (float)resolutions[i].refreshRateRatio.value;
                string option = $"{resolutions[i].width} x {resolutions[i].height} @{refreshRate}Hz";
                options.Add(option);
                
                // 检查是否为当前分辨率
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height &&
                    Mathf.Approximately((float)resolutions[i].refreshRateRatio.value, (float)Screen.currentResolution.refreshRateRatio.value))
                {
                    currentResolutionIndex = i;
                }
            }
            
            // 添加选项到下拉菜单
            resolutionDropdown.AddOptions(options);
            
            // 设置当前分辨率
            int savedResIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, currentResolutionIndex);
            if (savedResIndex >= 0 && savedResIndex < resolutions.Length)
            {
                resolutionDropdown.value = savedResIndex;
            }
            else
            {
                resolutionDropdown.value = currentResolutionIndex;
            }
            
            resolutionDropdown.RefreshShownValue();
        }
    }

    // 加载保存的设置
    private void LoadSettings()
    {
        // 加载音量设置
        if (audioMixer != null)
        {
            // 音量
            float volume = PlayerPrefs.GetFloat(VOLUME_KEY, defaultVolume);
            if (volumeSlider != null)
                volumeSlider.value = volume;
            SetVolume(volume);
        }
        
        // 全屏设置
        bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, defaultFullscreen ? 1 : 0) == 1;
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = fullscreen;
        SetFullscreen(fullscreen);
    }

    // 关闭设置面板
    public void CloseSettings()
    {
        // 应用设置
        ApplySettings();
        
        // 关闭设置面板
        if (UIManager.Instance != null && UIManager.Instance.IsPauseMenuOpen())
        {
            UIManager.Instance.TogglePauseMenu();
        }
        else if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // 应用设置
    public void ApplySettings()
    {
        // 保存设置到PlayerPrefs
        SaveSettings();
        
        // 应用分辨率和全屏设置
        ApplyResolutionSettings();
    }

    // 保存设置
    private void SaveSettings()
    {
        // 保存音量设置
        PlayerPrefs.SetFloat(VOLUME_KEY, volumeSlider != null ? volumeSlider.value : defaultVolume);
        
        // 保存分辨率和全屏设置
        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, resolutionDropdown != null ? resolutionDropdown.value : 0);
        PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreenToggle != null && fullscreenToggle.isOn ? 1 : 0);
        
        // 保存设置
        PlayerPrefs.Save();
    }

    // 应用分辨率设置
    private void ApplyResolutionSettings()
    {
        // 应用分辨率设置
        if (resolutionDropdown != null && resolutions != null && resolutionDropdown.value < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionDropdown.value];
            bool isFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : defaultFullscreen;
            
            // 使用新的SetResolution方法，传入RefreshRate而不是int
            Screen.SetResolution(resolution.width, resolution.height, isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, resolution.refreshRateRatio);
        }
    }

    // 重置为默认设置
    public void ResetToDefaults()
    {
        // 重置音量设置
        if (volumeSlider != null)
            volumeSlider.value = defaultVolume;
        SetVolume(defaultVolume);
        
        // 重置全屏设置
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = defaultFullscreen;
        SetFullscreen(defaultFullscreen);
        
        // 重置分辨率设置（使用当前屏幕分辨率）
        if (resolutionDropdown != null)
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
        }
    }

    // 刷新设置显示
    public void RefreshSettings()
    {
        if (!initialized)
        {
            // 如果尚未初始化，进行初始化
            InitializeSettings();
        }
        else
        {
            // 重新加载设置
            LoadSettings();
        }
    }

    // 设置音量
    public void SetVolume(float volume)
    {
        // 设置AudioMixer音量
        if (audioMixer != null)
        {
            // 将线性音量值转换为对数分贝值（-80dB至0dB）
            float dbVolume = volume > 0.001f ? Mathf.Log10(volume) * 20 : -80f;
            try
            {
                audioMixer.SetFloat(volumeParam, dbVolume);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"设置音量失败：{e.Message}。请检查AudioMixer中是否存在名为'{volumeParam}'的暴露参数。");
            }
        }
        
        // 同时更新AudioManager中的主音量
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(volume);
        }
    }

    // 设置分辨率
    public void SetResolution(int resolutionIndex)
    {
        // 在应用设置时处理
    }

    // 设置全屏模式
    public void SetFullscreen(bool isFullscreen)
    {
        // 在应用设置时处理
    }

    // 打开设置面板
    public void OpenSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            RefreshSettings();
        }
        else
        {
            Debug.LogWarning("无法打开设置面板：settingsPanel为空");
        }
    }
} 
