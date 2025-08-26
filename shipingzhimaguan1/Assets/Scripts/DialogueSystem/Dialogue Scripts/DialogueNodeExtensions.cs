using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueSystem;

public static class DialogueNodeExtensions
{
    // 提供一个获取对话图的方法
    public static DialogueNodeGraph GetParentGraph(this DialogueNode node)
    {
        // 这里需要根据你的对话系统结构提供一个获取父图的方法
        // 例如，如果节点存储了对父图的引用：
        // return node.parentGraph;
        
        // 临时方案：查找所有已加载的对话图资源，判断哪个包含此节点
        DialogueNodeGraph[] allGraphs = Resources.FindObjectsOfTypeAll<DialogueNodeGraph>();
        foreach (var graph in allGraphs)
        {
            // 查找包含此节点的图
            // 实际实现取决于你的对话图如何存储节点
            // if (graph.ContainsNode(node))
            // {
            //     return graph;
            // }
        }
        
        Debug.LogWarning("GetParentGraph方法未完全实现，请根据你的系统架构完善此方法");
        return null;
    }
    
    // 获取对话节点的角色扩展方法
    public static ActorSO GetActor(this DialogueNode node)
    {
        // 从节点中获取角色信息
        // 这里需要根据你的新DialogueNode结构来实现
        // 例如，如果是通过SerializeField的话：
        var fields = node.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.Name.Contains("actor") || field.Name.Contains("Actor"))
            {
                return field.GetValue(node) as ActorSO;
            }
        }
        
        // 如果找不到，返回null
        Debug.LogWarning($"无法找到节点 {node.name} 的角色信息");
        return null;
    }
    
    // 获取对话文本的扩展方法
    public static string GetText(this DialogueNode node)
    {
        // 从节点中获取对话文本
        var fields = node.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.Name.Contains("text") || field.Name.Contains("Text") || field.Name.Contains("content") || field.Name.Contains("Content"))
            {
                var value = field.GetValue(node);
                if (value is string)
                {
                    return value as string;
                }
            }
        }
        
        // 如果找不到，返回默认文本
        Debug.LogWarning($"无法找到节点 {node.name} 的对话文本");
        return "无法获取对话内容";
    }
    
    // 判断是否是结束节点的扩展方法
    public static bool IsEndNode(this DialogueNode node)
    {
        // 判断节点是否是结束节点
        var fields = node.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.Name.Contains("isEnd") || field.Name.Contains("IsEnd") || field.Name.Contains("end") || field.Name.Contains("End"))
            {
                var value = field.GetValue(node);
                if (value is bool)
                {
                    return (bool)value;
                }
            }
        }
        
        // 默认不是结束节点
        return false;
    }
}
