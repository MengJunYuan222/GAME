using UnityEngine;
using UnityEngine.EventSystems;

namespace LawSystem
{
    /// <summary>
    /// 法庭交互调试器
    /// 帮助诊断CourtInteraction点击问题
    /// </summary>
    public class CourtInteractionDebugger : MonoBehaviour
    {
        [Header("调试设置")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showGizmos = true;
        
        private void Start()
        {
            CheckRequiredComponents();
        }
        
        private void Update()
        {
            if (enableDebugLogs)
            {
                CheckMouseInput();
            }
        }
        
        /// <summary>
        /// 检查必需的组件
        /// </summary>
        private void CheckRequiredComponents()
        {
            Debug.Log("=== CourtInteraction 诊断开始 ===");
            
            // 检查EventSystem
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogError("[调试] 场景中没有找到EventSystem！请添加EventSystem。");
                Debug.LogError("[解决方案] 右键Hierarchy → UI → Event System");
            }
            else
            {
                Debug.Log("[调试] ✓ EventSystem 已找到");
            }
            
            // 检查摄像机
            var camera = Camera.main;
            if (camera == null)
            {
                camera = FindObjectOfType<Camera>();
            }
            
            if (camera == null)
            {
                Debug.LogError("[调试] 没有找到摄像机！");
            }
            else
            {
                Debug.Log($"[调试] ✓ 摄像机已找到: {camera.name}");
                
                // 检查摄像机是否有Physics Raycaster
                var raycaster = camera.GetComponent<PhysicsRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogError($"[调试] 摄像机 {camera.name} 没有PhysicsRaycaster组件！");
                    Debug.LogError("[解决方案] 在摄像机上添加PhysicsRaycaster组件");
                }
                else
                {
                    Debug.Log("[调试] ✓ PhysicsRaycaster 已找到");
                }
            }
            
            // 检查CourtInteraction脚本
            var courtInteraction = GetComponent<CourtInteraction>();
            if (courtInteraction == null)
            {
                Debug.LogError("[调试] 没有找到CourtInteraction脚本！");
            }
            else
            {
                Debug.Log("[调试] ✓ CourtInteraction 脚本已找到");
                Debug.Log($"[调试] - enableInteraction: {courtInteraction.enableInteraction}");
            }
            
            // 检查Collider
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError("[调试] 没有找到Collider组件！");
            }
            else
            {
                Debug.Log($"[调试] ✓ Collider 已找到: {collider.GetType().Name}");
                Debug.Log($"[调试] - Collider enabled: {collider.enabled}");
                Debug.Log($"[调试] - Collider isTrigger: {collider.isTrigger}");
            }
            
            Debug.Log("=== CourtInteraction 诊断结束 ===");
        }
        
        /// <summary>
        /// 检查鼠标输入
        /// </summary>
        private void CheckMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("[调试] 检测到鼠标左键点击");
                
                var camera = Camera.main ?? FindObjectOfType<Camera>();
                if (camera != null)
                {
                    Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                    Debug.Log($"[调试] 射线起点: {ray.origin}, 方向: {ray.direction}");
                    
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        Debug.Log($"[调试] 射线击中: {hit.transform.name}");
                        
                        if (hit.transform == transform)
                        {
                            Debug.Log("[调试] ✓ 射线击中了当前物体！");
                        }
                        else
                        {
                            Debug.Log("[调试] ✗ 射线击中了其他物体");
                        }
                    }
                    else
                    {
                        Debug.Log("[调试] ✗ 射线没有击中任何物体");
                    }
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // 绘制调试信息
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, collider.bounds.size);
            }
        }
    }
}
