using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class TimelineInitializer : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        PlayableDirector director = GetComponent<PlayableDirector>();
        
        // 注册到管理器
        if (director != null) {
            TimelineManager.Instance.RegisterDirector(director);
        }
    }

    private void OnDestroy()
    {
        PlayableDirector director = GetComponent<PlayableDirector>();
        
        // 从管理器中移除
        if (director != null) {
            TimelineManager.Instance.UnregisterDirector(director);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
