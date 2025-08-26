using System;
using UnityEngine;

namespace ReputationSystem
{
    public class SimpleReputationManager : MonoBehaviour
    {
        // 单例
        public static SimpleReputationManager Instance { get; private set; }
        
        [Header("声望配置")]
        [SerializeField] private int _currentReputation = 0;
        
        // 声望变化事件
        public event Action<int, int> OnReputationChanged; // 参数：旧值，新值
        
        // 当前声望值属性
        public int CurrentReputation 
        { 
            get { return _currentReputation; }
            private set 
            {
                int oldValue = _currentReputation;
                _currentReputation = value;
                OnReputationChanged?.Invoke(oldValue, _currentReputation);
            }
        }
        
        private void Awake()
        {
            // 单例初始化
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // 增加声望
        public void AddReputation(int amount)
        {
            CurrentReputation += amount;
        }
        
        // 减少声望
        public void SubtractReputation(int amount)
        {
            CurrentReputation -= amount;
        }
        
        // 设置声望
        public void SetReputation(int value)
        {
            CurrentReputation = value;
        }
    }
} 