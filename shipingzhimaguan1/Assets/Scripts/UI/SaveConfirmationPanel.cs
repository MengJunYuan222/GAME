using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SaveConfirmationPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image backgroundPanel;

    [Header("动画设置")]
    [SerializeField] private float fadeInSpeed = 2.0f;
    [SerializeField] private float fadeOutSpeed = 1.0f;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.5f);

    // 初始透明度值
    private float currentAlpha = 0f;
    private bool isFadingIn = false;
    private bool isFadingOut = false;

    private void Awake()
    {
        // 初始化UI组件
        if (messageText == null)
        {
            messageText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (backgroundPanel == null)
        {
            backgroundPanel = GetComponent<Image>();
        }

        // 设置默认消息
        if (messageText != null)
        {
            messageText.text = "游戏已保存";
        }

        // 初始隐藏
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        // 重置状态
        currentAlpha = 0f;
        isFadingIn = true;
        isFadingOut = false;
        SetAlpha(currentAlpha);
    }

    private void Update()
    {
        // 淡入效果
        if (isFadingIn)
        {
            currentAlpha += fadeInSpeed * Time.unscaledDeltaTime;
            if (currentAlpha >= 1f)
            {
                currentAlpha = 1f;
                isFadingIn = false;
            }
            SetAlpha(currentAlpha);
        }
        // 淡出效果
        else if (isFadingOut)
        {
            currentAlpha -= fadeOutSpeed * Time.unscaledDeltaTime;
            if (currentAlpha <= 0f)
            {
                currentAlpha = 0f;
                isFadingOut = false;
                gameObject.SetActive(false); // 完成淡出后隐藏
            }
            SetAlpha(currentAlpha);
        }
    }

    // 设置UI元素透明度
    private void SetAlpha(float alpha)
    {
        if (messageText != null)
        {
            Color textColor = messageText.color;
            textColor.a = alpha;
            messageText.color = textColor;
        }

        if (backgroundPanel != null)
        {
            Color bgColor = backgroundColor;
            bgColor.a = alpha * backgroundColor.a;
            backgroundPanel.color = bgColor;
        }
    }

    // 开始淡出过程
    public void StartFadeOut()
    {
        isFadingIn = false;
        isFadingOut = true;
    }

    // 设置显示的消息
    public void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
} 