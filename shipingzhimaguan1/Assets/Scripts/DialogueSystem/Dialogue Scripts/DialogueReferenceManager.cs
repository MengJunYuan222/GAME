using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DialogueSystem;

/// <summary>
/// 负责确保对话系统的引用在场景加载时正确建立
/// 每个场景需要放置一个此组件
/// </summary>
public class DialogueReferenceManager : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private string uiSceneName = "Ui";
    [SerializeField] private float reconnectDelay = 0.2f; // 连接延迟，给UI场景足够时间初始化
    [SerializeField] private bool debugMode = false; // 设为false禁用所有调试日志
    
    // Start is called before the first frame update
    void Start()
    {
        // 确保UI场景已加载
        StartCoroutine(EnsureUISceneLoadedAndReconnect());
    }

    private IEnumerator EnsureUISceneLoadedAndReconnect()
    {
        // 检查UI场景是否已加载
        if (!IsSceneLoaded(uiSceneName))
        {
            if (debugMode) Debug.Log($"[DialogueReferenceManager] UI场景 {uiSceneName} 未加载，开始加载...");
            
            // 异步加载UI场景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
            
            // 等待UI场景加载完成
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            if (debugMode) Debug.Log($"[DialogueReferenceManager] UI场景 {uiSceneName} 加载完成");
        }
        
        // 等待短暂延迟，确保UI场景中的对象都已初始化
        yield return new WaitForSeconds(reconnectDelay);
        
        // 重连所有对话组件的引用
        ReconnectDialogueReferences();
    }
    
    /// <summary>
    /// 重新连接所有需要DialogueUIManager引用的组件
    /// </summary>
    private void ReconnectDialogueReferences()
    {
        if (debugMode) Debug.Log("[DialogueReferenceManager] 开始重连对话系统引用...");
        
        // 查找DialogueUIManager实例
        DialogueUIManager dialogueManager = FindObjectOfType<DialogueUIManager>();
        
        if (dialogueManager == null)
        {
            Debug.LogError("[DialogueReferenceManager] 无法找到DialogueUIManager实例！请确保UI场景中存在此组件。");
            return;
        }

        // 查找所有NPCDialogue组件
        var dialogueComponents = FindObjectsOfType<NPCDialogue>();
        int reconnectedCount = 0;
        
        foreach (var comp in dialogueComponents)
        {
            // 设置引用
            comp.SetDialogueUIManager(dialogueManager);
            reconnectedCount++;
        }
        
        if (debugMode) Debug.Log($"[DialogueReferenceManager] 重连完成，已处理 {reconnectedCount} 个对话组件");
    }
    
    /// <summary>
    /// 检查场景是否已加载
    /// </summary>
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName && scene.isLoaded)
                return true;
        }
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
