using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Tooltip("要加载的目标场景名称")]
    [SerializeField] private string targetSceneName;
    
    [Tooltip("场景切换动画控制器")]
    [SerializeField] private Animator transitionAnimator;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 如果有动画控制器，播放动画
            if (transitionAnimator != null)
            {
                transitionAnimator.SetTrigger("Start");
            }
            
            // 加载新场景前销毁不需要保留的物体
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Destroy(player);
            }
            
            // 加载新场景
            SceneManager.LoadScene(targetSceneName);
        }
    }
}