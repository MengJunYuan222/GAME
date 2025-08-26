using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// 深度搜索查找子物体
    /// </summary>
    /// <param name="parent">父级变换</param>
    /// <param name="name">要查找的对象名称</param>
    /// <returns>找到的Transform，未找到则返回null</returns>
    public static Transform FindDeep(this Transform parent, string name)
    {
        if (parent.name == name)
            return parent;
            
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null)
                return found;
        }
        
        return null;
    }
    
    /// <summary>
    /// 根据组件类型深度查找子物体
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="parent">父级变换</param>
    /// <returns>找到的第一个组件，未找到则返回null</returns>
    public static T FindComponentInChildren<T>(this Transform parent) where T : Component
    {
        T component = parent.GetComponent<T>();
        if (component != null)
            return component;
            
        foreach (Transform child in parent)
        {
            component = FindComponentInChildren<T>(child);
            if (component != null)
                return component;
        }
        
        return null;
    }
    
    /// <summary>
    /// 收集所有指定类型的子组件
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="parent">父级变换</param>
    /// <param name="result">结果列表</param>
    public static void GetComponentsInChildrenNonAlloc<T>(this Transform parent, List<T> result) where T : Component
    {
        if (result == null)
            result = new List<T>();
            
        T component = parent.GetComponent<T>();
        if (component != null)
            result.Add(component);
            
        foreach (Transform child in parent)
        {
            GetComponentsInChildrenNonAlloc(child, result);
        }
    }
}
