using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GetProp : MonoBehaviour
{
    public static GetProp Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject promptCanvas;
    [SerializeField] private Image propsImage;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float displayTime = 2f;  // 显示时间

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }
    }

    // 显示获取道具提示
    public void ShowGetItemPrompt(string itemName, Sprite itemIcon)
    {
        if (!IsSetupValid()) return;
        
        // 停止之前的隐藏协程
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        // 设置物品图标
        if (itemIcon != null)
        {
            propsImage.sprite = itemIcon;
        }
        
        // 设置提示文本
        promptText.text = $"[获取 {itemName}]";
        
        // 显示提示框
        promptCanvas.SetActive(true);
        
        // 使用协程延迟隐藏
        hideCoroutine = StartCoroutine(HidePromptAfterDelay());
    }

    // 使用协程延迟隐藏提示
    private IEnumerator HidePromptAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        HidePrompt();
        hideCoroutine = null;
    }

    // 隐藏提示框
    private void HidePrompt()
    {
        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }
    }
    
    // 检查必要组件是否已设置
    private bool IsSetupValid()
    {
        return promptCanvas != null && propsImage != null && promptText != null;
    }
}
