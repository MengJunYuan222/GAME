using UnityEngine;
using Cinemachine;
using UnityEngine.UI;

public class UIVirtualCameraSwitch : MonoBehaviour
{
    [Header("相机设置")]
    [Tooltip("点击时要切换到的虚拟相机")]
    public CinemachineVirtualCameraBase targetVirtualCamera;
    
    [Tooltip("原始相机引用，如果不设置则使用当前活动的相机")]
    public CinemachineVirtualCameraBase originalVirtualCamera;
    
    [Header("退出设置")]
    [Tooltip("退出调查模式的按键")]
    public KeyCode exitKey = KeyCode.Escape;

    private bool isSwitched = false;
    private CinemachineBrain cinemachineBrain;
    private int originalCameraOldPriority;
    private int targetCameraOldPriority;
    
    private void Start()
    {
        // 缓存CinemachineBrain
        if (Camera.main != null)
        {
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        }
        
        // 为所有子对象的Button设置点击事件
        SetupButtonClickEvents();
    }
    
    private void Update()
    {
        // 检测退出键
        if (isSwitched && Input.GetKeyDown(exitKey))
        {
            SwitchToOriginalCamera();
        }
    }
    
    /// <summary>
    /// 为子对象的Button设置点击事件
    /// </summary>
    private void SetupButtonClickEvents()
    {
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked(button));
        }
    }
    
    /// <summary>
    /// Button点击事件处理
    /// </summary>
    private void OnButtonClicked(Button clickedButton)
    {
        // 设置UI已点击状态
        WorldUIVisibility worldUI = GetComponentInParent<WorldUIVisibility>();
        if (worldUI != null)
        {
            worldUI.hasBeenClicked = true;
        }
        
        // 切换到目标相机
        SwitchToTargetCamera();
    }
    
    /// <summary>
    /// 切换到目标虚拟相机
    /// </summary>
    public void SwitchToTargetCamera()
    {
        if (targetVirtualCamera == null || cinemachineBrain == null)
            return;

        // 获取原始相机
        if (originalVirtualCamera == null && cinemachineBrain.ActiveVirtualCamera != null)
        {
            originalVirtualCamera = cinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
        }

        if (originalVirtualCamera == null)
            return;

        // 确保目标相机激活
        if (!targetVirtualCamera.VirtualCameraGameObject.activeInHierarchy)
        {
            targetVirtualCamera.VirtualCameraGameObject.SetActive(true);
        }

        // 切换相机优先级
        targetCameraOldPriority = targetVirtualCamera.Priority;
        originalCameraOldPriority = originalVirtualCamera.Priority;
        
        originalVirtualCamera.Priority = originalCameraOldPriority - 1;
        targetVirtualCamera.Priority = originalCameraOldPriority + 1;
        
        // 进入调查模式
        if (DetectiveMode.Instance != null)
        {
            DetectiveMode.Instance.EnterDetectiveMode();
        }

        isSwitched = true;
    }
    
    /// <summary>
    /// 切换回原始虚拟相机
    /// </summary>
    public void SwitchToOriginalCamera()
    {
        if (originalVirtualCamera == null || targetVirtualCamera == null)
            return;
        
        // 恢复优先级
        targetVirtualCamera.Priority = targetCameraOldPriority;
        originalVirtualCamera.Priority = originalCameraOldPriority;
        
        // 退出调查模式
        if (DetectiveMode.Instance != null)
        {
            DetectiveMode.Instance.ExitDetectiveMode();
        }

        isSwitched = false;
    }
}