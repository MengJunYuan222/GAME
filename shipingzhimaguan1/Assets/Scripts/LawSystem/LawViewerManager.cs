using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace LawSystem
{
    /// <summary>
    /// 法律查看器管理器 - 跨场景持久化
    /// 管理法律界面的打开、关闭和内容显示
    /// </summary>
    public class LawViewerManager : MonoBehaviour
    {
        // 单例模式 - 跨场景持久化
        public static LawViewerManager Instance { get; private set; }
        [Header("法律界面引用")]
        [SerializeField] private GameObject lawMainPanel;           // 主法律界面（五个按钮）
        [SerializeField] private GameObject lawDetailPanel;        // 详细法律界面
        [SerializeField] private Button closeLawMainButton;        // 关闭主界面按钮
        [SerializeField] private Button closeLawDetailButton;      // 关闭详细界面按钮
        [SerializeField] private Button backToMainButton;          // 返回主界面按钮
        
        [Header("法律按钮引用")]
        [SerializeField] private Button[] lawCategoryButtons = new Button[5];  // 五个法律类别按钮
        
        [Header("详细界面UI")]
        [SerializeField] private TextMeshProUGUI lawTitleText;     // 法律标题
        [SerializeField] private Transform subItemContainer;       // 子条目容器
        [SerializeField] private GameObject subItemPrefab;         // 子条目预制体
        
        [Header("法律数据")]
        [SerializeField] private LawDataSO[] lawDataArray = new LawDataSO[5];  // 五个法律数据
        
        [Header("音效设置")]
        [SerializeField] private bool enableSounds = true;
        
        [Header("跨场景设置")]
        [SerializeField] private bool dontDestroyOnLoad = true;  // 是否跨场景保持
        
        // 状态管理
        private bool isLawMainOpen = false;
        private bool isLawDetailOpen = false;
        private int currentLawIndex = -1;
        
        // 系统引用
        private AudioManager audioManager;
        
        private void Awake()
        {
            // 单例模式实现
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // 跨场景持久化
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
                
                // 监听场景加载事件
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            
            // 初始化界面状态
            if (lawMainPanel != null)
                lawMainPanel.SetActive(false);
                
            if (lawDetailPanel != null)
                lawDetailPanel.SetActive(false);
        }
        
        private void Start()
        {
            // 获取音频管理器引用
            audioManager = AudioManager.Instance;
            
            SetupButtons();
            UpdateButtonTexts();
        }
        
        /// <summary>
        /// 场景加载事件处理
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 场景切换后重新获取音频管理器引用
            if (audioManager == null)
            {
                audioManager = AudioManager.Instance;
            }
            
            Debug.Log($"[LawViewerManager] 场景 {scene.name} 已加载，法律查看器已准备就绪");
        }
        
        /// <summary>
        /// 设置按钮事件
        /// </summary>
        private void SetupButtons()
        {
            // 设置关闭按钮
            if (closeLawMainButton != null)
                closeLawMainButton.onClick.AddListener(CloseLawViewer);
                
            if (closeLawDetailButton != null)
                closeLawDetailButton.onClick.AddListener(CloseLawViewer);
                
            if (backToMainButton != null)
                backToMainButton.onClick.AddListener(BackToMainPanel);
            
            // 设置法律类别按钮
            for (int i = 0; i < lawCategoryButtons.Length; i++)
            {
                if (lawCategoryButtons[i] != null)
                {
                    int index = i; // 捕获循环变量
                    lawCategoryButtons[i].onClick.AddListener(() => OpenLawDetail(index));
                }
            }
        }
        
        /// <summary>
        /// 更新按钮文本
        /// </summary>
        private void UpdateButtonTexts()
        {
            for (int i = 0; i < lawCategoryButtons.Length && i < lawDataArray.Length; i++)
            {
                if (lawCategoryButtons[i] != null && lawDataArray[i] != null)
                {
                    var buttonText = lawCategoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = lawDataArray[i].GetDisplayName();
                    }
                }
            }
        }
        
        /// <summary>
        /// 打开法律查看器主界面
        /// </summary>
        public void OpenLawViewer()
        {
            if (lawMainPanel == null)
            {
                Debug.LogError("[LawViewerManager] 法律主界面未设置！");
                return;
            }
            
            // 通知UIManager关闭其他面板
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                uiManager.CloseAllPanels();
            }
            
            // 打开主界面
            lawMainPanel.SetActive(true);
            isLawMainOpen = true;
            
            // 确保详细界面关闭
            if (lawDetailPanel != null)
            {
                lawDetailPanel.SetActive(false);
                isLawDetailOpen = false;
            }
            
            // 播放打开音效
            PlayOpenSound();
            
            Debug.Log("[LawViewerManager] 法律查看器已打开");
        }
        
        /// <summary>
        /// 打开法律详细界面
        /// </summary>
        /// <param name="lawIndex">法律索引（0-4）</param>
        public void OpenLawDetail(int lawIndex)
        {
            if (lawIndex < 0 || lawIndex >= lawDataArray.Length)
            {
                Debug.LogError($"[LawViewerManager] 无效的法律索引: {lawIndex}");
                return;
            }
            
            if (lawDataArray[lawIndex] == null)
            {
                Debug.LogError($"[LawViewerManager] 法律数据 {lawIndex} 未设置！");
                return;
            }
            
            if (lawDetailPanel == null)
            {
                Debug.LogError("[LawViewerManager] 法律详细界面未设置！");
                return;
            }
            
            currentLawIndex = lawIndex;
            
            // 隐藏主界面，显示详细界面
            if (lawMainPanel != null)
                lawMainPanel.SetActive(false);
                
            lawDetailPanel.SetActive(true);
            isLawMainOpen = false;
            isLawDetailOpen = true;
            
            // 更新界面内容
            UpdateLawDetailContent(lawDataArray[lawIndex]);
            
            // 播放点击音效
            PlayClickSound();
            
            Debug.Log($"[LawViewerManager] 打开法律详细界面: {lawDataArray[lawIndex].lawTitle}");
        }
        
        /// <summary>
        /// 更新法律详细内容
        /// </summary>
        /// <param name="lawData">法律数据</param>
        private void UpdateLawDetailContent(LawDataSO lawData)
        {
            // 设置标题
            if (lawTitleText != null)
            {
                lawTitleText.text = lawData.lawTitle;
            }
            
            // 清除之前的子条目
            ClearSubItems();
            
            // 动态生成子条目UI
            if (lawData.subItems != null && subItemContainer != null && subItemPrefab != null)
            {
                foreach (var subItem in lawData.subItems)
                {
                    if (subItem != null)
                    {
                        CreateSubItemUI(subItem);
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除所有子条目UI
        /// </summary>
        private void ClearSubItems()
        {
            if (subItemContainer == null) return;
            
            // 删除所有子物体
            for (int i = subItemContainer.childCount - 1; i >= 0; i--)
            {
                var child = subItemContainer.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 创建单个子条目UI
        /// </summary>
        /// <param name="subItem">子条目数据</param>
        private void CreateSubItemUI(LawSubItem subItem)
        {
            if (subItemPrefab == null || subItemContainer == null) return;
            
            // 实例化预制体
            var subItemObj = Instantiate(subItemPrefab, subItemContainer);
            
            // 尝试使用LawSubItemUI组件
            var subItemUI = subItemObj.GetComponent<LawSubItemUI>();
            if (subItemUI != null)
            {
                subItemUI.SetSubItemData(subItem);
            }
            else
            {
                // 回退到手动查找组件
                var titleText = subItemObj.transform.Find("SubTitle")?.GetComponent<TextMeshProUGUI>();
                var contentText = subItemObj.transform.Find("SubContent")?.GetComponent<TextMeshProUGUI>();
                
                if (titleText != null)
                {
                    titleText.text = subItem.subTitle;
                }
                
                if (contentText != null)
                {
                    contentText.text = subItem.subContent;
                }
            }
        }
        
        /// <summary>
        /// 返回主界面
        /// </summary>
        public void BackToMainPanel()
        {
            if (lawMainPanel == null || lawDetailPanel == null)
                return;
                
            // 显示主界面，隐藏详细界面
            lawMainPanel.SetActive(true);
            lawDetailPanel.SetActive(false);
            
            isLawMainOpen = true;
            isLawDetailOpen = false;
            currentLawIndex = -1;
            
            // 播放返回音效
            PlayBackSound();
            
            Debug.Log("[LawViewerManager] 返回法律主界面");
        }
        
        /// <summary>
        /// 关闭法律查看器
        /// </summary>
        public void CloseLawViewer()
        {
            // 关闭所有法律界面
            if (lawMainPanel != null)
                lawMainPanel.SetActive(false);
                
            if (lawDetailPanel != null)
                lawDetailPanel.SetActive(false);
            
            isLawMainOpen = false;
            isLawDetailOpen = false;
            currentLawIndex = -1;
            
            // 播放关闭音效
            PlayCloseSound();
            
            Debug.Log("[LawViewerManager] 法律查看器已关闭");
        }
        
        /// <summary>
        /// 检查法律查看器是否打开
        /// </summary>
        public bool IsLawViewerOpen()
        {
            return isLawMainOpen || isLawDetailOpen;
        }
        
        /// <summary>
        /// 获取当前查看的法律索引
        /// </summary>
        public int GetCurrentLawIndex()
        {
            return currentLawIndex;
        }
        
        #region 音效播放
        
        private void PlayOpenSound()
        {
            if (!enableSounds || audioManager == null) return;
            // audioManager.PlayUISound("LawOpen");
        }
        
        private void PlayCloseSound()
        {
            if (!enableSounds || audioManager == null) return;
            // audioManager.PlayUISound("LawClose");
        }
        
        private void PlayClickSound()
        {
            if (!enableSounds || audioManager == null) return;
            // audioManager.PlayUISound("LawClick");
        }
        
        private void PlayBackSound()
        {
            if (!enableSounds || audioManager == null) return;
            // audioManager.PlayUISound("LawBack");
        }
        
        #endregion
        
        #region 输入处理
        
        private void Update()
        {
            // ESC键关闭法律查看器
            if (Input.GetKeyDown(KeyCode.Escape) && IsLawViewerOpen())
            {
                if (isLawDetailOpen)
                {
                    BackToMainPanel();
                }
                else if (isLawMainOpen)
                {
                    CloseLawViewer();
                }
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // 取消场景加载事件监听
            if (dontDestroyOnLoad)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
    }
}
