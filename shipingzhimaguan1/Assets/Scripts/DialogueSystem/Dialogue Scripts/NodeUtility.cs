using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace DialogueSystem
{
    /// <summary>
    /// 提供节点相关的辅助功能
    /// </summary>
    public static class NodeUtility
    {
        /// <summary>
        /// 检查并确保节点名称在同一图中不重复，如果重复则添加后缀1，2，3...
        /// </summary>
        /// <param name="graph">所属的对话图</param>
        /// <param name="node">当前节点</param>
        /// <param name="baseName">基础名称</param>
        /// <returns>确保不重复的节点名称</returns>
        public static string EnsureUniqueName(NodeGraph graph, Node node, string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
                return "未命名节点";

            // 如果节点不在任何图中，或者图中没有其他节点，直接返回基础名称
            if (graph == null || graph.nodes == null || graph.nodes.Count <= 1)
                return baseName;

            // 收集同类型同名节点
            List<string> existingNames = new List<string>();
            foreach (Node existingNode in graph.nodes)
            {
                // 跳过当前节点
                if (existingNode == node)
                    continue;
                
                // 只检查相同类型的节点（Event或Condition）
                bool isSameType = 
                    (existingNode is EventNode && node is EventNode) ||
                    (existingNode is ConditionNode && node is ConditionNode);
                
                if (isSameType)
                {
                    existingNames.Add(existingNode.name);
                }
            }

            // 如果没有同名冲突，直接返回基础名称
            if (!existingNames.Contains(baseName))
                return baseName;

            // 添加后缀直到找到不重复的名称
            int suffix = 1;
            string uniqueName;
            do
            {
                uniqueName = $"{baseName}{suffix}";
                suffix++;
            } while (existingNames.Contains(uniqueName));

            return uniqueName;
        }
    }
} 