using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoBackgroundController : MonoBehaviour
{
    [Header("视频设置")]
    [SerializeField] private RawImage targetImage;
    [SerializeField] private bool loopVideo = true;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool muteAudio = false;
    [SerializeField][Range(0f, 1f)] private float videoVolume = 0.5f;
    
    [Header("视频资源")]
    [SerializeField] private VideoClip videoClip;
    [Tooltip("如果填入多个视频，将随机选择一个播放")]
    [SerializeField] private VideoClip[] alternateVideos;
    
    [Header("渐变效果")]
    [SerializeField] private bool useFadeEffect = true;
    [SerializeField] private float fadeInDuration = 1.5f;
    
    // 组件引用
    private VideoPlayer videoPlayer;
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        // 获取VideoPlayer组件
        videoPlayer = GetComponent<VideoPlayer>();
        
        // 获取或添加CanvasGroup组件
        if (targetImage != null)
        {
            canvasGroup = targetImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null && useFadeEffect)
            {
                canvasGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 配置VideoPlayer
        SetupVideoPlayer();
    }
    
    private void Start()
    {
        // 如果使用淡入效果，初始设置为透明
        if (useFadeEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // 选择视频
        SelectVideo();
        
        // 如果设置为自动播放
        if (playOnAwake)
        {
            PlayVideo();
        }
    }
    
    // 配置VideoPlayer组件
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;
        
        // 基本设置
        videoPlayer.playOnAwake = false;  // 手动控制播放
        videoPlayer.isLooping = loopVideo;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        
        // 静音设置
        videoPlayer.SetDirectAudioMute(0, muteAudio);
        videoPlayer.SetDirectAudioVolume(0, videoVolume);
        
        // 如果有目标RawImage，设置渲染目标
        if (targetImage != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetCameraAlpha = 1f;
            
            // 创建RT并分配给RawImage
            RenderTexture renderTexture = new RenderTexture(1920, 1080, 24);
            videoPlayer.targetTexture = renderTexture;
            targetImage.texture = renderTexture;
        }
        else
        {
            Debug.LogWarning("未设置目标RawImage，视频将不会显示");
        }
        
        // 添加视频准备完成事件
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }
    
    // 随机选择一个视频播放
    private void SelectVideo()
    {
        if (videoPlayer == null) return;
        
        // 如果有多个备选视频并且至少有一个视频
        if (alternateVideos != null && alternateVideos.Length > 0)
        {
            // 添加主视频到候选列表
            VideoClip[] allVideos = new VideoClip[alternateVideos.Length + (videoClip != null ? 1 : 0)];
            
            if (videoClip != null)
            {
                allVideos[0] = videoClip;
                for (int i = 0; i < alternateVideos.Length; i++)
                {
                    allVideos[i + 1] = alternateVideos[i];
                }
            }
            else
            {
                allVideos = alternateVideos;
            }
            
            // 随机选择一个视频
            int randomIndex = Random.Range(0, allVideos.Length);
            VideoClip selectedVideo = allVideos[randomIndex];
            
            if (selectedVideo != null)
            {
                videoPlayer.clip = selectedVideo;
            }
        }
        else if (videoClip != null)
        {
            // 使用主视频
            videoPlayer.clip = videoClip;
        }
    }
    
    // 播放视频
    public void PlayVideo()
    {
        if (videoPlayer == null || videoPlayer.clip == null) return;
        
        // 准备视频
        videoPlayer.Prepare();
    }
    
    // 视频准备完成回调
    private void OnVideoPrepared(VideoPlayer player)
    {
        // 开始播放视频
        player.Play();
        
        // 如果启用淡入效果
        if (useFadeEffect && canvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
    }
    
    // 淡入效果
    private System.Collections.IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    // 设置音量
    public void SetVolume(float volume)
    {
        if (videoPlayer != null)
        {
            videoVolume = Mathf.Clamp01(volume);
            videoPlayer.SetDirectAudioVolume(0, videoVolume);
        }
    }
    
    // 切换静音状态
    public void ToggleMute()
    {
        if (videoPlayer != null)
        {
            muteAudio = !muteAudio;
            videoPlayer.SetDirectAudioMute(0, muteAudio);
        }
    }
    
    private void OnDestroy()
    {
        // 取消事件订阅
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
        
        // 释放RenderTexture
        if (videoPlayer != null && videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture.Release();
        }
    }
} 