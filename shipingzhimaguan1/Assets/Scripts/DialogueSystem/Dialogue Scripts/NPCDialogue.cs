using UnityEngine;
using UnityEngine.Events;
using DialogueSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class NPCDialogue : MonoBehaviour
{
    [Header("对话设置")]
    [SerializeField] private DialogueNodeGraph dialogueGraph;
    [SerializeField] private DialogueUIManager dialogueUIManager;
    [SerializeField] private bool autoFindUIManager = true;
    [Tooltip("如果勾选，该NPC的对话只会触发一次")]
    [SerializeField] private bool isOneTimeDialogue = false;
    
    [Header("交互设置")]
    [Tooltip("直接触发：接触NPC时自动开始对话，提示触发：接触NPC时显示提示，按键触发对话")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.PromptTrigger;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private Transform player;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("直接触发模式下，对话结束后是否需要玩家离开范围再进入才能再次触发")]
    [SerializeField] private bool requireExitAndReenter = true;
    
    [Header("对话提示")]
    [Tooltip("提示预制体，可以在场景中共享使用同一个预制体")]
    [SerializeField] private GameObject interactionPrompt;
    [Tooltip("提示框距离NPC顶部的高度")]
    [SerializeField] private float promptHeight = 2f;
    [Tooltip("提示框位置的偏移量")]
    [SerializeField] private Vector3 promptOffset = Vector3.zero;
    [Tooltip("提示框的旋转角度 (X, Y, Z)")]
    [SerializeField] private Vector3 promptRotation = Vector3.zero;
    [Tooltip("是否使用相对于NPC的局部偏移")]
    [SerializeField] private bool useLocalOffset = false;
    
    [Header("事件")]
    [SerializeField] private UnityEvent onDialogueStart;
    [SerializeField] private UnityEvent onDialogueEnd;
    
    [Header("多层对话设置")]
    [SerializeField] private List<DialogueNodeGraph> dialogueGraphs = new List<DialogueNodeGraph>();
    private int currentDialogueIndex = 0;
    private string dialogueProgressKey => $"NPC_{gameObject.name}_DialogueIndex";
    
    [Header("自动行走设置")]
    public Transform[] waypoints; // 巡逻点
    public float walkSpeed = 2f;
    public float arriveThreshold = 0.2f;
    public bool autoPatrol = false;
    public bool waitAtPoint = true;
    public float waitTime = 2f;
    private float currentWaitTime = 0f;
    private int currentWaypointIndex = 0;
    private Animator animator;
    private bool isWalking = false;
    private float lastMoveX = 0f;
    private float lastMoveY = -1f; // 默认朝下
    
    [Header("调试")]
    [SerializeField] private bool showDebugMessages = false;
    
    [SerializeField] private float dialogueCooldown = 0.5f;
    [SerializeField] private bool debugMode = false;
    
    // 对话触发模式枚举
    public enum TriggerMode
    {
        DirectTrigger,   // 直接触发：接触NPC时自动开始对话
        PromptTrigger    // 提示触发：接触NPC时显示提示，按键触发对话
    }
    
    // 对话状态
    private bool isInRange = false;
    private bool isInDialogue = false;
    private bool canStartDialogue = true;
    private bool hasDialogueEnded = false;
    private float lastDialogueTime = 0f;
    
    // 缓存组件
    private Collider npcCollider;
    
    // 静态共享提示对象
    private static GameObject sharedPrompt;
    private static int activePromptOwnerID = -1;
    
    private void Awake()
    {
        // 确保有碰撞体
        npcCollider = GetComponent<Collider>();
        if (npcCollider != null && !npcCollider.isTrigger)
        {
            Debug.LogWarning($"NPCDialogue: {gameObject.name} 的碰撞体不是触发器，自动设置为触发器");
            npcCollider.isTrigger = true;
        }
        
        // 自动查找对话UI管理器
        if (dialogueUIManager == null && autoFindUIManager)
        {
            dialogueUIManager = FindObjectOfType<DialogueUIManager>();
        }
        
        // 自动查找玩家
        if (player == null && autoFindPlayer)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        // 读取多层对话进度
        currentDialogueIndex = PlayerPrefs.GetInt(dialogueProgressKey, 0);
        
        animator = GetComponent<Animator>();
        
        // 如果有提示预制体但还没有共享提示对象，创建一个
        if (interactionPrompt != null && sharedPrompt == null)
        {
            sharedPrompt = Instantiate(interactionPrompt);
            sharedPrompt.SetActive(false);
            DontDestroyOnLoad(sharedPrompt);
        }
    }
    
    private void Start()
    {
        // 订阅对话结束事件
        if (dialogueUIManager != null)
        {
            dialogueUIManager.OnDialogueEndedEvent += OnDialogueEnded;
        }
        
        // 验证对话图
        if (dialogueGraph == null)
        {
            Debug.LogError($"NPCDialogue: {gameObject.name} 没有设置对话图！");
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅对话结束事件
        if (dialogueUIManager != null)
        {
            dialogueUIManager.OnDialogueEndedEvent -= OnDialogueEnded;
        }
        
        // 如果当前NPC拥有活跃提示，隐藏它
        if (activePromptOwnerID == GetInstanceID())
        {
            HideInteractionPrompt();
        }
        
        // 存档多层对话进度
        PlayerPrefs.SetInt(dialogueProgressKey, currentDialogueIndex);
        PlayerPrefs.Save();
    }
    
    private void Update()
    {
        // 检查玩家是否在范围内
        CheckPlayerInRange();
        
        // 根据触发模式处理交互
        if (isInRange && !isInDialogue && canStartDialogue)
        {
            if (triggerMode == TriggerMode.DirectTrigger)
            {
                // 直接触发模式：进入范围自动开始对话
                // 如果启用了离开再进入的设置，那么对话结束后hasDialogueEnded会设为true，
                // 直到玩家离开再进入范围后OnPlayerEnterRange中会重置这个标志并允许再次对话
                if (!hasDialogueEnded || !requireExitAndReenter)
                {
                    StartDialogue();
                }
            }
            else if (triggerMode == TriggerMode.PromptTrigger && Input.GetKeyDown(interactKey))
            {
                // 提示触发模式：按下交互键开始对话
                StartDialogue();
            }
        }
        
        // 更新交互提示位置
        UpdatePromptPosition();
        
        // 自动巡逻逻辑
        if (autoPatrol && waypoints != null && waypoints.Length > 0 && !isInDialogue)
        {
            Patrol();
        }
    }
    
    // 检查玩家是否在范围内
    private void CheckPlayerInRange()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        bool wasInRange = isInRange;
        isInRange = (distance <= interactDistance);
        
        // 玩家进入范围
        if (isInRange && !wasInRange)
        {
            OnPlayerEnterRange();
        }
        // 玩家离开范围
        else if (!isInRange && wasInRange)
        {
            OnPlayerExitRange();
        }
    }
    
    // 玩家进入范围
    private void OnPlayerEnterRange()
    {
        // 如果是直接触发模式且对话曾经结束过且需要离开再进入
        if (triggerMode == TriggerMode.DirectTrigger && hasDialogueEnded && requireExitAndReenter)
        {
            // 玩家离开再进入范围，重置标志，允许再次对话
            hasDialogueEnded = false;
        }
        
        // 直接触发模式下，不显示提示
        if (triggerMode == TriggerMode.PromptTrigger)
        {
            // 显示交互提示
            ShowInteractionPrompt();
        }
    }
    
    // 玩家离开范围
    private void OnPlayerExitRange()
    {
        // 隐藏交互提示
        HideInteractionPrompt();
        
        // 如果是直接触发模式下的对话，重置状态但不立即允许对话
        // 只有在玩家离开范围再重新进入时才会在OnPlayerEnterRange中重置hasDialogueEnded
        // 同时在离开范围时重新允许对话，这样当玩家下次再进入范围时可以触发对话
        if (triggerMode == TriggerMode.DirectTrigger && !isOneTimeDialogue)
        {
            canStartDialogue = true; 
            // 注意：hasDialogueEnded依然保持为true，直到玩家再次进入范围
        }
    }
    
    // 显示交互提示
    private void ShowInteractionPrompt()
    {
        if (sharedPrompt != null)
        {
            // 计算提示位置和旋转
            Vector3 position = CalculatePromptPosition();
            sharedPrompt.transform.position = position;
            sharedPrompt.transform.rotation = Quaternion.Euler(promptRotation);
            
            // 记录当前拥有提示的NPC ID
            activePromptOwnerID = GetInstanceID();
            
            // 显示提示
            sharedPrompt.SetActive(true);
        }
        else if (interactionPrompt != null)
        {
            // 如果没有共享提示但有预制体，创建一个
            sharedPrompt = Instantiate(interactionPrompt);
            sharedPrompt.transform.position = CalculatePromptPosition();
            sharedPrompt.transform.rotation = Quaternion.Euler(promptRotation);
            activePromptOwnerID = GetInstanceID();
            sharedPrompt.SetActive(true);
            DontDestroyOnLoad(sharedPrompt);
        }
    }
    
    // 隐藏交互提示
    private void HideInteractionPrompt()
    {
        // 只有当前NPC拥有提示时才隐藏
        if (sharedPrompt != null && activePromptOwnerID == GetInstanceID())
        {
            sharedPrompt.SetActive(false);
            activePromptOwnerID = -1;
        }
    }
    
    // 计算提示位置
    private Vector3 CalculatePromptPosition()
    {
        // 基础位置：NPC位置 + 高度偏移
        Vector3 basePosition = transform.position + Vector3.up * promptHeight;
        
        // 应用额外偏移
        Vector3 finalOffset = promptOffset;
        if (useLocalOffset)
        {
            // 使用NPC的局部坐标系计算偏移
            finalOffset = transform.TransformDirection(promptOffset);
        }
        
        // 最终位置
        return basePosition + finalOffset;
    }
    
    // 更新交互提示位置
    private void UpdatePromptPosition()
    {
        // 只有当前NPC拥有提示且提示处于激活状态时才更新位置
        if (sharedPrompt != null && sharedPrompt.activeSelf && activePromptOwnerID == GetInstanceID())
        {
            sharedPrompt.transform.position = CalculatePromptPosition();
        }
    }
    
    // 开始对话
    public void StartDialogue()
    {
        // 如果没有引用，尝试通过单例获取
        if (dialogueUIManager == null)
        {
            dialogueUIManager = DialogueUIManager.Instance;
            
            if (dialogueUIManager == null)
            {
                Debug.LogError($"[NPCDialogue] {gameObject.name} 无法找到DialogueUIManager！对话无法启动。");
                return;
            }
        }
        
        // 获取当前可用的对话图
        DialogueNodeGraph currentGraph = GetCurrentAvailableDialogueGraph();
        
        if (currentGraph == null)
        {
            Debug.LogError($"[NPCDialogue] {gameObject.name} 没有可用的对话图！对话无法启动。");
            return;
        }
        
        // 添加对话结束事件监听
        dialogueUIManager.OnDialogueEndedEvent -= OnDialogueEnded;  // 先移除以防重复
        dialogueUIManager.OnDialogueEndedEvent += OnDialogueEnded;
        
        // 设置对话状态
        isInDialogue = true;
        
        // 使用对话管理器启动对话
        dialogueUIManager.StartDialogue(currentGraph);
    }
    
    private DialogueNodeGraph GetCurrentAvailableDialogueGraph()
    {
        // 优先用多层对话
        if (dialogueGraphs != null && dialogueGraphs.Count > 0)
        {
            for (int i = currentDialogueIndex; i < dialogueGraphs.Count; i++)
            {
                var graph = dialogueGraphs[i];
                if (graph == null) continue;
                if (graph.startNode is DialogueNode node && node.isOneTimeDialogue)
                {
                    if (!graph.IsDialogueCompleted(node))
                    {
                        currentDialogueIndex = i;
                        return graph;
                    }
                }
                else
                {
                    currentDialogueIndex = i;
                    return graph;
                }
            }
            // 没有可用对话
            return null;
        }
        // 兼容旧用法
        if (dialogueGraph != null)
        {
            return dialogueGraph;
        }
        return null;
    }
    
    // 处理对话结束事件
    private void OnDialogueEnded()
    {
        // 移除事件监听
        if (dialogueUIManager != null)
        {
            dialogueUIManager.OnDialogueEndedEvent -= OnDialogueEnded;
        }
        
        isInDialogue = false;
        
        // 多层对话推进
        if (dialogueGraphs != null && currentDialogueIndex < dialogueGraphs.Count)
        {
            var graph = dialogueGraphs[currentDialogueIndex];
            if (graph != null && graph.startNode is DialogueNode node && node.isOneTimeDialogue && graph.IsDialogueCompleted(node))
            {
                currentDialogueIndex++;
                PlayerPrefs.SetInt(dialogueProgressKey, currentDialogueIndex);
                PlayerPrefs.Save();
            }
        }
        
        // 如果是一次性对话，不再允许对话
        if (isOneTimeDialogue)
        {
            canStartDialogue = false;
        }
        else
        {
            // 如果是直接触发模式且需要玩家离开再进入，设置标志
            if (triggerMode == TriggerMode.DirectTrigger && requireExitAndReenter)
            {
                hasDialogueEnded = true;
                // 暂时禁用对话触发，直到玩家离开再进入
                canStartDialogue = false;
            }
            else
            {
                // 延迟允许再次对话，防止对话立即重新触发
                Invoke("AllowDialogueAgain", 0.1f);
            }
        }
        
        // 触发对话结束事件
        onDialogueEnd?.Invoke();
    }
    
    // 允许再次开始对话
    private void AllowDialogueAgain()
    {
        canStartDialogue = true;
    }
    
    // 绘制Gizmos
    private void OnDrawGizmosSelected()
    {
        // 绘制交互范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
        
        // 绘制提示位置
        // 计算完整的提示位置
        Vector3 basePosition = transform.position + Vector3.up * promptHeight;
        Vector3 finalOffset = useLocalOffset ? transform.TransformDirection(promptOffset) : promptOffset;
        Vector3 finalPosition = basePosition + finalOffset;
        
        // 绘制从NPC到提示框的线
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, finalPosition);
        Gizmos.DrawSphere(finalPosition, 0.1f);
        
        // 绘制旋转方向（前方向）
        Quaternion rotation = Quaternion.Euler(promptRotation);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(finalPosition, rotation * Vector3.forward * 0.5f);
    }
    
    // 手动触发对话（可以通过其他脚本调用）
    public void TriggerDialogue()
    {
        // 检查冷却和状态
        if (isInDialogue || Time.time < lastDialogueTime + dialogueCooldown)
        {
            LogDebug("对话冷却中或已在对话状态，忽略触发");
            return;
        }
        
        // 检查全局对话状态
        if (DialogueInputManager.Instance != null && 
            DialogueInputManager.Instance.IsGlobalDialogueActive)
        {
            LogDebug("全局对话已经活跃，忽略新对话请求");
            return;
        }
        
        // 设置状态
        isInDialogue = true;
        lastDialogueTime = Time.time;
        
        // 获取对话管理器
        DialogueUIManager manager = DialogueUIManager.Instance;
        if (manager == null)
        {
            LogError("找不到DialogueUIManager实例");
            isInDialogue = false;
            return;
        }
        
        // 获取当前可用的对话图
        DialogueNodeGraph currentGraph = GetCurrentAvailableDialogueGraph();
        
        if (currentGraph == null)
        {
            LogError("没有可用的对话图！对话无法启动。");
            isInDialogue = false;
            return;
        }
        
        // 开始对话
        LogDebug($"开始对话: {currentGraph.name}");
        manager.StartDialogue(currentGraph);
        
        // 订阅对话结束事件
        manager.OnDialogueEndedEvent -= OnDialogueEnded; // 防止重复订阅
        manager.OnDialogueEndedEvent += OnDialogueEnded;
    }
    
    // 设置对话图（可以动态更改对话）
    public void SetDialogueGraph(DialogueNodeGraph newGraph)
    {
        dialogueGraph = newGraph;
    }
    
    // 设置触发模式（可以在运行时动态更改）
    public void SetTriggerMode(TriggerMode mode)
    {
        triggerMode = mode;
    }
    
    // 新增方法：允许外部设置对话UI管理器的引用
    public void SetDialogueUIManager(DialogueUIManager manager)
    {
        // 只有当当前引用为空时才设置新引用，避免覆盖已手动设置的引用
        if (dialogueUIManager == null)
        {
            dialogueUIManager = manager;
            
            if (showDebugMessages)
                Debug.Log($"[NPCDialogue] {gameObject.name} 已连接到DialogueUIManager");
        }
    }
    
    private void Patrol()
    {
        if (waypoints.Length == 0) return;
        Transform target = waypoints[currentWaypointIndex];
        Vector3 direction = (target.position - transform.position);
        direction.y = 0; // 保持水平移动

        if (direction.magnitude > arriveThreshold)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * walkSpeed * Time.deltaTime;
            // if (moveDir != Vector3.zero)
            //     transform.forward = moveDir;

            // 设置动画参数
            SetWalking(true);
            SetMoveDirection(moveDir);

            currentWaitTime = 0f;
        }
        else
        {
            SetWalking(false);
            SetMoveDirection(Vector3.zero);
            if (waitAtPoint)
            {
                currentWaitTime += Time.deltaTime;
                if (currentWaitTime >= waitTime)
                {
                    currentWaitTime = 0f;
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                }
            }
            else
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }
    }

    private void SetWalking(bool walking)
    {
        if (animator != null && isWalking != walking)
        {
            animator.SetBool("isWalking", walking);
            isWalking = walking;
        }
    }

    private void SetMoveDirection(Vector3 moveDir)
    {
        if (animator != null)
        {
            // 将世界方向转换为本地方向（适配3D场景2D角色，X-Z平面）
            Vector3 localDir = transform.InverseTransformDirection(moveDir);

            animator.SetFloat("moveX", localDir.x);
            animator.SetFloat("moveY", localDir.z);

            // 记录最后移动方向（用于Idle朝向）
            if (moveDir != Vector3.zero)
            {
                lastMoveX = localDir.x;
                lastMoveY = localDir.z;
                animator.SetFloat("lastMoveX", lastMoveX);
                animator.SetFloat("lastMoveY", lastMoveY);
            }
        }
    }
    
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[NPCDialogue] {gameObject.name}: {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[NPCDialogue] {gameObject.name}: {message}");
    }
}
