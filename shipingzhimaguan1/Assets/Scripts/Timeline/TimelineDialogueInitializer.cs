using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using DialogueSystem;

public class TimelineDialogueInitializer : MonoBehaviour
{
    [SerializeField] private DialogueTimelineController dialogueController;
    [SerializeField] private PlayableDirector director;
    
    private void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
            
        if (dialogueController == null)
            dialogueController = FindObjectOfType<DialogueTimelineController>();
            
        if (dialogueController != null && director != null)
        {
            // 设置当前Director
            dialogueController.SetCurrentDirector(director);
            
            // 监听Timeline开始播放事件
            director.played += OnDirectorPlayed;
            director.stopped += OnDirectorStopped;
            
            Debug.Log($"Timeline对话初始化完成: {gameObject.name}");
        }
        else
        {
            Debug.LogError("未找到必要的组件: DialogueTimelineController或PlayableDirector");
        }
    }
    
    private void OnDirectorPlayed(PlayableDirector playableDirector)
    {
        if (dialogueController != null)
        {
            dialogueController.SetCurrentDirector(playableDirector);
            Debug.Log($"Timeline开始播放，更新当前Director: {playableDirector.gameObject.name}");
        }
    }
    
    private void OnDirectorStopped(PlayableDirector playableDirector)
    {
        Debug.Log($"Timeline停止播放: {playableDirector.gameObject.name}");
    }
    
    private void OnDestroy()
    {
        if (director != null)
        {
            director.played -= OnDirectorPlayed;
            director.stopped -= OnDirectorStopped;
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
