using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class TimelineManager : MonoBehaviour
{
    private static TimelineManager _instance;
    public static TimelineManager Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<TimelineManager>();
                if (_instance == null) {
                    GameObject go = new GameObject("TimelineManager");
                    _instance = go.AddComponent<TimelineManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    // 当前活动的Timeline Director
    private PlayableDirector _currentPlayingDirector;
    public PlayableDirector CurrentPlayingDirector => _currentPlayingDirector;
    
    // 所有被监控的Directors
    private List<PlayableDirector> _activeDirectors = new List<PlayableDirector>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void RegisterDirector(PlayableDirector director)
    {
        if (!_activeDirectors.Contains(director)) {
            _activeDirectors.Add(director);
            
            // 添加播放/暂停/停止事件监听
            director.played += OnDirectorPlayed;
            director.paused += OnDirectorPaused;
            director.stopped += OnDirectorStopped;
            
            Debug.Log($"注册Timeline Director: {director.gameObject.name}");
        }
    }
    
    public void UnregisterDirector(PlayableDirector director)
    {
        if (_activeDirectors.Contains(director)) {
            _activeDirectors.Remove(director);
            
            // 移除事件监听
            director.played -= OnDirectorPlayed;
            director.paused -= OnDirectorPaused;
            director.stopped -= OnDirectorStopped;
        }
    }
    
    private void OnDirectorPlayed(PlayableDirector director)
    {
        _currentPlayingDirector = director;
        Debug.Log($"Timeline开始播放: {director.gameObject.name}");
    }
    
    private void OnDirectorPaused(PlayableDirector director)
    {
        // 暂停时保持当前引用不变
        Debug.Log($"Timeline暂停: {director.gameObject.name}");
    }
    
    private void OnDirectorStopped(PlayableDirector director)
    {
        if (_currentPlayingDirector == director) {
            _currentPlayingDirector = null;
        }
        Debug.Log($"Timeline停止: {director.gameObject.name}");
    }
}
