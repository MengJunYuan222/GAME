using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class DialogueOptionTimelineController : MonoBehaviour
{
    private static DialogueOptionTimelineController _instance;
    public static DialogueOptionTimelineController Instance => _instance;

    private PlayableDirector currentDirector;
    private System.Action onTimelineComplete;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayOptionTimeline(PlayableAsset timeline, GameObject targetObj, System.Action onComplete = null)
    {
        // 创建临时Director或使用目标对象上的Director
        PlayableDirector director = targetObj.GetComponent<PlayableDirector>();
        if (director == null)
            director = targetObj.AddComponent<PlayableDirector>();

        director.playableAsset = timeline;
        currentDirector = director;
        onTimelineComplete = onComplete;

        // 添加完成事件
        director.stopped -= OnTimelineComplete;
        director.stopped += OnTimelineComplete;

        // 播放Timeline
        director.Play();
    }

    private void OnTimelineComplete(PlayableDirector director)
    {
        director.stopped -= OnTimelineComplete;
        onTimelineComplete?.Invoke();
        currentDirector = null;
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
