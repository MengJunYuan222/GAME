using UnityEngine;
using Cinemachine;

public class CameraShakeManager : MonoBehaviour
{
    private static CameraShakeManager _instance;
    public static CameraShakeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CameraShakeManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CameraShakeManager");
                    _instance = go.AddComponent<CameraShakeManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("震动源")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("预设震动效果")]
    [Tooltip("轻微震动 - 例如轻微强调")]
    [SerializeField] private float lightShakeIntensity = 0.2f;
    [Tooltip("中等震动 - 例如重要信息")]
    [SerializeField] private float mediumShakeIntensity = 0.5f;
    [Tooltip("强烈震动 - 例如重要剧情转折")]
    [SerializeField] private float strongShakeIntensity = 1.0f;
    [Tooltip("逆转裁判效果 - 强烈震动加特效")]
    [SerializeField] private float aceAttorneyShakeIntensity = 1.5f;

    [Header("自定义震动设置")]
    [SerializeField] private float defaultShakeDuration = 0.5f;
    
    // 预定义的动画曲线
    private AnimationCurve defaultAttackCurve;
    private AnimationCurve defaultDecayCurve;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建默认的动画曲线
        CreateDefaultCurves();
        
        // 如果没有设置震动源，则自动添加
        if (impulseSource == null)
        {
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        // 初始化震动源设置
        InitializeImpulseSource();
    }
    
    // 创建默认的动画曲线
    private void CreateDefaultCurves()
    {
        // 创建线性上升的攻击曲线 (0->1)
        defaultAttackCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        
        // 创建线性下降的衰减曲线 (1->0)
        defaultDecayCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, -2f)
        );
    }

    private void InitializeImpulseSource()
    {
        if (impulseSource != null)
        {
            // 设置默认的震动参数
            impulseSource.m_ImpulseDefinition.m_ImpulseDuration = defaultShakeDuration;
            
            // 使用动画曲线而不是浮点值
            impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_AttackShape = defaultAttackCurve;
            impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_DecayShape = defaultDecayCurve;
            impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime = 0.2f;
            
            impulseSource.m_DefaultVelocity = new Vector3(1, 1, 0); // 主要在水平和垂直方向震动
        }
    }

    /// <summary>
    /// 触发轻微震动
    /// </summary>
    public void ShakeLight()
    {
        ShakeCamera(lightShakeIntensity);
    }

    /// <summary>
    /// 触发中等震动
    /// </summary>
    public void ShakeMedium()
    {
        ShakeCamera(mediumShakeIntensity);
    }

    /// <summary>
    /// 触发强烈震动
    /// </summary>
    public void ShakeStrong()
    {
        ShakeCamera(strongShakeIntensity);
    }

    /// <summary>
    /// 触发逆转裁判风格的震动效果
    /// </summary>
    public void ShakeAceAttorney()
    {
        ShakeCamera(aceAttorneyShakeIntensity);
    }

    /// <summary>
    /// 使用自定义参数触发震动
    /// </summary>
    /// <param name="intensity">震动强度</param>
    /// <param name="duration">震动持续时间(可选)</param>
    /// <param name="velocity">震动方向向量(可选)</param>
    public void ShakeCustom(float intensity, float? duration = null, Vector3? velocity = null)
    {
        if (duration.HasValue)
        {
            impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration.Value;
        }

        if (velocity.HasValue)
        {
            impulseSource.m_DefaultVelocity = velocity.Value;
        }

        ShakeCamera(intensity);

        // 重置为默认值
        impulseSource.m_ImpulseDefinition.m_ImpulseDuration = defaultShakeDuration;
        impulseSource.m_DefaultVelocity = new Vector3(1, 1, 0);
    }

    private void ShakeCamera(float intensity)
    {
        if (impulseSource != null)
        {
            // 生成震动
            impulseSource.GenerateImpulse(intensity);
        }
        else
        {
            Debug.LogWarning("CameraShakeManager: 没有设置震动源，无法产生震动效果");
        }
    }
} 