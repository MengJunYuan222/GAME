using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraShakeListener : MonoBehaviour
{
    private CinemachineImpulseListener impulseListener;
    private CinemachineVirtualCamera virtualCamera;

    [Header("震动接收设置")]
    [Tooltip("震动响应强度倍数")]
    [SerializeField] private float responseMultiplier = 1.0f;
    [Tooltip("是否使用2D震动 (忽略Z轴)")]
    [SerializeField] private bool use2DResponse = true;

    private void Awake()
    {
        // 获取虚拟摄像机组件
        virtualCamera = GetComponent<CinemachineVirtualCamera>();

        // 获取或添加震动监听器组件
        impulseListener = GetComponent<CinemachineImpulseListener>();
        if (impulseListener == null)
        {
            impulseListener = gameObject.AddComponent<CinemachineImpulseListener>();
        }

        // 设置震动响应参数
        ConfigureImpulseListener();
    }

    private void ConfigureImpulseListener()
    {
        if (impulseListener != null)
        {
            // 设置响应通道 (默认0通道)
            impulseListener.m_ChannelMask = 1;

            // 设置响应强度
            impulseListener.m_Gain = responseMultiplier;

            // 注意：在Cinemachine 2.10.3中，我们不能直接设置m_UseImpulseScroll和m_Amplitude
            // 我们将通过运行时的修改来模拟2D震动效果
            if (use2DResponse)
            {
                // 启用后处理来修改震动响应
                StartCoroutine(Apply2DConstraint());
            }
        }
    }

    // 持续应用2D约束，限制Z轴震动
    private System.Collections.IEnumerator Apply2DConstraint()
    {
        while (this.enabled && impulseListener != null)
        {
            // 在每一帧中捕获当前震动位置
            Vector3 shakePos = transform.localPosition;
            
            if (use2DResponse)
            {
                // 保持Z轴不变，只允许XY平面震动
                shakePos.z = 0;
                transform.localPosition = shakePos;
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// 设置震动响应强度
    /// </summary>
    /// <param name="multiplier">强度倍数</param>
    public void SetResponseMultiplier(float multiplier)
    {
        responseMultiplier = multiplier;
        if (impulseListener != null)
        {
            impulseListener.m_Gain = responseMultiplier;
        }
    }
} 