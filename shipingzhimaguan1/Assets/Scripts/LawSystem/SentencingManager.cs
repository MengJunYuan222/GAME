using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace LawSystem
{
    /// <summary>
    /// 判刑系统管理器 - 跨场景持久化
    /// 管理判刑界面的显示和交互逻辑
    /// </summary>
    public class SentencingManager : MonoBehaviour
    {
        // 单例模式 - 跨场景持久化
        public static SentencingManager Instance { get; private set; }
        
        [Header("判刑界面引用")]
        [SerializeField] private GameObject sentencingPanel;          // 判刑界面面板
        [SerializeField] private Button closeSentencingButton;        // 关闭按钮
        
        [Header("嫌疑人信息UI")]
        [SerializeField] private Image suspectPortrait;               // 头像
        [SerializeField] private TextMeshProUGUI suspectNameText;     // 姓名
        [SerializeField] private TextMeshProUGUI suspectAgeText;      // 年龄
        [SerializeField] private TextMeshProUGUI suspectHometownText; // 籍贯
        [SerializeField] private TextMeshProUGUI suspectDescText;     // 简介
        
        [Header("罪名选择")]
        [SerializeField] private TMP_Dropdown crimeDropdown;          // 罪名下拉选择
        [SerializeField] private Button submitCrimeButton;            // 罪名选择按钮
        
        [Header("判决UI")]
        [SerializeField] private TMP_Dropdown punishmentDropdown;     // 刑罚下拉选择
        [SerializeField] private TMP_Dropdown fineDropdown;           // 罚银下拉选择
        [SerializeField] private TMP_Dropdown prisonDropdown;         // 徒刑下拉选择
        [SerializeField] private Button finalJudgmentButton;          // 最终判决按钮
        
        [Header("数据")]
        [SerializeField] private SuspectSO currentSuspect;            // 当前嫌疑人
        [SerializeField] private LawDataSO[] lawDataArray;            // 法律数据数组，从中提取罪名
        
        [Header("跨场景设置")]
        [SerializeField] private bool dontDestroyOnLoad = true;       // 是否跨场景保持
        
        // 状态管理
        private bool isSentencingPanelOpen = false;
        private string selectedCrimeName = "";
        private string selectedPunishment = "杖刑";
        private int selectedFine = 10;
        private int selectedPrison = 1;
        
        // 所有可用罪名列表（从法律数据中提取）
        private System.Collections.Generic.List<string> availableCrimeNames = new System.Collections.Generic.List<string>();
        
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
            if (sentencingPanel != null)
                sentencingPanel.SetActive(false);
        }
        
        private void Start()
        {
            // 获取音频管理器引用
            audioManager = AudioManager.Instance;
            
            SetupButtons();
            ExtractCrimeNamesFromLawData();
            InitializeDropdowns();
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
            
            Debug.Log($"[SentencingManager] 场景 {scene.name} 已加载，判刑系统已准备就绪");
        }
        
        /// <summary>
        /// 设置按钮事件
        /// </summary>
        private void SetupButtons()
        {
            if (closeSentencingButton != null)
                closeSentencingButton.onClick.AddListener(CloseSentencing);
                
            if (submitCrimeButton != null)
                submitCrimeButton.onClick.AddListener(OnCrimeSelected);
                
            if (finalJudgmentButton != null)
                finalJudgmentButton.onClick.AddListener(OnFinalJudgment);
        }
        
        /// <summary>
        /// 从法律数据中提取所有罪名
        /// </summary>
        private void ExtractCrimeNamesFromLawData()
        {
            availableCrimeNames.Clear();
            
            if (lawDataArray == null) return;
            
            foreach (var lawData in lawDataArray)
            {
                if (lawData == null || lawData.subItems == null) continue;
                
                foreach (var subItem in lawData.subItems)
                {
                    if (subItem != null && !string.IsNullOrEmpty(subItem.subTitle))
                    {
                        // 避免重复添加相同的罪名
                        if (!availableCrimeNames.Contains(subItem.subTitle))
                        {
                            availableCrimeNames.Add(subItem.subTitle);
                        }
                    }
                }
            }
            
            Debug.Log($"[SentencingManager] 从法律数据中提取到 {availableCrimeNames.Count} 个罪名");
        }
        
        /// <summary>
        /// 初始化下拉选择
        /// </summary>
        private void InitializeDropdowns()
        {
            InitializeCrimeDropdown();
            InitializePunishmentDropdowns();
        }
        
        /// <summary>
        /// 初始化罪名下拉选择
        /// </summary>
        private void InitializeCrimeDropdown()
        {
            if (crimeDropdown == null) return;
            
            crimeDropdown.ClearOptions();
            crimeDropdown.options.Add(new TMP_Dropdown.OptionData("请选择罪名"));
            
            foreach (var crimeName in availableCrimeNames)
            {
                crimeDropdown.options.Add(new TMP_Dropdown.OptionData(crimeName));
            }
            
            crimeDropdown.value = 0;
            crimeDropdown.RefreshShownValue();
        }
        
        /// <summary>
        /// 初始化刑罚相关下拉选择
        /// </summary>
        private void InitializePunishmentDropdowns()
        {
            // 初始化刑罚类型
            if (punishmentDropdown != null)
            {
                punishmentDropdown.ClearOptions();
                punishmentDropdown.options.Add(new TMP_Dropdown.OptionData("杖刑"));
                punishmentDropdown.options.Add(new TMP_Dropdown.OptionData("徒刑"));
                punishmentDropdown.options.Add(new TMP_Dropdown.OptionData("流刑"));
                punishmentDropdown.options.Add(new TMP_Dropdown.OptionData("死刑"));
                punishmentDropdown.value = 0;
                punishmentDropdown.RefreshShownValue();
            }
            
            // 初始化罚银选项
            if (fineDropdown != null)
            {
                fineDropdown.ClearOptions();
                fineDropdown.options.Add(new TMP_Dropdown.OptionData("10两"));
                fineDropdown.options.Add(new TMP_Dropdown.OptionData("20两"));
                fineDropdown.options.Add(new TMP_Dropdown.OptionData("50两"));
                fineDropdown.options.Add(new TMP_Dropdown.OptionData("100两"));
                fineDropdown.value = 0;
                fineDropdown.RefreshShownValue();
            }
            
            // 初始化徒刑选项
            if (prisonDropdown != null)
            {
                prisonDropdown.ClearOptions();
                prisonDropdown.options.Add(new TMP_Dropdown.OptionData("1年"));
                prisonDropdown.options.Add(new TMP_Dropdown.OptionData("2年"));
                prisonDropdown.options.Add(new TMP_Dropdown.OptionData("3年"));
                prisonDropdown.options.Add(new TMP_Dropdown.OptionData("5年"));
                prisonDropdown.value = 0;
                prisonDropdown.RefreshShownValue();
            }
        }
        
        /// <summary>
        /// 打开判刑界面
        /// </summary>
        public void OpenSentencing(SuspectSO suspect = null)
        {
            if (sentencingPanel == null)
            {
                Debug.LogError("[SentencingManager] 判刑界面未设置！");
                return;
            }
            
            // 通知UIManager关闭其他面板
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                uiManager.CloseAllPanels();
            }
            
            // 设置当前嫌疑人
            if (suspect != null)
            {
                currentSuspect = suspect;
            }
            
            // 更新嫌疑人信息显示
            UpdateSuspectInfo();
            
            // 重置判决状态
            ResetSentencingState();
            
            // 打开界面
            sentencingPanel.SetActive(true);
            isSentencingPanelOpen = true;
            
            Debug.Log("[SentencingManager] 判刑界面已打开");
        }
        
        /// <summary>
        /// 关闭判刑界面
        /// </summary>
        public void CloseSentencing()
        {
            if (sentencingPanel != null)
            {
                sentencingPanel.SetActive(false);
            }
            
            isSentencingPanelOpen = false;
            
            Debug.Log("[SentencingManager] 判刑界面已关闭");
        }
        
        /// <summary>
        /// 更新嫌疑人信息显示
        /// </summary>
        private void UpdateSuspectInfo()
        {
            if (currentSuspect == null) return;
            
            if (suspectPortrait != null)
            {
                suspectPortrait.sprite = currentSuspect.suspectPortrait;
            }
            
            if (suspectNameText != null)
            {
                suspectNameText.text = currentSuspect.suspectName;
            }
            
            if (suspectAgeText != null)
            {
                suspectAgeText.text = currentSuspect.GetAgeText();
            }
            
            if (suspectHometownText != null)
            {
                suspectHometownText.text = currentSuspect.hometown;
            }
            
            if (suspectDescText != null)
            {
                suspectDescText.text = currentSuspect.description;
            }
        }
        
        /// <summary>
        /// 重置判决状态
        /// </summary>
        private void ResetSentencingState()
        {
            selectedCrimeName = "";
            selectedPunishment = "杖刑";
            selectedFine = 10;
            selectedPrison = 1;
            
            // 重置下拉选择
            if (crimeDropdown != null)
            {
                crimeDropdown.value = 0;
            }
            
            if (punishmentDropdown != null)
            {
                punishmentDropdown.value = 0;
            }
            
            if (fineDropdown != null)
            {
                fineDropdown.value = 0;
            }
            
            if (prisonDropdown != null)
            {
                prisonDropdown.value = 0;
            }
        }
        
        /// <summary>
        /// 罪名选择事件
        /// </summary>
        private void OnCrimeSelected()
        {
            if (crimeDropdown == null) return;
            
            int selectedIndex = crimeDropdown.value - 1; // 减1因为第一个是"请选择罪名"
            
            if (selectedIndex >= 0 && selectedIndex < availableCrimeNames.Count)
            {
                selectedCrimeName = availableCrimeNames[selectedIndex];
                
                Debug.Log($"[SentencingManager] 选择罪名: {selectedCrimeName}");
            }
            else
            {
                selectedCrimeName = "";
            }
        }
        

        
        /// <summary>
        /// 最终判决事件
        /// </summary>
        private void OnFinalJudgment()
        {
            if (string.IsNullOrEmpty(selectedCrimeName))
            {
                Debug.LogWarning("[SentencingManager] 请先选择罪名！");
                return;
            }
            
            // 获取当前选择的判决
            UpdateSelectedValues();
            
            // 执行判决
            ExecuteJudgment();
        }
        
        /// <summary>
        /// 更新选择的值
        /// </summary>
        private void UpdateSelectedValues()
        {
            // 更新刑罚类型
            if (punishmentDropdown != null)
            {
                var punishmentOptions = new string[] { "杖刑", "徒刑", "流刑", "死刑" };
                int punishmentIndex = punishmentDropdown.value;
                if (punishmentIndex >= 0 && punishmentIndex < punishmentOptions.Length)
                {
                    selectedPunishment = punishmentOptions[punishmentIndex];
                }
            }
            
            // 更新罚银
            if (fineDropdown != null)
            {
                var fineOptions = new int[] { 10, 20, 50, 100 };
                int fineIndex = fineDropdown.value;
                if (fineIndex >= 0 && fineIndex < fineOptions.Length)
                {
                    selectedFine = fineOptions[fineIndex];
                }
            }
            
            // 更新徒刑
            if (prisonDropdown != null)
            {
                var prisonOptions = new int[] { 1, 2, 3, 5 };
                int prisonIndex = prisonDropdown.value;
                if (prisonIndex >= 0 && prisonIndex < prisonOptions.Length)
                {
                    selectedPrison = prisonOptions[prisonIndex];
                }
            }
        }
        
        /// <summary>
        /// 执行判决
        /// </summary>
        private void ExecuteJudgment()
        {
            string judgmentText = $"判决结果：\n" +
                                $"嫌疑人：{currentSuspect?.suspectName ?? "未知"}\n" +
                                $"罪名：{selectedCrimeName}\n" +
                                $"刑罚：{selectedPunishment}\n" +
                                $"罚银：{selectedFine}两\n" +
                                $"徒刑：{selectedPrison}年";
            
            Debug.Log($"[SentencingManager] {judgmentText}");
            
            // 这里可以添加判决结果的处理逻辑
            // 例如：保存到游戏数据、触发后续剧情等
            
            // 关闭界面
            CloseSentencing();
        }
        
        /// <summary>
        /// 检查界面是否打开
        /// </summary>
        public bool IsSentencingOpen()
        {
            return isSentencingPanelOpen;
        }
        
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
