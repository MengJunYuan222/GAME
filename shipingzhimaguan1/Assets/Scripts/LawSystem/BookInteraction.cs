using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LawSystem
{
    /// <summary>
    /// 书本交互脚本
    /// 处理点击书本物体打开法律查看器的功能
    /// </summary>
    public class BookInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("交互设置")]
        [Tooltip("是否启用点击交互")]
        public bool enableInteraction = true;
        
        [Header("视觉反馈")]
        [Tooltip("高亮材质")]
        public Material highlightMaterial;
        
        [Tooltip("原始材质")]
        public Material originalMaterial;
        
        [Tooltip("要高亮的渲染器")]
        public Renderer bookRenderer;
        
        [Header("音效设置")]
        [Tooltip("是否播放交互音效")]
        public bool playInteractionSound = true;
        
        // 私有变量
        private Camera mainCamera;
        private bool isMouseOver = false;
        private AudioManager audioManager;
        
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
            
            // 获取书本渲染器
            if (bookRenderer == null)
            {
                bookRenderer = GetComponent<Renderer>();
            }
            
            // 保存原始材质
            if (bookRenderer != null && originalMaterial == null)
            {
                originalMaterial = bookRenderer.material;
            }
            
            // 确保物体有碰撞器用于射线检测
            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[BookInteraction] {gameObject.name} 没有Collider组件，点击检测无法工作！请添加Box Collider、Mesh Collider或其他Collider组件。");
            }
        }
        
        /// <summary>
        /// 鼠标点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[BookInteraction] OnPointerClick 被调用！按钮: {eventData.button}");
            
            if (eventData.button == PointerEventData.InputButton.Left && enableInteraction)
            {
                // 播放交互音效
                PlayInteractionSound();
                
                // 打开法律查看器
                OpenLawViewer();
                
                Debug.Log("[BookInteraction] 点击书本，打开法律查看器");
            }
        }
        
        /// <summary>
        /// 鼠标进入事件
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log("[BookInteraction] OnPointerEnter 被调用！");
            
            if (!enableInteraction) return;
            
            isMouseOver = true;
            UpdateHighlight(true);
            
            Debug.Log("[BookInteraction] 鼠标悬停在书本上");
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
        /// 打开法律查看器
        /// </summary>
        private void OpenLawViewer()
        {
            // 通过单例获取法律查看器管理器
            var lawViewerManager = LawViewerManager.Instance;
            
            if (lawViewerManager == null)
            {
                Debug.LogError("[BookInteraction] 未找到法律查看器管理器！请确保场景中有LawViewerManager组件");
                return;
            }
            
            // 打开法律查看器
            lawViewerManager.OpenLawViewer();
        }
        
        /// <summary>
        /// 更新高亮效果
        /// </summary>
        private void UpdateHighlight(bool highlight)
        {
            if (bookRenderer == null) return;
            
            if (highlight && highlightMaterial != null)
            {
                bookRenderer.material = highlightMaterial;
            }
            else if (!highlight && originalMaterial != null)
            {
                bookRenderer.material = originalMaterial;
            }
        }
        
        /// <summary>
        /// 播放交互音效
        /// </summary>
        private void PlayInteractionSound()
        {
            if (!playInteractionSound || audioManager == null) return;
            
            // audioManager.PlaySFX("BookInteraction");
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
        /// 在场景视图中绘制书本图标
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制一个小书本图标
            Gizmos.color = enableInteraction ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
        }
        
        /// <summary>
        /// 在选中时绘制更明显的标识
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            
            // 安全获取Collider边界，如果没有则使用默认大小
            var collider = GetComponent<Collider>();
            Vector3 size = collider != null ? collider.bounds.size : Vector3.one;
            
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
