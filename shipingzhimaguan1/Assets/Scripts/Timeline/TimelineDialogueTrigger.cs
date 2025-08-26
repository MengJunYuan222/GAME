using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineDialogueTrigger : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector director;

    [Header("触发设置")]
    public bool repeatable = false; // 是否可重复播放

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!repeatable && hasPlayed)
            return;

        if (director != null)
        {
            director.Play();
            hasPlayed = true;
        }
    }

    // 可选：如果你希望玩家离开后可以再次触发（比如进出多次都能触发）
    private void OnTriggerExit(Collider other)
    {
        // 如果repeatable为true，允许再次触发
        // 如果你希望玩家每次进入都能触发，可以在这里重置hasPlayed
        // 例如：hasPlayed = false;
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
