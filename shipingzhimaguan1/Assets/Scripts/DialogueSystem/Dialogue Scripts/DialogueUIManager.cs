using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;
using DialogueSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Text; // Added for StringBuilder

namespace DialogueSystem
{
    public enum UIAnimType
    {
        None,
        Qieman,
        Wait,
        Special
        // 你可以根据实际动画类型继续添加
    }

public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private Image speakerImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject[] optionButtons;

    [Header("出示功能")]
    [SerializeField] private GameObject presentItemPanel; // 出示功能面板
    [SerializeField] private Button showItemButton; // 出示按钮
    [SerializeField] private InventoryInput inventoryInput; // 背包输入管理器
    private ItemSO lastPresentedItem; // 最后出示的物品
    private System.Action<ItemSO> onItemPresented; // 物品出示回调

    [Header("UI场景设置")]
    [SerializeField] private string uiSceneName = "Ui";
    
    [Header("设置")]
    [SerializeField] private bool logDebugInfo = false; // 调试日志已禁用
    [Tooltip("对话冷却时间（秒），决定两段对话之间的最小间隔")]
    [SerializeField] private float dialogueCooldown = 1.0f;

    [Header("打字机效果设置")]
    [Tooltip("每个字符显示的时间间隔（秒）")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    [Tooltip("是否可以通过按键跳过打字机效果")]
    [SerializeField] private bool canSkipTypewriter = true;

    // 当前对话图
    [System.NonSerialized]
    private DialogueNodeGraph currentGraph;

    // 打字机效果相关变量
    private Coroutine typewriterCoroutine = null;
    private bool isTyping = false;
    private string fullDialogueText = "";
    private bool canCheckSkipInput = false; // 是否可以检查跳过输入
    private bool skipTypewriterForButtonAction = false; // 当点击按钮时跳过打字机效果
    private BaseNode lastButtonActionNode = null; // 记录最后一个执行按钮操作的节点
    private bool hasShownFirstDialogueAfterOption = false; // 选项选择后是否已显示首次对话
    
    // 选项回调
    [System.NonSerialized]
    private Action<int> onOptionSelected;
    
    // 对话事件 - 为音频系统添加
    // 对话开始事件
    public event Action OnDialogueStartEvent;
    // 对话结束事件
    public event Action OnDialogueEndedEvent;
    // 节点变化事件
    public event Action<BaseNode> OnNodeChangedEvent;
    // 选项选择事件
    public event Action<int> OnOptionSelectedEvent;
    // 对话文本变化事件
    public event Action<string> OnDialogueTextChangeEvent;
    
    // 当前节点是否是结束节点
    public bool isCurrentNodeEndNode { get; private set; }
    
    // 对话冷却计时器
    private float lastInputTime = 0f;
    
    // 临时存储等待Timeline完成后要处理的目标节点
    private BaseNode _pendingTargetNode;

        [SerializeField] private GameObject qiemanPanel; // 拖入 Canvas 1/且慢
        [SerializeField] private Animator qiemanAnimator; // 拖入 Canvas 1/且慢 的 Animator

        [System.Serializable]
        public class UIAnimEntry
        {
            public UIAnimType animType;
            public GameObject animPanel;
            public Animator animAnimator;
        }
        [SerializeField] private List<UIAnimEntry> uiAnimEntries = new List<UIAnimEntry>();

        [Header("Timeline联动")]
        public PlayableDirector timelineDirector; // 已有的Timeline引用(可作为默认值使用)
        
        // 当前活跃的Timeline Director
        [System.NonSerialized]
        private PlayableDirector _activeTimelineDirector;
        
        // 设置当前活跃的Timeline Director
        public void SetActiveTimelineDirector(PlayableDirector director)
        {
            if (director != null)
            {
                _activeTimelineDirector = director;
                LogDebug($"设置当前活跃Timeline: {director.gameObject.name}");
            }
        }

    [Header("摄像机震动设置")]
    [Tooltip("是否启用摄像机震动效果")]
    [SerializeField] private bool enableCameraShake = true;
    [Tooltip("UI动画时是否触发震动")]
    [SerializeField] private bool shakeOnUIAnimation = true;

    // 初始化出示功能
    private void InitializePresentButton()
    {
        // 检查面板和按钮引用
        if (presentItemPanel != null)
        {
            // 默认隐藏出示面板
            presentItemPanel.SetActive(false);
        }
        else
        {
            LogWarning("InitializePresentButton: presentItemPanel为null");
        }
        
        if (showItemButton != null)
        {
            // 移除可能的旧监听器
            showItemButton.onClick.RemoveAllListeners();
            
            // 添加点击监听器
            showItemButton.onClick.AddListener(OnShowItemButtonClicked);
        }
        else
        {
            LogWarning("InitializePresentButton: showItemButton为null");
        }
    }

    // 出示按钮点击处理
    private void OnShowItemButtonClicked()
    {
        // 标记当前是按钮操作，避免重复触发打字机效果
        skipTypewriterForButtonAction = true;
        
        // 记录当前节点为按钮操作节点
        if (currentGraph != null)
        {
            try {
                var node = currentGraph.GetType().GetField("currentNode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (node != null)
                {
                    lastButtonActionNode = node.GetValue(currentGraph) as BaseNode;
                    if (lastButtonActionNode != null)
                    {
                        LogDebug($"点击出示按钮，记录当前节点 {lastButtonActionNode.name} 为按钮操作节点");
                    }
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"获取当前节点失败：{e.Message}");
            }
        }
        
        // 如果没有背包输入控制器，无法打开背包
        if (inventoryInput == null)
        {
            LogError("无法打开背包：inventoryInput为null");
            skipTypewriterForButtonAction = false; // 重置状态
            lastButtonActionNode = null;
            return;
        }
        
        // 打开背包进行物品选择
        OpenInventoryForItemPresentation();
    }

    // 开启物品出示模式，打开背包
    private void OpenInventoryForItemPresentation()
    {
        if (inventoryInput != null)
        {
            // 设置背包物品选择后的回调
            InventoryManager.Instance.OnSlotSelected += OnItemSelectedForPresentation;
            
            // 锁定背包，禁止关闭
            if (UIManager.Instance != null)
            {
                UIManager.Instance.LockInventory(true);
                LogDebug("出示对话时锁定背包，防止意外关闭");
            }
            
            // 打开背包
            inventoryInput.OpenBagForItemSelection();
            
            // 启动协程检测背包状态
            StartCoroutine(CheckInventoryClosed());
        }
    }
    
    // 检测背包是否处于打开状态
    private bool IsInventoryOpen()
    {
        if (UIManager.Instance != null)
        {
            return UIManager.Instance.IsInventoryOpen();
        }
        return false;
    }
    
    // 检测背包关闭状态的协程
    private IEnumerator CheckInventoryClosed()
    {
        bool inventoryWasOpen = false;
        bool itemSelected = false;
        ItemSO currentItem = lastPresentedItem; // 记录当前物品状态
        
        // 等待背包打开
        yield return new WaitForSeconds(0.2f);
        
        // 检测背包打开状态
        inventoryWasOpen = IsInventoryOpen();
        
        // 如果背包成功打开，等待关闭
        if (inventoryWasOpen)
        {
            // 每帧检测背包是否关闭
            while (inventoryWasOpen && !itemSelected)
            {
                // 检查是否已经选择了物品(比较物品是否改变)
                itemSelected = (lastPresentedItem != currentItem);
                
                // 检查背包是否关闭
                inventoryWasOpen = IsInventoryOpen();
                
                yield return null; // 等待下一帧
            }
            
            // 如果背包已关闭但没有选择物品
            if (!inventoryWasOpen && !itemSelected)
            {
                // 移除物品选择监听
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.OnSlotSelected -= OnItemSelectedForPresentation;
                }
                
                // 解锁背包
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.LockInventory(false);
                    LogDebug("背包已解锁");
                }
                
                // 关闭出示面板
                DisableItemPresentation();
                
                // 重置按钮状态
                skipTypewriterForButtonAction = false;
                lastButtonActionNode = null;
                
                LogDebug("检测到背包关闭但未选择物品，自动关闭出示面板");
            }
        }
        else
        {
            // 背包没有打开，等待一些时间再检查
            yield return new WaitForSeconds(1.0f);
            
            // 再次检查是否选择了物品
            itemSelected = (lastPresentedItem != currentItem);
            
            // 如果仍然没有选择物品，自动关闭出示面板
            if (!itemSelected)
            {
                // 移除物品选择监听
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.OnSlotSelected -= OnItemSelectedForPresentation;
                }
                
                // 解锁背包
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.LockInventory(false);
                    LogDebug("背包未打开，解锁背包");
                }
                
                // 关闭出示面板
                DisableItemPresentation();
                
                // 重置按钮状态
                skipTypewriterForButtonAction = false;
                lastButtonActionNode = null;
                
                LogDebug("背包未打开或打开失败，自动关闭出示面板");
            }
        }
    }

    // 物品选择回调
    private void OnItemSelectedForPresentation(ItemSlot selectedSlot)
    {
        if (selectedSlot != null && selectedSlot.GetItem() != null)
        {
            // 获取选中的物品
            Item selectedItem = selectedSlot.GetItem();
            
            // 保存出示的物品数据
            lastPresentedItem = selectedItem.ItemData;
            
            // 关闭背包
            if (UIManager.Instance != null)
            {
                // 先解锁背包，允许关闭
                UIManager.Instance.LockInventory(false);
                LogDebug("物品选择完成，解锁背包");
                
                // 关闭背包
                UIManager.Instance.ToggleInventory();
            }
            
            // 取消物品选择监听
            InventoryManager.Instance.OnSlotSelected -= OnItemSelectedForPresentation;
            
                    // 保持按钮操作状态，并保持当前节点作为按钮操作节点记录
            // 这样在后续对话中，仍会跳过打字机效果，但仅限于当前节点
            // lastButtonActionNode保持不变，这样在节点变化时能正确检测
            LogDebug("物品选择完成，保持按钮操作状态，为后续对话准备");
            
            // 触发物品出示回调
            onItemPresented?.Invoke(lastPresentedItem);
            
            // 记录日志
            LogDebug($"向NPC出示物品: {lastPresentedItem.itemName}");
        }
    }

    // 显示出示面板，并设置回调
    public void EnableItemPresentation(System.Action<ItemSO> callback)
    {
            // 在PresentationNode中调用此方法之前，会先调用ShowDialogue
    // 所以这里需要设置标志，然后立即应用到当前正在显示的对话上
    skipTypewriterForButtonAction = true;
    
    // 记录当前节点为按钮操作节点
    if (currentGraph != null)
    {
        try {
            var node = currentGraph.GetType().GetField("currentNode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (node != null)
            {
                lastButtonActionNode = node.GetValue(currentGraph) as BaseNode;
                if (lastButtonActionNode != null)
                {
                    LogDebug($"启用出示功能，记录当前节点 {lastButtonActionNode.name} 为按钮操作节点");
                }
            }
        }
        catch (System.Exception e) {
            Debug.LogError($"获取当前节点失败：{e.Message}");
        }
    }
    
    // 如果正在显示对话，立即完成打字机效果
    if (isTyping && typewriterCoroutine != null)
    {
        LogDebug("检测到正在打字，立即完成打字机效果");
        CompleteTypewriter();
    }
        
        if (presentItemPanel != null)
        {
            presentItemPanel.SetActive(true);
            onItemPresented = callback;
            
            // 添加调试日志
            LogDebug("已启用出示功能，面板已激活");
        }
        else
        {
            LogError("EnableItemPresentation: presentItemPanel为null");
            skipTypewriterForButtonAction = false; // 出错时重置状态
        }
    }

    // 隐藏出示面板
    public void DisableItemPresentation()
    {
        if (presentItemPanel != null)
        {
            presentItemPanel.SetActive(false);
            onItemPresented = null;
            
            // 添加调试日志
            LogDebug("已禁用出示功能，面板已隐藏");
        }
        else
        {
            LogWarning("DisableItemPresentation: presentItemPanel为null");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // 将对象移动到UI场景或保持不销毁
            Scene uiScene = SceneManager.GetSceneByName(uiSceneName);
            if (uiScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(gameObject, uiScene);
                LogDebug($"对话UI管理器已移动到 {uiSceneName} 场景");
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                LogDebug("对话UI管理器设置为不销毁");
            }
            
            // 添加场景加载事件监听
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // 检查组件引用
        CheckReferences();
        
        // 初始化选项按钮
        InitOptionButtons();
        InitializePresentButton(); // 初始化出示按钮

        // 连接到输入管理器
        if (DialogueInputManager.Instance != null)
        {
            DialogueInputManager.Instance.SetDialogueActive(false);
        }
    }
    
    // 检查组件引用
    private void CheckReferences()
    {
        // 使用简化的方式检查组件引用
        string[] missingComponents = new string[] {};
        
        if (dialogueCanvas == null) missingComponents = AddToArray(missingComponents, "dialogueCanvas");
        if (speakerText == null) missingComponents = AddToArray(missingComponents, "speakerText");
        if (speakerImage == null) missingComponents = AddToArray(missingComponents, "speakerImage");
        if (dialogueText == null) missingComponents = AddToArray(missingComponents, "dialogueText");
        if (optionPanel == null) missingComponents = AddToArray(missingComponents, "optionPanel");
        if (optionButtons == null || optionButtons.Length == 0) missingComponents = AddToArray(missingComponents, "optionButtons");
        if (presentItemPanel == null) missingComponents = AddToArray(missingComponents, "presentItemPanel (出示面板)");
        if (showItemButton == null) missingComponents = AddToArray(missingComponents, "showItemButton (出示按钮)");
        
        // 只记录一次日志
        if (missingComponents.Length > 0)
        {
            LogError($"DialogueUIManager: 缺少组件引用: {string.Join(", ", missingComponents)}");
        }
    }
    
    // 辅助方法：向数组添加元素
    private string[] AddToArray(string[] array, string item)
    {
        string[] newArray = new string[array.Length + 1];
        array.CopyTo(newArray, 0);
        newArray[array.Length] = item;
        return newArray;
    }

    private void InitOptionButtons()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                int optionIndex = i; // 保存索引，避免闭包问题
                Button button = optionButtons[i].GetComponent<Button>();
                if (button != null)
                {
                    // 设置按钮不保持焦点，防止空格键误触发
                    Navigation nav = button.navigation;
                    nav.mode = Navigation.Mode.None;
                    button.navigation = nav;
                    
                    // 确保按钮点击后不再保持焦点
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => {
                        // 点击后立即移除焦点
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                        OnOptionClicked(optionIndex);
                    });
                }
            }
        }
    }

    private void OnOptionClicked(int optionIndex)
    {
        LogDebug("选择了选项: " + optionIndex);
        
        // 标记当前是按钮操作，但保证第一次对话会有打字机效果
        skipTypewriterForButtonAction = true;
        hasShownFirstDialogueAfterOption = false; // 重置首次对话标记
        
        // 记录当前节点为按钮操作节点
        if (currentGraph != null)
        {
            try {
                var node = currentGraph.GetType().GetField("currentNode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (node != null)
                {
                    lastButtonActionNode = node.GetValue(currentGraph) as BaseNode;
                    if (lastButtonActionNode != null)
                    {
                        LogDebug($"点击选项按钮，记录当前节点 {lastButtonActionNode.name} 为按钮操作节点");
                    }
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"获取当前节点失败：{e.Message}");
            }
        }
        
        // 触发选项选择事件
        OnOptionSelectedEvent?.Invoke(optionIndex);
        
        // 临时存储回调，避免多次触发
        var callback = onOptionSelected;
        
        // 立即清空回调和选项面板，防止重复触发
        onOptionSelected = null;
        HideOptions();
        
        // 注意：不再在这里触发全局震动，而是依赖对话节点中的选项震动设置
        // 震动将在DialogueNode.ContinueDialogueAfterOption方法中根据每个选项的设置触发
        
        // 调用选项回调
        callback?.Invoke(optionIndex);
    }

    public void SetDialogueGraph(DialogueNodeGraph graph)
    {
        if (graph == null)
        {
            LogError("SetDialogueGraph: 传入的对话图为null");
            return;
        }
        
        LogDebug("设置对话图: " + graph.name);
        currentGraph = graph;
    }

    // 检查当前节点是否是结束节点
    public void SetCurrentNodeAsEndNode(bool isEndNode)
    {
        isCurrentNodeEndNode = isEndNode;
        LogDebug("当前节点结束状态: " + (isCurrentNodeEndNode ? "是结束节点" : "非结束节点"));
    }

    // 显示对话内容 - 根据状态决定是否使用打字机效果
    public void ShowDialogue(ActorSO actor, string text)
    {
        if (dialogueCanvas == null)
        {
            LogError("ShowDialogue: dialogueCanvas为null");
            return;
        }
        
        // 添加调试日志
        LogDebug($"ShowDialogue: 显示角色 {(actor != null ? actor.actorName : "无角色")} 的对话，文本长度: {(text != null ? text.Length : 0)}");
        
        // 确保对话框显示
        if (!dialogueCanvas.activeInHierarchy)
        {
            dialogueCanvas.SetActive(true);
            LogDebug("激活对话框");
        }

        // 避免null或空文本
        string displayText = !string.IsNullOrEmpty(text) ? text : "";
        
        // 保存完整文本内容，包含"按空格键继续"的提示
        fullDialogueText = displayText;
        if (isCurrentNodeEndNode)
        {
            fullDialogueText += "\n\n<color=#888888><size=80%>[按空格键继续]</size></color>";
        }
        
        // 更新说话者信息
        if (speakerText != null)
        {
            speakerText.text = actor != null ? actor.actorName : "";
        }
        
        if (speakerImage != null)
        {
            speakerImage.sprite = actor != null ? actor.actorSprite : null;
        }
        
        // 强制使用打字机效果，无论useTypewriterEffect设置如何
        if (dialogueText != null)
        {
            // 如果之前有正在执行的打字机协程，先停止
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                isTyping = false;
                canCheckSkipInput = false; // 重置跳过检测状态
            }
            
            // 检查是否需要跳过打字机效果
            if (skipTypewriterForButtonAction && hasShownFirstDialogueAfterOption)
            {
                // 按钮操作后且已显示过第一次对话，直接显示完整文本，不使用打字机效果
                LogDebug($"按钮操作后续对话，跳过打字机效果，直接显示文本");
                dialogueText.text = fullDialogueText;
            }
            else
            {
                // 第一次对话或正常情况下使用打字机效果
                dialogueText.text = ""; // 先清空文本
                typewriterCoroutine = StartCoroutine(TypewriterEffect(fullDialogueText));
                
                // 如果是按钮操作后的第一次对话，标记已显示
                if (skipTypewriterForButtonAction && !hasShownFirstDialogueAfterOption)
                {
                    hasShownFirstDialogueAfterOption = true;
                    LogDebug($"按钮操作后的第一次对话，触发打字机效果");
                }
                else
                {
                    LogDebug($"开始新的打字机效果，文本:{(fullDialogueText.Length > 10 ? fullDialogueText.Substring(0, 10) + "..." : fullDialogueText)}");
                }
            }
        }
        else
        {
            LogError("ShowDialogue: dialogueText为null，无法显示文本");
        }
    }

    // 显示选项 - 简化版
    public void ShowOptions(List<string> options, Action<int> callback)
    {
        if (optionPanel == null || optionButtons == null)
        {
            LogError("ShowOptions: optionPanel或optionButtons为null");
            return;
        }
        
        // 保存回调
        onOptionSelected = callback;
        
        // 显示选项面板
        optionPanel.SetActive(true);
        
        // 清除并设置选项按钮
        int optionCount = Mathf.Min(options.Count, optionButtons.Length);
        
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            
            bool isActive = i < optionCount;
            optionButtons[i].SetActive(isActive);
            
            if (isActive)
            {
                // 设置选项文本 - 优先使用TMP_Text
                TMP_Text buttonText = optionButtons[i].GetComponentInChildren<TMP_Text>();
                Text legacyText = buttonText == null ? optionButtons[i].GetComponentInChildren<Text>() : null;
                
                if (buttonText != null)
                    buttonText.text = options[i];
                else if (legacyText != null)
                    legacyText.text = options[i];
                else
                    LogWarning($"ShowOptions: 选项按钮 {i} 没有Text或TMP_Text组件");
            }
        }
    }

    // 隐藏选项
    public void HideOptions()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }
    }

    // 隐藏对话框
    public void HideDialogue()
    {
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
        }
    }

    // 修改Update方法，处理打字机效果的跳过
    private void Update()
    {
        // 简化的调试信息
        if (logDebugInfo && Input.GetKeyDown(KeyCode.Space))
        {
            bool isActive = currentGraph != null;
            bool canProcess = Time.time > lastInputTime + dialogueCooldown;
            bool inputManagerActive = DialogueInputManager.Instance?.IsGlobalDialogueActive ?? false;
            
            LogDebug($"空格键 - 对话:{isActive}, 可处理:{canProcess}, 输入管理器:{inputManagerActive}, 打字中:{isTyping}");
        }
        
        // 处理打字机效果的跳过 - 确保IsSkipPressed使用正确的标志位
        if (isTyping && canSkipTypewriter && IsSkipPressed())
        {
            LogDebug("检测到跳过按键，完成打字机效果");
            CompleteTypewriter();
            return; // 如果正在打字，先处理打字机效果的跳过，不继续处理对话推进
        }
        
        // 只有在未打字状态下才处理对话推进
        if (!isTyping && currentGraph != null && 
            Time.time > lastInputTime + dialogueCooldown &&
            DialogueInputManager.Instance != null && 
            DialogueInputManager.Instance.IsGlobalDialogueActive)
        {
            // 检查是否按下了推进对话的按键
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Mouse0))
            {
                lastInputTime = Time.time;
                
                // 停止当前语音播放
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopVoice();
                }
                
                // 检查当前节点是否是结束节点
                if (isCurrentNodeEndNode)
                {
                    // 如果是结束节点，结束对话
                    LogDebug("Next: 当前节点是结束节点，结束对话");
                    EndDialogue();
                }
                else
                {
                    // 否则处理下一个节点
                    LogDebug("Next: 处理下一个节点");
                    currentGraph.ProcessNextNode();
                }
            }
        }
    }

    // 开始对话
    public void StartDialogue(DialogueNodeGraph graph)
    {
        if (graph == null)
        {
            LogError("StartDialogue: 传入的对话图为null");
            return;
        }
        
        // 设置当前对话图
        currentGraph = graph;
        
        // 通知输入管理器对话已开始
        if (DialogueInputManager.Instance != null)
        {
            DialogueInputManager.Instance.SetDialogueActive(true);
        }
        
        // 清空对话内容但不显示UI
        ClearDialogueContent();
        
        // 触发对话开始事件
        OnDialogueStartEvent?.Invoke();
        
        // 启动对话 - 先准备内容再显示UI
        StartCoroutine(StartDialogueDelayed());
    }

    // 清空对话内容
    private void ClearDialogueContent()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        
        if (speakerText != null)
        {
            speakerText.text = "";
        }
        
        if (speakerImage != null)
        {
            speakerImage.sprite = null;
        }
        
        HideOptions();
    }

    // 延迟启动对话，确保UI已正确设置
    private IEnumerator StartDialogueDelayed()
    {
        // 先保持UI隐藏状态
        if (dialogueCanvas != null && dialogueCanvas.activeInHierarchy)
        {
            dialogueCanvas.SetActive(false);
        }
        
        // 等待一帧，确保UI已正确设置
        yield return null;
        
        // 检查currentGraph是否为null
        if (currentGraph == null)
        {
            LogError("StartDialogueDelayed: currentGraph为null");
            OnDialogueEnded(); // 直接结束对话
            yield break;
        }
        
        // 准备对话内容
        // 检查是否是一次性对话且已完成
        if (currentGraph.startNode is DialogueNode dialogueStartNode && 
            dialogueStartNode.isOneTimeDialogue && 
            currentGraph.IsDialogueCompleted(dialogueStartNode))
        {
            LogDebug("对话已完成，不再触发");
            OnDialogueEnded(); // 直接结束对话
            yield break; // 退出协程
        }
        
        // 预处理对话图，但不显示UI
        currentGraph.PrepareDialogue(this);
        
        // 准备好后再显示UI
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
        }
        
        // 正式开始对话
        currentGraph.StartDialogue(this);
    }

    // 处理下一步
    public void Next()
    {
        if (currentGraph == null)
        {
            LogWarning("Next: 当前没有活动的对话图");
            return;
        }
        
        // 检查是否在冷却期
        if (Time.time < lastInputTime + dialogueCooldown) // 使用配置的对话冷却时间
        {
            LogDebug($"Next: 输入冷却中，忽略输入 - 剩余冷却:{(lastInputTime + dialogueCooldown - Time.time):F2}秒");
            return;
        }
        
        lastInputTime = Time.time;
        
        // 停止当前语音播放
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopVoice();
        }
        
        // 检查当前节点是否是结束节点
        if (isCurrentNodeEndNode)
        {
            // 如果是结束节点，结束对话
            LogDebug("Next: 当前节点是结束节点，结束对话");
            EndDialogue();
        }
        else
        {
            // 否则处理下一个节点
            LogDebug("Next: 处理下一个节点");
            currentGraph.ProcessNextNode();
        }
    }

    // 对话结束处理方法
    public void OnDialogueEnded()
    {
        // 隐藏对话界面
        HideDialogue();
        
        // 通知输入管理器对话已结束
        if (DialogueInputManager.Instance != null)
        {
            DialogueInputManager.Instance.SetDialogueActive(false);
        }
        
        // 重置是否为终止节点的标记
        isCurrentNodeEndNode = false;
        
        // 触发对话结束事件
        OnDialogueEndedEvent?.Invoke();
        
        // 重置当前对话图
        currentGraph = null;
    }

    // 对话结束
    public void EndDialogue()
    {
        // 停止当前语音播放
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopVoice();
        }

        // 调用对话结束处理方法
        OnDialogueEnded();
    }
    
    // 处理节点变化 - 增加逻辑以区分普通节点和按钮操作节点
    public void HandleNodeChanged(BaseNode node)
    {
        // 触发节点变化事件
        OnNodeChangedEvent?.Invoke(node);
        
        // 检查是否需要重置跳过打字机标志
        // 如果当前节点不是最后操作的按钮节点，则恢复打字机效果
        if (node != null && lastButtonActionNode != null && node != lastButtonActionNode)
        {
            skipTypewriterForButtonAction = false;
            hasShownFirstDialogueAfterOption = false; // 重置首次对话标记
            LogDebug("切换到新的非按钮操作节点，恢复打字机效果");
        }
        
        LogDebug($"节点变化：切换到新节点 {(node != null ? node.name : "null")}, 跳过打字机:{skipTypewriterForButtonAction}, 已显示首次对话:{hasShownFirstDialogueAfterOption}");
    }

    // 统一的日志系统 - 调试信息已禁用
    private void LogDebug(string message)
    {
        // 调试日志已禁用
        return;
    }
    
    private void LogWarning(string message) => Debug.LogWarning($"[DialogueUIManager] {message}");
    
    private void LogError(string message) => Debug.LogError($"[DialogueUIManager] {message}");
    
    // 以下是现有的其他方法...

    // 添加场景加载处理方法
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果是UI场景，不需要处理
        if (scene.name == uiSceneName)
            return;
        
        // 更新UI引用，但不重置对话状态
        UpdateUIReferences();
        
        // 注意：对话状态将由DialogueContinuityManager管理，这里不再重置
        LogDebug("场景切换 - 已更新UI引用");
    }

    private void OnDestroy()
    {
        // 移除事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void StartDialogueFromNode(DialogueNode dialogueNode)
    {
        if (dialogueNode == null)
        {
            LogError("StartDialogueFromNode: 对话节点为空");
            return;
        }
        
        // 显示对话UI
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
        }
        
        // 直接显示该节点的内容
        ActorSO actor = null;
        try {
            actor = dialogueNode.GetActor();
        } catch (System.Exception) {
            LogWarning("无法获取对话节点角色信息");
        }
        
        // 显示对话文本
        string dialogueText = "默认对话文本";
        try {
            dialogueText = dialogueNode.GetText();
        } catch (System.Exception) {
            LogWarning("无法获取对话文本");
        }
        ShowDialogue(actor, dialogueText);
        
        // 设置结束标志
        bool isEndNode = false;
        try {
            isEndNode = dialogueNode.IsEndNode();
        } catch (System.Exception) {
            LogWarning("无法获取节点结束状态");
        }
        SetCurrentNodeAsEndNode(isEndNode);
        
        LogDebug("直接从节点显示对话: " + dialogueNode.name);
    }

    public void OnOptionSelected(int optionIndex)
    {
        DialogueNode currentDialogueNode = currentGraph.currentNode as DialogueNode;
        if (currentDialogueNode == null) return;

        // 选项只是字符串，需要创建DialogueOption结构
        string optionText = currentDialogueNode.options[optionIndex]; 
        
        // 从nextNodes列表获取该选项对应的目标节点
        BaseNode targetNode = null;
        if (optionIndex < currentDialogueNode.nextNodes.Count) {
            targetNode = currentDialogueNode.nextNodes[optionIndex];
        }
        
        // 根据timelineID、动画名称等信息构造选项数据
        bool playTimeline = false;
        string timelineID = "";
        bool waitForTimeline = true;
        
        // 检查选项是否配置了Timeline
        if (currentDialogueNode.optionTimelineID != null && 
            optionIndex < currentDialogueNode.optionTimelineID.Count) {
            timelineID = currentDialogueNode.optionTimelineID[optionIndex];
            playTimeline = !string.IsNullOrEmpty(timelineID);
            
            // 获取等待设置
            if (currentDialogueNode.optionWaitForTimeline != null && 
                optionIndex < currentDialogueNode.optionWaitForTimeline.Count) {
                waitForTimeline = currentDialogueNode.optionWaitForTimeline[optionIndex];
            }
        }
        
        // 检查是否需要播放Timeline
        if (playTimeline && !string.IsNullOrEmpty(timelineID))
        {
            // 如果需要等待Timeline完成
            if (waitForTimeline)
            {
                // 使用TimelineController播放Timeline
                if (TimelineController.Instance != null)
                {
                    // 订阅Timeline完成事件
                    TimelineController.Instance.OnTimelineCompleted += OnTimelineCompletedHandler;
                    
                    // 临时保存目标节点，供事件处理器使用
                    _pendingTargetNode = targetNode;
                    
                    // 播放Timeline
                    TimelineController.Instance.PlayTimeline(timelineID);
                }
                else
                {
                    // 如果没有Timeline控制器，直接处理选项
                    ProcessNextNode(targetNode);
                }
            }
            else
            {
                // 不等待Timeline，同时播放并处理选项
                if (TimelineController.Instance != null)
                {
                    TimelineController.Instance.PlayTimeline(timelineID);
                }
                ProcessNextNode(targetNode);
            }
        }
        else
        {
            // 没有Timeline，直接处理选项
            ProcessNextNode(targetNode);
        }
    }

    // 选项处理逻辑 - 新版本不再使用DialogueOption
    private void ProcessNextNode(BaseNode targetNode)
    {
        // 更新当前节点并处理
        if (currentGraph != null)
        {
            currentGraph.currentNode = targetNode;
            currentGraph.ProcessCurrentNode();
        }
    }
    
    // Timeline完成事件处理
    private void OnTimelineCompletedHandler()
    {
        // 取消订阅事件，避免重复触发
        if (TimelineController.Instance != null)
        {
            TimelineController.Instance.OnTimelineCompleted -= OnTimelineCompletedHandler;
        }
        
        // 处理等待的目标节点
        if (_pendingTargetNode != null)
        {
            ProcessNextNode(_pendingTargetNode);
            _pendingTargetNode = null;
        }
    }

    // 更新UI引用 - 简化版
    private void UpdateUIReferences()
    {
        // 使用Unity的FindObjectOfType查找必要组件
        if (dialogueCanvas == null)
            dialogueCanvas = GameObject.Find("DialogueCanvas");
            
        if (speakerText == null)
            speakerText = GameObject.FindObjectOfType<TMP_Text>(true);
            
        if (speakerImage == null)
            speakerImage = GameObject.FindObjectOfType<Image>(true);
            
        // 确保引用有效
        CheckReferences();
        
        LogDebug("已更新UI引用");
    }

    // 添加公共属性访问器
    public GameObject DialogueCanvas => dialogueCanvas;
    public bool IsCurrentNodeEndNode => isCurrentNodeEndNode;
    public DialogueNodeGraph CurrentGraph => currentGraph;

    // 添加一个公共方法检查对话是否活跃
    public bool IsDialogueActive()
    {
        return dialogueCanvas != null && dialogueCanvas.activeInHierarchy;
    }

    // 原有的PlayAnimation方法
    public bool PlayAnimation(UIAnimType animType, string animName, bool waitForCompletion)
    {
        if (string.IsNullOrEmpty(animName)) return false;
        
        // 根据动画类型查找对应的UI动画条目
        UIAnimEntry entry = uiAnimEntries.Find(e => e.animType == animType);
        
        // 如果找到匹配的条目，播放动画
        if (entry != null && entry.animPanel != null && entry.animAnimator != null)
        {
            // 确保动画面板显示在最前方
            Canvas panelCanvas = entry.animPanel.GetComponent<Canvas>();
            if (panelCanvas != null)
            {
                // 设置为最高排序顺序
                panelCanvas.sortingOrder = 999;
            }
            else
            {
                // 如果面板没有Canvas组件，尝试将其移动到层级的最后（最前方）
                entry.animPanel.transform.SetAsLastSibling();
                
                // 检查父级是否有Canvas，如果有，确保其排序层级较高
                Canvas parentCanvas = entry.animPanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    parentCanvas.sortingOrder = 999;
                }
            }
            
            // 显示动画面板
            entry.animPanel.SetActive(true);
            
            // 播放动画
            entry.animAnimator.Play(animName);
            LogDebug($"播放动画: {animName}，类型: {animType}");
            
            // 如果需要震动且震动功能已启用，触发摄像机震动
            if (shakeOnUIAnimation && enableCameraShake)
            {
                TriggerCameraShake(UIAnimShakeIntensity(animType));
            }
            
            // 如果需要等待动画完成
            if (waitForCompletion)
            {
                // 启动协程等待动画完成
                StartCoroutine(WaitForAnimationComplete(entry.animAnimator, () => {
                    // 动画完成后隐藏面板
                    entry.animPanel.SetActive(false);
                    
                    // 继续对话
                    if (currentGraph != null)
                    {
                        currentGraph.ProcessNextNode();
                    }
                }));
            }
            else
            {
                // 即使不需要等待，也要自动隐藏动画面板
                StartCoroutine(AutoHideAnimationPanel(entry.animAnimator, entry.animPanel));
            }
            
            return true;
        }
        
        // 如果没找到匹配的条目，尝试使用且慢面板（兼容旧代码）
        if (animType == UIAnimType.Qieman)
        {
            return PlayQiemanAnimation(animName, waitForCompletion);
        }
            
        LogWarning($"未找到UI动画: {animName}，类型: {animType}");
        return false;
    }
    
    // 新增自动隐藏动画面板的协程
    private IEnumerator AutoHideAnimationPanel(Animator animator, GameObject panel)
    {
        // 等待一帧确保动画开始播放
        yield return null;
        
        // 获取当前动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        
        LogDebug($"等待动画完成后自动隐藏面板，动画长度: {animationLength}秒");
        
        // 等待动画播放完成
        yield return new WaitForSeconds(animationLength);
        
        // 隐藏面板
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // 添加根据UI动画类型确定震动强度的辅助方法
    private float UIAnimShakeIntensity(UIAnimType animType)
    {
        switch (animType)
            {
            case UIAnimType.None:
                return 0.2f;
            case UIAnimType.Qieman:
                return 0.5f;
            case UIAnimType.Wait:
                return 0.3f;
            case UIAnimType.Special:
                return 0.8f;
            default:
                return 0.4f;
        }
    }

    // 播放且慢动画
    private bool PlayQiemanAnimation(string animName, bool waitForCompletion)
    {
        if (qiemanPanel != null && qiemanAnimator != null)
        {
            // 确保且慢面板显示在最前方
            Canvas panelCanvas = qiemanPanel.GetComponent<Canvas>();
            if (panelCanvas != null)
            {
                // 设置为最高排序顺序
                panelCanvas.sortingOrder = 999;
            }
            else
            {
                // 如果面板没有Canvas组件，尝试将其移动到层级的最后（最前方）
                qiemanPanel.transform.SetAsLastSibling();
                
                // 检查父级是否有Canvas，如果有，确保其排序层级较高
                Canvas parentCanvas = qiemanPanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    parentCanvas.sortingOrder = 999;
                }
            }
            
            // 显示且慢面板
            qiemanPanel.SetActive(true);
            
            // 播放且慢动画
            string animationName = string.IsNullOrEmpty(animName) ? "Qieman" : animName;
            qiemanAnimator.Play(animationName);
            
            // 如果需要等待动画完成
            if (waitForCompletion)
            {
                // 启动协程等待动画完成
                StartCoroutine(WaitForAnimationComplete(qiemanAnimator, () => {
                    // 动画完成后隐藏面板
                    qiemanPanel.SetActive(false);
                    
                    // 继续对话
                    if (currentGraph != null)
                    {
                        currentGraph.ProcessNextNode();
                    }
                }));
            }
            else
            {
                // 即使不需要等待，也要自动隐藏动画面板
                StartCoroutine(AutoHideAnimationPanel(qiemanAnimator, qiemanPanel));
            }
            
            return true;
        }
        
        return false;
    }

    // 等待动画完成的协程
    private IEnumerator WaitForAnimationComplete(Animator animator, System.Action onComplete)
    {
        // 等待一帧确保动画开始播放
        yield return null;
        
        // 获取当前动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        
        LogDebug($"等待动画完成，动画长度: {animationLength}秒");
        
        // 等待动画播放完成
        yield return new WaitForSeconds(animationLength);
        
        // 执行完成回调
        if (onComplete != null)
        {
            onComplete();
        }
    }

    // 添加摄像机震动方法
    private void TriggerCameraShake(float intensity)
    {
        // 尝试获取CameraShakeManager实例并触发震动
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeCustom(intensity);
        }
        else
        {
            LogWarning("未找到CameraShakeManager，无法触发摄像机震动");
        }
    }

    // 公开的震动触发方法，供对话节点或动画事件调用
    public void ShakeCamera(string intensityType)
    {
        if (!enableCameraShake) return;
        
        if (CameraShakeManager.Instance == null)
        {
            LogWarning("未找到CameraShakeManager，无法触发摄像机震动");
            return;
        }
        
        // 根据传入的强度类型触发不同的震动
        switch (intensityType.ToLower())
        {
            case "light":
                CameraShakeManager.Instance.ShakeLight();
                break;
            case "medium":
                CameraShakeManager.Instance.ShakeMedium();
                break;
            case "strong":
                CameraShakeManager.Instance.ShakeStrong();
                break;
            case "ace":
            case "objection":
            case "aceattorney":
                CameraShakeManager.Instance.ShakeAceAttorney();
                break;
            default:
                // 默认使用中等强度
                CameraShakeManager.Instance.ShakeMedium();
                break;
        }
    }

    // 新增：打字机效果协程
    private IEnumerator TypewriterEffect(string textToType)
    {
        isTyping = true;
        
        // 延迟一帧后才开始检测跳过输入，避免在打字机效果开始前的按键被检测到
        yield return null;
        canCheckSkipInput = true;
        
        // 清空文本
        if (dialogueText == null)
        {
            Debug.LogError("[DialogueUIManager] TypewriterEffect: dialogueText为null");
            isTyping = false;
            yield break;
        }
        
        dialogueText.text = "";
        
        // 用于存储正在构建的富文本标签
        string currentTag = "";
        bool insideTag = false;
        
        // 临时存储已显示的文本
        StringBuilder visibleText = new StringBuilder();
        
        // 逐字显示
        for (int i = 0; i < textToType.Length; i++)
        {
            // 如果协程被中断或dialogueText为null，中止打字效果
            if (dialogueText == null)
            {
                isTyping = false;
                yield break;
            }
            
            char currentChar = textToType[i];
            
            // 处理富文本标签
            if (currentChar == '<')
            {
                insideTag = true;
                currentTag = "<";
                visibleText.Append(currentChar);
                continue;
            }
            else if (insideTag)
            {
                currentTag += currentChar;
                visibleText.Append(currentChar);
                
                if (currentChar == '>')
                {
                    insideTag = false;
                }
                continue;
            }
            
            // 添加当前字符到可见文本
            visibleText.Append(currentChar);
            dialogueText.text = visibleText.ToString();
            
            // 触发对话文本变化事件
            OnDialogueTextChangeEvent?.Invoke(dialogueText.text);
            
            // 使用固定延迟
            float delay = typewriterSpeed;
            
            // 等待一定时间再显示下一个字符
            float endTime = Time.time + delay;
            while (Time.time < endTime)
            {
                // 检查是否跳过打字机效果
                if (canSkipTypewriter && IsSkipPressed())
                {
                    // 跳过剩余打字效果，直接显示全部文本
                    dialogueText.text = textToType;
                    OnDialogueTextChangeEvent?.Invoke(textToType);
                    isTyping = false;
                    canCheckSkipInput = false; // 重置跳过检测状态
                    yield break;
                }
                yield return null;
            }
        }
        
        isTyping = false;
        canCheckSkipInput = false; // 打字机效果完成后，禁用跳过输入检测
    }

    // 新增：判断是否按下了跳过打字机效果的按键
    private bool IsSkipPressed()
    {
        // 只有在打字机效果开始后且允许检查输入时才检测按键
        if (!canCheckSkipInput) return false;
        
        // 恢复所有跳过按键的检测
        return Input.GetKeyDown(KeyCode.Space) || 
               Input.GetKeyDown(KeyCode.Return) || 
               Input.GetKeyDown(KeyCode.Mouse0);
    }

    // 新增：立即完成打字机效果
    public void CompleteTypewriter()
    {
        if (isTyping && typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            dialogueText.text = fullDialogueText;
            OnDialogueTextChangeEvent?.Invoke(fullDialogueText);
            isTyping = false;
            canCheckSkipInput = false; // 重置跳过检测状态
        }
    }
} 

// 创建一个扩展组件，不需要修改原有DialogueUIManager
[RequireComponent(typeof(DialogueUIManager))]
public class DialogueTimelineController : MonoBehaviour
{
    // 事件系统，用于通知对话完成
    public static event Action OnDialogueCompleted;
    
    // 当前正在控制的Timeline
    private PlayableDirector _currentDirector;
    private bool _isDialogueActive = false;
    private DialogueUIManager _dialogueUIManager;
    
    private void Awake()
    {
        _dialogueUIManager = GetComponent<DialogueUIManager>();
        
        // 监听原有DialogueUIManager的对话完成事件
        // 如果原系统没有事件，需要修改其代码添加事件
        if (_dialogueUIManager != null)
        {
            // 假设DialogueUIManager有onDialogueEnd事件
            // _dialogueUIManager.onDialogueEnd.AddListener(OnDialogueFinished);
            
            // 如果没有事件，则需要通过其他方式监听对话完成
            // 例如每帧检查对话状态
        }
    }
    
    private void Update()
    {
        // 如果DialogueUIManager没有提供事件系统，可以在每帧检测对话状态
        if (_isDialogueActive && _dialogueUIManager != null)
        {
            // 检查对话Canvas是否激活
            var dialogueCanvas = _dialogueUIManager.transform.Find("DialogueCanvas")?.gameObject;
            if (dialogueCanvas != null && !dialogueCanvas.activeInHierarchy)
            {
                OnDialogueFinished();
                return;
            }
            
            // 尝试通过反射获取dialogueCanvas
            if (dialogueCanvas == null)
            {
                var fields = _dialogueUIManager.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.Name.Contains("dialogueCanvas"))
                    {
                        GameObject canvas = field.GetValue(_dialogueUIManager) as GameObject;
                        if (canvas != null && !canvas.activeInHierarchy)
                        {
                            OnDialogueFinished();
                            return;
                        }
                    }
                }
            }
        }
    }
    
    public void SetCurrentDirector(PlayableDirector director)
    {
        _currentDirector = director;
        Debug.Log($"设置当前Timeline Director: {director.gameObject.name}");
    }
    
    // 从Timeline信号调用此方法开始对话
    public void StartTimelineDialogue(DialogueNode dialogueNode)
    {
        // 使用TimelineManager获取当前Director
        PlayableDirector director = TimelineManager.Instance.CurrentPlayingDirector;
        
        // 暂停Timeline
        if (director != null) {
            TimelineDebugTool.PauseForDialogue(director);
            // 保存当前Director引用
            _currentDirector = director;
        } else {
            Debug.LogWarning("无法获取当前播放的Timeline Director");
        }
        
        // 开始对话
        _isDialogueActive = true;
        
        // 使用临时方案直接访问节点内容显示对话
        if (_dialogueUIManager != null)
        {
            // 显示对话UI
            DialogueCanvasSetActive(true);
            
            // 直接显示对话内容
            if (dialogueNode != null)
            {
                ActorSO actor = null;
                // 获取角色 - 使用扩展方法
                try {
                    actor = dialogueNode.GetActor();
                } catch (System.Exception) {
                    Debug.LogWarning("无法获取对话节点角色信息");
                }
                
                // 获取对话文本 - 使用扩展方法
                string text = dialogueNode.GetText();
                
                // 显示对话
                _dialogueUIManager.ShowDialogue(actor, text);
            }
            else
            {
                Debug.LogError("对话节点为空");
            }
        }
        
        Debug.Log($"开始Timeline对话");
    }
    
    // 工具方法 - 激活/关闭对话Canvas
    private void DialogueCanvasSetActive(bool active)
    {
        if (_dialogueUIManager == null) return;
        
        // 使用公共属性访问Canvas
        GameObject canvas = _dialogueUIManager.DialogueCanvas;
        if (canvas != null)
        {
            canvas.SetActive(active);
        }
        else
        {
            Debug.LogWarning("找不到DialogueCanvas");
        }
    }
    
    private void OnDialogueFinished()
    {
        _isDialogueActive = false;
        
        // 恢复Timeline播放
        if (_currentDirector != null && _currentDirector.state == PlayState.Paused)
        {
            _currentDirector.Play();
            Debug.Log($"对话结束，恢复Timeline: {_currentDirector.gameObject.name}");
        }
        else if (TimelineDebugTool.currentDialogueDirector != null)
        {
            // 尝试使用TimelineDebugTool中保存的引用
            TimelineDebugTool.ResumeAfterDialogue();
        }
        
        // 触发对话完成事件
        OnDialogueCompleted?.Invoke();
    }
    
    private void OnDestroy()
    {
        // 清理事件监听
        // _dialogueUIManager.onDialogueEnd.RemoveListener(OnDialogueFinished);
    }
} // DialogueTimelineController 类结束

} // DialogueSystem 命名空间结束