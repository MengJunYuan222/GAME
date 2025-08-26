using UnityEngine;
using UnityEngine.EventSystems;

namespace LawSystem
{
    /// <summary>
    /// 法庭物体交互脚本
    /// 处理点击法庭物体打开判刑界面的功能
    /// </summary>
    public class CourtInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("交互设置")]
        [Tooltip("是否启用点击交互")]
        public bool enableInteraction = true;
        
        [Header("嫌疑人数据")]
        [Tooltip("要审判的嫌疑人数据")]
        public SuspectSO suspectToJudge;
        
        [Header("视觉反馈")]
        [Tooltip("高亮材质")]
        public Material highlightMaterial;
        
        [Tooltip("原始材质")]
        public Material originalMaterial;
        
        [Tooltip("要高亮的渲染器")]
        public Renderer courtRenderer;
        
        [Header("音效设置")]
        [Tooltip("是否播放交互音效")]
        public bool playInteractionSound = true;
        
        // 私有变量
        private Camera mainCamera;
        private bool isMouseOver = false;
        private AudioManager audioManager;
        
        private void Awake()
        {
            // 获取渲染器组件
            if (courtRenderer == null)
            {
                courtRenderer = GetComponent<Renderer>();
            }
            
            // 保存原始材质
            if (courtRenderer != null && originalMaterial == null)
            {
                originalMaterial = courtRenderer.material;
            }
        }
        
        private void Start()
        {
            InitializeComponents();
        }
        
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 获取主摄像机
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            // 获取音频管理器
            audioManager = AudioManager.Instance;
            
            // 确保物体有碰撞器用于点击检测
            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[CourtInteraction] {gameObject.name} 没有Collider组件，点击检测无法工作！请添加Box Collider、Mesh Collider或其他Collider组件。");
            }
        }
        
        /// <summary>
        /// 鼠标点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[CourtInteraction] OnPointerClick 被调用！按钮: {eventData.button}");
            
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InteractWithCourt();
            }
        }
        
        /// <summary>
        /// 鼠标进入事件
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log("[CourtInteraction] OnPointerEnter 被调用！");
            
            if (!enableInteraction) return;
            
            isMouseOver = true;
            UpdateHighlight(true);
            
            Debug.Log("[CourtInteraction] 鼠标悬停在法庭物体上");
        }
        
        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            isMouseOver = false;
            UpdateHighlight(false);
        }
        
        /// <summary>
        /// 与法庭物体交互
        /// </summary>
        private void InteractWithCourt()
        {
            if (!enableInteraction) return;
            
            // 播放交互音效
            PlayInteractionSound();
            
            // 打开判刑界面
            OpenSentencingPanel();
            
            Debug.Log("[CourtInteraction] 点击法庭物体，打开判刑界面");
        }
        
        /// <summary>
        /// 打开判刑界面
        /// </summary>
        private void OpenSentencingPanel()
        {
            // 通过单例获取判刑管理器
            var sentencingManager = SentencingManager.Instance;
            
            if (sentencingManager == null)
            {
                Debug.LogError("[CourtInteraction] 未找到判刑管理器！请确保场景中有SentencingManager组件");
                return;
            }
            
            // 打开判刑界面，传入嫌疑人数据
            sentencingManager.OpenSentencing(suspectToJudge);
        }
        
        /// <summary>
        /// 更新高亮效果
        /// </summary>
        private void UpdateHighlight(bool highlight)
        {
            if (courtRenderer == null) return;
            
            if (highlight && highlightMaterial != null)
            {
                courtRenderer.material = highlightMaterial;
            }
            else if (!highlight && originalMaterial != null)
            {
                courtRenderer.material = originalMaterial;
            }
        }
        
        /// <summary>
        /// 播放交互音效
        /// </summary>
        private void PlayInteractionSound()
        {
            if (!playInteractionSound || audioManager == null) return;
            
            // audioManager.PlaySFX("CourtInteraction");
        }
        
        /// <summary>
        /// 设置要审判的嫌疑人
        /// </summary>
        /// <param name="suspect">嫌疑人数据</param>
        public void SetSuspectToJudge(SuspectSO suspect)
        {
            suspectToJudge = suspect;
        }
        
        /// <summary>
        /// 启用/禁用交互
        /// </summary>
        public void SetInteractionEnabled(bool enabled)
        {
            enableInteraction = enabled;
            
            // 如果禁用交互时鼠标正在悬停，取消高亮
            if (!enabled && isMouseOver)
            {
                OnPointerExit(null);
            }
        }
        
        /// <summary>
        /// 在场景视图中绘制法庭图标
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制一个法庭图标
            Gizmos.color = enableInteraction ? Color.blue : Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
        }
        
        /// <summary>
        /// 在选中时绘制更明显的标识
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            
            // 安全获取Collider边界，如果没有则使用默认大小
            var collider = GetComponent<Collider>();
            Vector3 size = collider != null ? collider.bounds.size : Vector3.one;
            
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
