using UnityEngine;
using Cinemachine;

/// <summary>
/// 调查模式管理器 - 简单的单例模式，用于判断当前是否处于调查模式
/// </summary>
public class DetectiveMode : MonoBehaviour
{
    // 单例实例
    public static DetectiveMode Instance { get; private set; }
    
    // 当前是否处于调查模式
    private bool _isInDetectiveMode = false;
    public bool IsInDetectiveMode => _isInDetectiveMode;
    
    private void Awake()
    {
        // 设置单例
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
    
    /// <summary>
    /// 进入调查模式
    /// </summary>
    public void EnterDetectiveMode()
    {
        _isInDetectiveMode = true;
        Debug.Log("进入调查模式");
    }
    
    /// <summary>
    /// 退出调查模式
    /// </summary>
    public void ExitDetectiveMode()
    {
        _isInDetectiveMode = false;
        Debug.Log("退出调查模式");
    }
}
