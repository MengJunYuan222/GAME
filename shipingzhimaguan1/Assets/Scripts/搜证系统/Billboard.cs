using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Tooltip("UI相对于物体的偏移位置")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    
    [Tooltip("是否仅绕Y轴朝向相机")]
    public bool rotateOnlyYAxis = true;
    
    private Camera mainCamera;
    private Vector3 originalLocalPosition;
    
    void Start()
    {
        mainCamera = Camera.main;
        originalLocalPosition = transform.localPosition;
    }
    
    void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }
        
        // 应用偏移
        transform.localPosition = originalLocalPosition + offset;
        
        // 让UI面向摄像机（修复文字翻转问题）
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        if (rotateOnlyYAxis)
        {
            directionToCamera.y = 0f;
        }
        
        if (directionToCamera.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }
}