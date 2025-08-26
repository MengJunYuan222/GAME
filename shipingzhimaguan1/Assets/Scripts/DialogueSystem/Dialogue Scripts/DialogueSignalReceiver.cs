using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using DialogueSystem; // 添加正确的命名空间引用

public class DialogueSignalReceiver : MonoBehaviour
{
    // 在Inspector中分配对话资源
    public DialogueNodeGraph dialogueGraph;
    
    // 信号接收方法
    public void PlayDialogue()
    {
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.SetDialogueGraph(dialogueGraph);
            dialogueGraph.StartDialogue(DialogueUIManager.Instance);
        }
        else
        {
            Debug.LogError("DialogueUIManager实例不存在！");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
