using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace DialogueSystem
{
    /// <summary>
    /// 所有对话节点类型的基类
    /// </summary>
    public abstract class BaseNode : Node
    {
        // 重命名为baseInput，避免与子类的input字段冲突
        [Input] public BaseNode baseInput;
        
        // 节点ID，用于持久化引用
        [HideInInspector]
        public string nodeGUID;
        
        // 节点位置，用于在编辑器中显示
        [HideInInspector]
        public Vector2 nodePosition;
        
        // 初始化节点
        protected override void Init()
        {
            base.Init();
            
            // 如果没有GUID，生成一个
            if (string.IsNullOrEmpty(nodeGUID))
            {
                nodeGUID = System.Guid.NewGuid().ToString();
            }
            
            // 调用更新节点名称的虚拟方法
            UpdateNodeName();
        }
        
        // 节点处理方法 - 所有节点类型都需要实现
        public abstract void ProcessNode(DialogueUIManager uiManager, DialogueNodeGraph graph);
        
        // 获取节点的下一个节点 - 由子类实现具体逻辑
        public abstract BaseNode GetNextNode();
        
        // 检查节点是否是结束节点
        public virtual bool IsEndNode()
        {
            return false;
        }
        
        // 更新节点名称 - 由子类实现具体逻辑
        public virtual void UpdateNodeName()
        {
            // 基类不做任何处理，由子类重写实现特定命名逻辑
        }
    }
} 