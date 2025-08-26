using UnityEngine;

public class WorldUIVisibility : MonoBehaviour
{
    [Header("基本设置")]
    [Tooltip("要显示的UI对象")]
    public GameObject uiObject;
    
    [Tooltip("玩家角色的引用")]
    public Transform playerTransform;
    
    [Tooltip("物品和玩家之间的最大可见距离")]
    public float maxVisibleDistance = 5f;
    
    [Tooltip("是否检查视线方向")]
    public bool checkViewDirection = true;
    
    [Tooltip("是否已经被点击过")]
    [HideInInspector]
    public bool hasBeenClicked = false;
    
    [Tooltip("是否在进入视野范围时重置点击状态")]
    public bool resetOnReenter = true;
    
    [Header("Debug设置")]
    [Tooltip("启用距离检测Debug功能")]
    public bool enableDistanceDebug = false;
    
    private Camera mainCamera;
    private bool wasOutOfRange = true;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        // 自动查找玩家
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        if (uiObject == null)
        {
            Debug.LogError("未设置UI对象");
            return;
        }
        
        uiObject.SetActive(false);
        
        // 配置WorldSpace Canvas
        Canvas canvas = uiObject.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = mainCamera;
            }
            
            if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }
    }
    
    private void Update()
    {
        if (uiObject == null || playerTransform == null)
            return;
        
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        bool isInRange = IsInPlayerRange();
        
        // Debug输出
        if (enableDistanceDebug)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            Debug.Log($"[{gameObject.name}] 距离: {distance:F2}m / {maxVisibleDistance:F2}m - 在范围内: {isInRange}");
        }
        
        // 重新进入范围时重置点击状态
        if (isInRange && wasOutOfRange && resetOnReenter)
        {
            hasBeenClicked = false;
        }
        
        wasOutOfRange = !isInRange;
        
        // 显示/隐藏UI
        bool shouldShow = isInRange && !hasBeenClicked;
        if (uiObject.activeSelf != shouldShow)
        {
            uiObject.SetActive(shouldShow);
        }
    }
    
    private bool IsInPlayerRange()
    {
        Vector3 playerToItem = transform.position - playerTransform.position;
        float sqrDistance = playerToItem.sqrMagnitude;
        
        if (sqrDistance > maxVisibleDistance * maxVisibleDistance)
            return false;

        if (!checkViewDirection)
            return true;

        if (mainCamera == null)
            return true;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
        if (viewportPos.z <= 0f)
            return false;
        if (viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
            return false;

        return true;
    }
}