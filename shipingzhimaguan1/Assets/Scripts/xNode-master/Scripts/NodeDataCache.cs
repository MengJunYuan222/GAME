using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace XNode {
    /// <summary> Precaches reflection data in editor so we won't have to do it runtime </summary>
    public static class NodeDataCache {
        private static PortDataCache portDataCache;
        private static Dictionary<System.Type, Dictionary<string, string>> formerlySerializedAsCache;
        private static Dictionary<System.Type, string> typeQualifiedNameCache;
        private static bool Initialized { get { return portDataCache != null; } }

        public static string GetTypeQualifiedName(System.Type type) {
            if(typeQualifiedNameCache == null) typeQualifiedNameCache = new Dictionary<System.Type, string>();
            
            string name;
            if (!typeQualifiedNameCache.TryGetValue(type, out name)) {
                name = type.AssemblyQualifiedName;
                typeQualifiedNameCache.Add(type, name);
            }
            return name;
        }

        /// <summary> Update all ports on target node and assign relevant properties. </summary>
        public static void UpdatePorts(Node node, Dictionary<string, NodePort> ports) {
            try 
            {
                if (!Initialized) BuildCache();
                
                // Check if we have cached ports for this node type
                Dictionary<string, NodePort> portCache = null;
                if (!portDataCache.TryGetValue(node.GetType(), out portCache)) return;
                
                // Add missing fields
                foreach (KeyValuePair<string, NodePort> kvp in portCache) {
                    // Check if field exists in dictionary
                    NodePort port;
                    if (ports.TryGetValue(kvp.Key, out port)) {
                        // If it does, make sure the types match
                        if (port.ValueType != kvp.Value.ValueType)
                        {
                            // Types don't match. Remove it before moving on
                            ports.Remove(kvp.Key);
                        }
                        else continue;
                    }

                    // Field doesn't exist. Create and cache it
                    port = new NodePort(kvp.Value, node);
                    // Log if we're adding a port with a name that already exists
                    if (ports.ContainsKey(port.fieldName)) {
                        Debug.LogWarning($"尝试添加已存在的端口 {port.fieldName} 到 {node.GetType().Name}，跳过此端口");
                        continue;
                    }
                    ports.Add(port.fieldName, port);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NodeDataCache.UpdatePorts错误: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Extracts the underlying types from arrays and lists, the only collections for dynamic port lists
        /// currently supported. If the given type is not applicable (i.e. if the dynamic list port was not
        /// defined as an array or a list), returns the given type itself.
        /// </summary>
        private static System.Type GetBackingValueType(System.Type portValType) {
            if (portValType.HasElementType) {
                return portValType.GetElementType();
            }
            if (portValType.IsGenericType && portValType.GetGenericTypeDefinition() == typeof(List<>)) {
                return portValType.GetGenericArguments()[0];
            }
            return portValType;
        }

        /// <summary>Returns true if the given port is in a dynamic port list.</summary>
        private static bool IsDynamicListPort(NodePort port) {
            // Ports flagged as "dynamicPortList = true" end up having a "backing port" and a name with an index, but we have
            // no guarantee that a dynamic port called "output 0" is an element in a list backed by a static "output" port.
            // Thus, we need to check for attributes... (but at least we don't need to look at all fields this time)
            string[] fieldNameParts = port.fieldName.Split(' ');
            if (fieldNameParts.Length != 2) return false;

            FieldInfo backingPortInfo = port.node.GetType().GetField(fieldNameParts[0]);
            if (backingPortInfo == null) return false;

            object[] attribs = backingPortInfo.GetCustomAttributes(true);
            return attribs.Any(x => {
                Node.InputAttribute inputAttribute = x as Node.InputAttribute;
                Node.OutputAttribute outputAttribute = x as Node.OutputAttribute;
                return inputAttribute != null && inputAttribute.dynamicPortList ||
                       outputAttribute != null && outputAttribute.dynamicPortList;
            });
        }

        /// <summary> Cache node types </summary>
        private static void BuildCache() {
            portDataCache = new PortDataCache();
            System.Type baseType = typeof(Node);
            List<System.Type> nodeTypes = new List<System.Type>();
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            // Loop through assemblies and add node types to list
            foreach (Assembly assembly in assemblies) {
                // Skip certain dlls to improve performance
                string assemblyName = assembly.GetName().Name;
                int index = assemblyName.IndexOf('.');
                if (index != -1) assemblyName = assemblyName.Substring(0, index);
                switch (assemblyName) {
                    // The following assemblies, and sub-assemblies (eg. UnityEngine.UI) are skipped
                    case "UnityEditor":
                    case "UnityEngine":
                    case "Unity":
                    case "System":
                    case "mscorlib":
                    case "Microsoft":
                        continue;
                    default:
                        nodeTypes.AddRange(assembly.GetTypes().Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t)).ToArray());
                        break;
                }
            }

            for (int i = 0; i < nodeTypes.Count; i++) {
                CachePorts(nodeTypes[i]);
            }
        }

        public static List<FieldInfo> GetNodeFields(System.Type nodeType) {
            List<System.Reflection.FieldInfo> fieldInfo = new List<System.Reflection.FieldInfo>(nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            // GetFields doesnt return inherited private fields, so walk through base types and pick those up
            System.Type tempType = nodeType;
            while ((tempType = tempType.BaseType) != typeof(XNode.Node)) {
                FieldInfo[] parentFields = tempType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < parentFields.Length; i++) {
                    // Ensure that we do not already have a member with this type and name
                    FieldInfo parentField = parentFields[i];
                    if (fieldInfo.TrueForAll(x => x.Name != parentField.Name)) {
                        fieldInfo.Add(parentField);
                    }
                }
            }
            return fieldInfo;
        }

        private static void CachePorts(System.Type nodeType) {
            List<System.Reflection.FieldInfo> fieldInfo = GetNodeFields(nodeType);

            for (int i = 0; i < fieldInfo.Count; i++) {

                //Get InputAttribute and OutputAttribute
                object[] attribs = fieldInfo[i].GetCustomAttributes(true);
                Node.InputAttribute inputAttrib = attribs.FirstOrDefault(x => x is Node.InputAttribute) as Node.InputAttribute;
                Node.OutputAttribute outputAttrib = attribs.FirstOrDefault(x => x is Node.OutputAttribute) as Node.OutputAttribute;
                UnityEngine.Serialization.FormerlySerializedAsAttribute formerlySerializedAsAttribute = attribs.FirstOrDefault(x => x is UnityEngine.Serialization.FormerlySerializedAsAttribute) as UnityEngine.Serialization.FormerlySerializedAsAttribute;

                if (inputAttrib == null && outputAttrib == null) continue;

                if (inputAttrib != null && outputAttrib != null) Debug.LogError("Field " + fieldInfo[i].Name + " of type " + nodeType.FullName + " cannot be both input and output.");
                else {
                    if (!portDataCache.ContainsKey(nodeType)) portDataCache.Add(nodeType, new Dictionary<string, NodePort>());
                    NodePort port = new NodePort(fieldInfo[i]);
                    
                    // 检查字典中是否已存在该键，防止重复添加
                    if (portDataCache[nodeType].ContainsKey(port.fieldName))
                    {
                        Debug.LogWarning($"已存在相同名称的端口 '{port.fieldName}' 在类型 {nodeType.Name} 中。这可能是由继承引起的。跳过此端口以避免错误。");
                        continue;
                    }
                     
                    portDataCache[nodeType].Add(port.fieldName, port);
                }

                if (formerlySerializedAsAttribute != null) {
                    if (formerlySerializedAsCache == null) formerlySerializedAsCache = new Dictionary<System.Type, Dictionary<string, string>>();
                    if (!formerlySerializedAsCache.ContainsKey(nodeType)) formerlySerializedAsCache.Add(nodeType, new Dictionary<string, string>());

                    if (formerlySerializedAsCache[nodeType].ContainsKey(formerlySerializedAsAttribute.oldName)) Debug.LogError("Another FormerlySerializedAs with value '" + formerlySerializedAsAttribute.oldName + "' already exist on this node.");
                    else formerlySerializedAsCache[nodeType].Add(formerlySerializedAsAttribute.oldName, fieldInfo[i].Name);
                }
            }
        }

        [System.Serializable]
        private class PortDataCache : Dictionary<System.Type, Dictionary<string, NodePort>> { }
    }
}
