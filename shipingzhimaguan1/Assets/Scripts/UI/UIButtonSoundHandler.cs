using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI按钮音效处理器：监听UI事件系统中的按钮点击、切换等事件，并播放相应的音效
/// </summary>
public class UIButtonSoundHandler : MonoBehaviour
{
    private UIManager uiManager;
    private EventSystem eventSystem;
    private GameObject lastSelected = null;
    private float soundCooldown = 0.1f;
    private float lastSoundTime = 0f;

    private void Start()
    {
        // 获取UIManager引用
        uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogWarning("[UIButtonSoundHandler] 无法找到UIManager实例，音效处理可能无法正常工作");
        }

        // 获取EventSystem引用
        eventSystem = GetComponent<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[UIButtonSoundHandler] 无法获取EventSystem组件");
        }
    }

    private void Update()
    {
        if (eventSystem == null || uiManager == null) return;

        // 检测按钮点击
        if (Input.GetMouseButtonDown(0) && eventSystem.currentSelectedGameObject != null)
        {
            GameObject selected = eventSystem.currentSelectedGameObject;

            // 检查是否点击了按钮
            Button button = selected.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                uiManager.PlayButtonClickSound();
                return;
            }

            // 检查是否点击了开关
            Toggle toggle = selected.GetComponent<Toggle>();
            if (toggle != null && toggle.interactable)
            {
                uiManager.PlayToggleSound();
                return;
            }
        }

        // 检测滑块拖动
        if (Input.GetMouseButton(0) && eventSystem.currentSelectedGameObject != null)
        {
            GameObject selected = eventSystem.currentSelectedGameObject;
            
            // 检查是否拖动了滑块
            Slider slider = selected.GetComponent<Slider>();
            if (slider != null && slider.interactable)
            {
                // 避免过于频繁地播放音效
                if (Time.unscaledTime - lastSoundTime >= soundCooldown)
                {
                    uiManager.PlaySliderSound();
                    lastSoundTime = Time.unscaledTime;
                }
            }
        }

        // 检测选择变化（用于导航音效）
        if (eventSystem.currentSelectedGameObject != lastSelected && eventSystem.currentSelectedGameObject != null)
        {
            lastSelected = eventSystem.currentSelectedGameObject;
            
            // 如果是通过键盘/控制器导航选择的UI元素，播放导航音效
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButton(0))
            {
                // 避免过于频繁地播放音效
                if (Time.unscaledTime - lastSoundTime >= soundCooldown)
                {
                    uiManager.PlayUISound();
                    lastSoundTime = Time.unscaledTime;
                }
            }
        }
    }
} 