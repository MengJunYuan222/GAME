using UnityEngine;
using UnityEngine.Video; // 如果你使用的是VideoPlayer
using UnityEngine.UI; // 如果你使用的是RawImage
using TMPro; // 添加 TextMeshPro 的命名空间
using UnityEngine.SceneManagement; // 添加场景管理的命名空间

public class SkipAnimation : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject animationPanel;  // AnimationPanel的引用
    public RawImage videoDisplay; // 你的RawImage组件
    public TMP_Text skipPrompt; // 将 Text 改为 TMP_Text
    public float delayBeforeShowingPrompt = 5f; // 延迟显示提示的时间
    [SerializeField] private string nextSceneName; // 添加下一个场景的名称变量

    private VideoPlayer videoPlayer; // 如果你使用的是VideoPlayer
    private bool isPlaying = true;

    void Start()
    {
        // 获取VideoPlayer组件（如果你使用的是VideoPlayer）
        videoPlayer = videoDisplay.GetComponentInParent<VideoPlayer>();

        // 初始隐藏提示
        if (skipPrompt != null)
        {
            skipPrompt.gameObject.SetActive(false);
        }

        // 延迟显示提示
        Invoke("ShowSkipPrompt", delayBeforeShowingPrompt);
    }

    void Update()
    {
        // 检测ESC键或鼠标点击是否触发
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)) && isPlaying)
        {
            SkipVideo();
        }
    }

    void ShowSkipPrompt()
    {
        // 显示提示
        if (skipPrompt != null)
        {
            skipPrompt.gameObject.SetActive(true);
        }
    }

    void SkipVideo()
    {
        // 停止视频播放
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        // 隐藏提示
        if (skipPrompt != null)
        {
            skipPrompt.gameObject.SetActive(false);
        }

        // 隐藏动画面板
        if (animationPanel != null)
        {
            animationPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("AnimationPanel未设置！请在Inspector中设置引用。");
        }

        // 设置状态为不再播放
        isPlaying = false;

        // 加载下一个场景
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // 检查场景是否存在于构建设置中
            if (IsSceneInBuildSettings(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogError($"场景 '{nextSceneName}' 未添加到构建设置中！请在 File -> Build Settings 中添加该场景。");
            }
        }
        else
        {
            Debug.LogWarning("下一个场景名称未设置！");
        }
    }

    // 检查场景是否存在于构建设置中
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}