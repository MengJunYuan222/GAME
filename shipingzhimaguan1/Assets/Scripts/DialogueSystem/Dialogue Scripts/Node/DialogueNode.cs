using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using System.Linq;
using DialogueSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DialogueSystem
{
	/// <summary>
	/// 统一对话节点 - 同时支持普通对话和分支选项
	/// </summary>
	[Serializable] // 添加Serializable特性
	public class DialogueNode : BaseNode
	{
		// 使用基类中的baseInput作为输入端口
		[Output(connectionType = ConnectionType.Multiple)] public BaseNode output;

		[Header("基本信息")]
		[SerializeField] 
		public ActorSO actorSO;
		[TextArea(3, 10)]
		public string dialogueText;

		[Header("语音设置")]
		public bool playVoice = false;  // 是否播放对话语音
		public AudioClip voiceClip;  // 对话语音剪辑
		public string voicePath = ""; // Resources下的语音路径，如 "Audio/Voice/NPC_01"
		[Tooltip("语音播放音量，相对于全局语音音量")]
		[Range(0, 1)]
		public float voiceVolume = 1.0f;  // 语音音量
		[Tooltip("语音输出通道")]
		public VoiceOutputType voiceOutputType = VoiceOutputType.Default;  // 语音输出类型
		[Tooltip("语音输出的混音器组，可以控制音频效果和路由")]
		public AudioMixerGroup outputAudioMixerGroup; // 语音输出的混音器组

		// 语音输出类型枚举
		public enum VoiceOutputType
		{
			Default,    // 默认语音通道
			Dialogue,   // 对话专用通道
			Music,      // 音乐通道
			SFX,        // 音效通道
			Custom      // 自定义通道
		}
		
		[Tooltip("自定义输出通道名称（仅在VoiceOutputType为Custom时使用）")]
		public string customOutputName = "";  // 自定义输出通道名称

		[Header("节点类型")]
		public DialogueType dialogueType = DialogueType.Simple;
		
		public enum DialogueType
		{
			Simple,     // 普通对话，使用output端口
			Choice      // 选项对话，使用nextNodes端口
		}

		[Header("选项设置")]
		public List<string> options = new List<string>();
		[Output(dynamicPortList = true, connectionType = ConnectionType.Multiple)] public List<BaseNode> nextNodes;

		[Header("选项语音设置")]
		[Tooltip("是否为每个选项播放语音")]
		public bool playOptionsVoice = false;  // 是否为选项播放语音
		public List<AudioClip> optionVoiceClips = new List<AudioClip>();  // 选项语音剪辑
		public List<string> optionVoicePaths = new List<string>();  // 选项语音路径
		[Tooltip("选项语音输出的混音器组")]
		public AudioMixerGroup optionsAudioMixerGroup; // 选项语音输出的混音器组

		// 其他设置
		[Header("节点设置")]
		public bool isOneTimeDialogue = false;
		[Tooltip("勾选此项表示此节点是对话结束节点")]
		public bool isEndNode = false;

		// 摄像机震动设置
		[Header("摄像机震动设置")]
		[Tooltip("是否在显示此对话节点时触发摄像机震动")]
		public bool shakeOnShow = false;
		[Tooltip("震动强度类型")]
		public CameraShakeType shakeType = CameraShakeType.Medium;
		
		public enum CameraShakeType
		{
			Light,      // 轻微震动
			Medium,     // 中等震动
			Strong,     // 强烈震动
			AceAttorney // 逆转裁判式震动
		}

		// 选项选择状态
		private int selectedOptionIndex = -1;

		// 选项动画设置
		[Header("选项动画设置")]
		public List<bool> optionPlayAnim = new List<bool>();      // 是否播放动画
		public List<string> optionAnimName = new List<string>();  // 动画名或动画面板名
		[Header("选项动画类型")]
		public List<UIAnimType> optionAnimType = new List<UIAnimType>();

		// 选项震动设置
		[Header("选项震动设置")]
		[Tooltip("选项是否触发摄像机震动")]
		public List<bool> optionShakeCamera = new List<bool>();
		[Tooltip("选项震动强度类型")]
		public List<CameraShakeType> optionShakeType = new List<CameraShakeType>();

		// 选项Timeline设置
		[Header("选项Timeline设置")]
		[Tooltip("选择此选项后触发的Timeline ID")]
		public List<string> optionTimelineID = new List<string>();  // 选择选项后要播放的Timeline ID
		[Tooltip("是否等待Timeline播放完成后再继续对话")]
		public List<bool> optionWaitForTimeline = new List<bool>(); // 是否等待Timeline播放完成

		// 选项Timeline数据
		[System.Serializable]
		public class OptionTimelineData
		{
			public PlayableAsset timelineAsset;  // Timeline资产
			public GameObject timelineTarget;     // Timeline目标对象
			public bool pauseDialogueWhilePlaying = true;  // 播放时暂停对话
		}

		// 选项类
		[System.Serializable]
		public class DialogueOption
		{
			public string optionText;
			public BaseNode targetNode;
			public bool playTimeline = false;  // 是否播放Timeline
			public OptionTimelineData timelineData;  // Timeline数据
		}

		// 获取对话文本的简短预览
		public string GetDialoguePreview()
		{
			if (string.IsNullOrEmpty(dialogueText))
				return "空对话";
                
			// 取前5个字符，超出则添加省略号
			if (dialogueText.Length <= 5)
				return dialogueText;
			else
				return dialogueText.Substring(0, 5) + "...";
		}

		// 更新节点名称以反映对话内容
		public override void UpdateNodeName()
		{
			string actorName = actorSO != null ? actorSO.actorName : "未知角色";
			string preview = GetDialoguePreview();
			name = $"{actorName}：{preview}";
		}

		// 在RuntimeInitializeOnLoadMethod中添加一个静态初始化方法
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			// 确保DialogueNode类在运行时初始化
			Debug.Log("DialogueNode类已初始化");
		}
		
		// 修改OnValidate方法，使其更健壮
		private void OnValidate()
		{
			try
			{
				// 确保选择类型节点有至少一个选项
				if (dialogueType == DialogueType.Choice && options.Count == 0)
				{
					options.Add("选项 1");
				}
				
				// 如果是选择类型，确保每个选项都有对应的动态端口
				if (dialogueType == DialogueType.Choice)
				{
					for (int i = 0; i < options.Count; i++)
					{
						string portName = "nextNodes " + i;
						if (!HasPort(portName))
						{
							Debug.Log($"OnValidate: 为选项 {i+1} 创建端口 {portName}");
							AddDynamicOutput(typeof(BaseNode), ConnectionType.Override, TypeConstraint.Inherited, portName);
						}
					}
				}

				// 确保Timeline相关列表与选项数量一致
				SyncTimelineLists();
				
				// 确保选项语音列表与选项数量一致
				SyncOptionVoiceLists();
				
				// 如果设置了语音剪辑但没有路径，自动生成路径
				if (playVoice && voiceClip != null && string.IsNullOrEmpty(voicePath))
				{
					// 使用角色名称和节点GUID生成路径
					string actorName = actorSO != null ? actorSO.actorName : "Voice";
					voicePath = $"Audio/Voice/{actorName}_{nodeGUID}";
				}
				
				UpdateNodeName();
				
				// 更新端口配置
				try
				{
					UpdatePorts();
				}
				catch (System.Exception ex)
				{
					Debug.LogWarning($"更新端口时出错: {ex.Message}");
				}
				
				#if UNITY_EDITOR
				// 标记资产已修改
				UnityEditor.EditorUtility.SetDirty(this);
				if (graph != null) UnityEditor.EditorUtility.SetDirty(graph);
				#endif
			}
			catch (System.Exception e)
			{
				Debug.LogError($"DialogueNode.OnValidate错误: {e.Message}\n{e.StackTrace}");
			}
		}

		// 同步Timeline相关列表与选项数量一致
		private void SyncTimelineLists()
		{
			if (dialogueType != DialogueType.Choice) return;

			// 确保optionTimelineID列表与options数量一致
			while (optionTimelineID.Count < options.Count)
			{
				optionTimelineID.Add(string.Empty);
			}
			while (optionTimelineID.Count > options.Count)
			{
				optionTimelineID.RemoveAt(optionTimelineID.Count - 1);
			}

			// 确保optionWaitForTimeline列表与options数量一致
			while (optionWaitForTimeline.Count < options.Count)
			{
				optionWaitForTimeline.Add(true);  // 默认等待Timeline播放完成
			}
			while (optionWaitForTimeline.Count > options.Count)
			{
				optionWaitForTimeline.RemoveAt(optionWaitForTimeline.Count - 1);
			}
			
			// 确保摄像机震动列表与选项数量一致
			while (optionShakeCamera.Count < options.Count)
			{
				optionShakeCamera.Add(false);  // 默认不震动
			}
			while (optionShakeCamera.Count > options.Count)
			{
				optionShakeCamera.RemoveAt(optionShakeCamera.Count - 1);
			}
			
			// 确保摄像机震动类型列表与选项数量一致
			while (optionShakeType.Count < options.Count)
			{
				optionShakeType.Add(CameraShakeType.Medium);  // 默认中等震动
			}
			while (optionShakeType.Count > options.Count)
			{
				optionShakeType.RemoveAt(optionShakeType.Count - 1);
			}
		}
        
        // 同步选项语音列表与选项数量一致
        private void SyncOptionVoiceLists()
        {
            if (dialogueType != DialogueType.Choice) return;
            
            // 确保optionVoiceClips列表与options数量一致
            while (optionVoiceClips.Count < options.Count)
            {
                optionVoiceClips.Add(null);
            }
            while (optionVoiceClips.Count > options.Count)
            {
                optionVoiceClips.RemoveAt(optionVoiceClips.Count - 1);
            }
            
            // 确保optionVoicePaths列表与options数量一致
            while (optionVoicePaths.Count < options.Count)
            {
                optionVoicePaths.Add(string.Empty);
            }
            while (optionVoicePaths.Count > options.Count)
            {
                optionVoicePaths.RemoveAt(optionVoicePaths.Count - 1);
            }
        }

		// 重写基类中的IsEndNode方法
		public override bool IsEndNode()
		{
			// 如果显式设置为结束节点，则返回true
			if (isEndNode) 
			{
				Debug.Log($"节点 {name} 标记为结束节点");
				return true;
			}
			
			// 如果是简单对话模式且没有连接任何后续节点，也视为结束节点
			if (dialogueType == DialogueType.Simple)
			{
				var port = GetOutputPort("output");
				if (port == null || !port.IsConnected)
				{
					Debug.Log($"节点 {name} 是简单对话模式且没有连接输出，视为结束节点");
					return true;
				}
			}
			
			// 如果是选择模式且所有选项都没有连接，也视为结束节点
			if (dialogueType == DialogueType.Choice)
			{
				bool hasConnections = false;
				// 遍历所有动态输出端口，查找是否有连接
				foreach (var port in DynamicOutputs)
				{
					if (port.IsConnected)
					{
						hasConnections = true;
						break;
					}
				}
				
				if (!hasConnections)
				{
					Debug.Log($"节点 {name} 是选择模式且没有选项连接，视为结束节点");
					return true;
				}
			}
			
			return false;
		}

		// 处理节点 - 根据类型显示对话内容或选项
		public override void ProcessNode(DialogueUIManager uiManager, DialogueNodeGraph graph)
		{
			if (uiManager == null) return;

			// 重置选项索引，确保每次都重新选择
			selectedOptionIndex = -1;

			// 显示对话内容
			uiManager.ShowDialogue(actorSO, dialogueText);
			
			// 如果设置了震动，触发摄像机震动
			if (shakeOnShow && uiManager != null)
			{
				TriggerCameraShake(uiManager, shakeType);
			}

			// 播放语音
			if (playVoice)
			{
				if (voiceClip != null)
				{
					// 直接使用指定的语音剪辑
					AudioManager.Instance?.PlayVoiceClip(voiceClip, outputAudioMixerGroup);
				}
				else if (!string.IsNullOrEmpty(voicePath))
				{
					// 使用路径加载语音
					AudioManager.Instance?.PlayVoice(voicePath);
				}
			}

			// 如果是结束节点，特殊处理
			if (isEndNode)
			{
				Debug.Log($"处理结束节点 {name}");
				uiManager.HideOptions();
				// 不自动进入下一节点，等待用户确认
				return;
			}

			// 根据节点类型处理
			if (dialogueType == DialogueType.Choice)
			{
				// 确保选项和目标节点是同步的
				UpdateNextNodesList();
				// 同步Timeline相关列表
				SyncTimelineLists();
				// 同步选项语音列表
				SyncOptionVoiceLists();

				// 显示选项
				if (options != null && options.Count > 0)
				{
					uiManager.ShowOptions(options, (index) => {
						selectedOptionIndex = index;
						
						// 播放选项语音
						if (playOptionsVoice)
						{
							if (index < optionVoiceClips.Count && optionVoiceClips[index] != null)
							{
								// 播放指定的选项语音剪辑
								AudioManager.Instance?.PlayVoiceClip(optionVoiceClips[index], optionsAudioMixerGroup);
							}
							else if (index < optionVoicePaths.Count && !string.IsNullOrEmpty(optionVoicePaths[index]))
							{
								// 使用路径播放选项语音
								AudioManager.Instance?.PlayVoice(optionVoicePaths[index]);
							}
						}
						
						// 检查是否需要播放Timeline
						string timelineToPlay = string.Empty;
						if (optionTimelineID != null && index < optionTimelineID.Count)
						{
							timelineToPlay = optionTimelineID[index];
						}

						// 如果配置了Timeline ID，先播放Timeline
						if (!string.IsNullOrEmpty(timelineToPlay) && TimelineController.Instance != null)
						{
							Debug.Log($"选项 {options[index]} 触发Timeline: {timelineToPlay}");
							
							// 检查是否需要等待Timeline播放完成
							bool waitForTimeline = index < optionWaitForTimeline.Count && optionWaitForTimeline[index];
							
							if (waitForTimeline)
							{
								// 订阅Timeline完成事件
								TimelineController.Instance.OnTimelineCompleted += OnTimelineCompleted;
								
								// 播放Timeline
								TimelineController.Instance.PlayTimeline(timelineToPlay);
								
								// 暂时不继续对话流程，等Timeline播放完毕后再继续
							}
							else
							{
								// 不需要等待，播放Timeline并立即继续对话
								TimelineController.Instance.PlayTimeline(timelineToPlay);
								ContinueDialogueAfterOption(index, uiManager, graph);
							}
						}
						else
						{
							// 没有Timeline，直接继续对话
							ContinueDialogueAfterOption(index, uiManager, graph);
						}
					});
				}
				else
				{
					Debug.LogWarning($"选择对话节点 {name} 没有选项");
				}
			}
			else
			{
				// 普通对话节点隐藏选项
				uiManager.HideOptions();
			}
		}

		// Timeline播放完成后继续对话
		private void OnTimelineCompleted()
		{
			// 取消订阅，避免重复触发
			if (TimelineController.Instance != null)
			{
				TimelineController.Instance.OnTimelineCompleted -= OnTimelineCompleted;
			}
			
			// 获取当前的DialogueUIManager和DialogueNodeGraph
			DialogueUIManager uiManager = GameObject.FindObjectOfType<DialogueUIManager>();
			DialogueNodeGraph graph = GameObject.FindObjectOfType<DialogueNodeGraph>();
			
			if (uiManager != null && graph != null && selectedOptionIndex >= 0)
			{
				ContinueDialogueAfterOption(selectedOptionIndex, uiManager, graph);
			}
		}

		// 在选择选项后继续对话流程
		private void ContinueDialogueAfterOption(int index, DialogueUIManager uiManager, DialogueNodeGraph graph)
		{
			// 如果设置了选项震动，触发摄像机震动
			if (optionShakeCamera != null && index < optionShakeCamera.Count && optionShakeCamera[index])
			{
				CameraShakeType shakeIntensity = (optionShakeType != null && index < optionShakeType.Count) 
					? optionShakeType[index] : CameraShakeType.Medium;
				
				TriggerCameraShake(uiManager, shakeIntensity);
			}
			
			// 先处理UI动画（如果有）
			UIAnimType animType = (optionAnimType != null && index < optionAnimType.Count) ? optionAnimType[index] : UIAnimType.None;
			string animName = (optionAnimName != null && index < optionAnimName.Count) ? optionAnimName[index] : string.Empty;
			bool shouldPlayAnim = (optionPlayAnim != null && index < optionPlayAnim.Count) && optionPlayAnim[index];
			
			if (shouldPlayAnim && (animType != UIAnimType.None || !string.IsNullOrEmpty(animName)))
			{
				Debug.Log($"选项 {index+1} 触发动画: 类型={animType}, 名称={animName}");
				
				// 在DialogueUIManager中调用播放动画方法
				if (uiManager != null)
				{
					// 播放动画
					bool waitForAnimation = true; // 默认等待动画播放完成
					
					// 尝试播放动画，如果返回true表示动画已开始播放
					if (uiManager.PlayAnimation(animType, animName, waitForAnimation))
					{
						// 如果需要等待动画播放完成，则返回，由动画完成事件触发下一步
						Debug.Log("等待动画播放完成后继续对话");
						// 这里不直接进入下一节点，而是让动画系统在完成后调用
						return;
					}
					else
					{
						// 如果动画播放失败，记录警告并继续
						Debug.LogWarning($"动画播放失败或未找到: 类型={animType}, 名称={animName}");
					}
				}
			}

			// 动画相关处理完毕，继续进入下一节点
			if (graph != null)
				graph.ProcessNextNode();
		}

		// 获取下一个节点 - 根据节点类型和选择
		public override BaseNode GetNextNode()
		{
			// 如果是结束节点，直接返回null
			if (isEndNode)
			{
				Debug.Log($"结束节点 {name} 返回null作为下一节点");
				return null;
			}
			
			// 根据对话类型获取下一个节点
			if (dialogueType == DialogueType.Choice)
			{
				// 选项对话模式
				if (selectedOptionIndex >= 0 && selectedOptionIndex < nextNodes.Count)
				{
					BaseNode nextNode = nextNodes[selectedOptionIndex];
					// 重置选项索引，以便下次进入该节点时重新选择
					selectedOptionIndex = -1;
					return nextNode;
				}
				return null;
			}
			else
			{
				// 普通对话模式
				var port = GetOutputPort("output");
				if (port != null && port.IsConnected)
				{
					return port.Connection.node as BaseNode;
				}
				return null;
			}
		}

		// 同步动态端口到nextNodes列表
		private void UpdateNextNodesList()
		{
			try
			{
				// 查找所有nextNodes开头的输出端口
				var nextNodePorts = DynamicOutputs.Where(port => port != null && port.fieldName != null && port.fieldName.StartsWith("nextNodes")).ToList();
				
				// 创建一个新的列表来存储连接的节点
				List<BaseNode> connectedNodes = new List<BaseNode>();
				
				// 按照端口名称的索引排序
				nextNodePorts.Sort((a, b) => {
					int indexA = GetPortIndex(a?.fieldName);
					int indexB = GetPortIndex(b?.fieldName);
					return indexA.CompareTo(indexB);
				});
				
				// 收集所有连接的节点
				foreach (var port in nextNodePorts)
				{
					if (port != null && port.IsConnected)
					{
						BaseNode connectedNode = port.Connection?.node as BaseNode;
						if (connectedNode != null)
						{
							connectedNodes.Add(connectedNode);
						}
						else
						{
							connectedNodes.Add(null); // 保持索引对齐
						}
					}
					else
					{
						connectedNodes.Add(null); // 未连接的端口也添加一个空项
					}
				}
				
				// 更新nextNodes列表
				// 确保options和nextNodes列表长度一致
				while (options.Count < connectedNodes.Count)
				{
					options.Add("选项 " + (options.Count + 1));
				}
				
				nextNodes = connectedNodes;
			}
			catch (System.Exception e)
			{
				Debug.LogError($"UpdateNextNodesList错误: {e.Message}\n{e.StackTrace}");
			}
		}
		
		// 从端口名称中提取索引
		private int GetPortIndex(string portName)
		{
			if (string.IsNullOrEmpty(portName)) return 0;
			
			if (portName.StartsWith("nextNodes "))
			{
				string indexStr = portName.Substring("nextNodes ".Length);
				int index;
				if (int.TryParse(indexStr, out index))
				{
					return index;
				}
			}
			return 0;
		}

		// 获取输出端口值
		public override object GetValue(NodePort port)
		{
			if (port.fieldName == "output")
			{
				return this;
			}
			else if (port.fieldName == "baseInput")
			{
				return this; // 处理基类的输入端口
			}
			else if (port.fieldName.StartsWith("nextNodes "))
			{
				int index;
				if (int.TryParse(port.fieldName.Substring("nextNodes ".Length), out index))
				{
					if (nextNodes != null && index >= 0 && index < nextNodes.Count)
					{
						return nextNodes[index];
					}
				}
			}
			return null;
		}

		// 重写OnCreateConnection确保序列化正确更新
		public override void OnCreateConnection(NodePort from, NodePort to)
		{
			base.OnCreateConnection(from, to);
			
			Debug.Log($"创建连接: 从 [{from.node.name}].{from.fieldName} 到 [{to.node.name}].{to.fieldName}");
			
			// 确保nextNodes列表更新
			UpdateNextNodesList();
			
			#if UNITY_EDITOR
			// 标记资产已修改
			UnityEditor.EditorUtility.SetDirty(this);
			if (graph != null) UnityEditor.EditorUtility.SetDirty(graph);
			#endif
		}
		
		// 当连接被移除时调用
		public override void OnRemoveConnection(NodePort port)
		{
			Debug.Log($"移除连接: 节点[{name}]的端口[{port.fieldName}]");
			UpdateNextNodesList();
			// 同步选项语音列表
			SyncOptionVoiceLists();
		}

		// 添加新的选项
		[ContextMenu("添加选项")]
		public void AddOption()
		{
			options.Add("选项 " + (options.Count + 1));
			
			// 确保选项相关的数据数组也同步添加一个元素
			if (optionPlayAnim != null) optionPlayAnim.Add(false);
			if (optionAnimName != null) optionAnimName.Add(string.Empty);
			if (optionAnimType != null) optionAnimType.Add(UIAnimType.None);
			if (optionShakeCamera != null) optionShakeCamera.Add(false);
			if (optionShakeType != null) optionShakeType.Add(CameraShakeType.Medium);
			if (optionTimelineID != null) optionTimelineID.Add(string.Empty);
			if (optionWaitForTimeline != null) optionWaitForTimeline.Add(true);
			if (optionVoiceClips != null) optionVoiceClips.Add(null);
			if (optionVoicePaths != null) optionVoicePaths.Add(string.Empty);
			
			// 确保nextNodes列表也同步更新
			if (nextNodes != null) nextNodes.Add(null);
			
			// 更新端口配置以反映新增的选项
			UpdatePorts();
			
			// 确保为新选项创建动态输出端口
			string portName = "nextNodes " + (options.Count - 1);
			if (!HasPort(portName)) {
				Debug.Log($"为选项 {options.Count} 创建新端口: {portName}");
				AddDynamicOutput(typeof(BaseNode), ConnectionType.Override, TypeConstraint.Inherited, portName);
			}
			
			// 标记资产已修改，确保Unity保存更改
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
			if (graph != null) UnityEditor.EditorUtility.SetDirty(graph);
			#endif
		}

		// 删除最后一个选项
		[ContextMenu("删除最后选项")]
		public void RemoveLastOption()
		{
			if (options.Count > 0)
			{
				int lastIndex = options.Count - 1;
				
				// 移除最后一个选项相关的所有数据
				options.RemoveAt(lastIndex);
				
				// 移除相关的数据数组元素
				if (optionPlayAnim != null && optionPlayAnim.Count > lastIndex)
					optionPlayAnim.RemoveAt(lastIndex);
					
				if (optionAnimName != null && optionAnimName.Count > lastIndex)
					optionAnimName.RemoveAt(lastIndex);
					
				if (optionAnimType != null && optionAnimType.Count > lastIndex)
					optionAnimType.RemoveAt(lastIndex);
					
				if (optionShakeCamera != null && optionShakeCamera.Count > lastIndex)
					optionShakeCamera.RemoveAt(lastIndex);
					
				if (optionShakeType != null && optionShakeType.Count > lastIndex)
					optionShakeType.RemoveAt(lastIndex);
					
				if (optionTimelineID != null && optionTimelineID.Count > lastIndex)
					optionTimelineID.RemoveAt(lastIndex);
					
				if (optionWaitForTimeline != null && optionWaitForTimeline.Count > lastIndex)
					optionWaitForTimeline.RemoveAt(lastIndex);
					
				if (optionVoiceClips != null && optionVoiceClips.Count > lastIndex)
					optionVoiceClips.RemoveAt(lastIndex);
					
				if (optionVoicePaths != null && optionVoicePaths.Count > lastIndex)
					optionVoicePaths.RemoveAt(lastIndex);
					
				// 移除nextNodes中对应的元素
				if (nextNodes != null && nextNodes.Count > lastIndex)
					nextNodes.RemoveAt(lastIndex);
				
				// 移除对应的动态端口
				string portName = "nextNodes " + lastIndex;
				NodePort port = GetPort(portName);
				if (port != null)
				{
					Debug.Log($"移除端口: {portName}");
					RemoveDynamicPort(port);
				}
				
				// 更新端口配置
				UpdatePorts();
				
				// 标记资产已修改，确保Unity保存更改
				#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
				if (graph != null) UnityEditor.EditorUtility.SetDirty(graph);
				#endif
			}
			else
			{
				Debug.LogWarning("没有选项可以移除");
			}
		}

		// 切换对话模式
		[ContextMenu("切换对话模式")]
		public void ToggleDialogueMode()
		{
			if (dialogueType == DialogueType.Simple)
			{
				dialogueType = DialogueType.Choice;
				// 确保至少有一个选项
				if (options.Count == 0)
				{
					options.Add("选项 1");
				}
			}
			else
			{
				dialogueType = DialogueType.Simple;
			}
			
			// 更新端口
			UpdatePorts();
			
			// 同步选项语音列表
			SyncOptionVoiceLists();
		}

		protected override void Init()
		{
			base.Init();
			
			// 确保节点名称反映对话内容
			UpdateNodeName();
		}

		// 触发摄像机震动
		private void TriggerCameraShake(DialogueUIManager uiManager, CameraShakeType shakeType)
		{
			if (uiManager == null) return;
			
			// 将震动类型转换为字符串，调用DialogueUIManager的震动方法
			string shakeIntensity = shakeType.ToString();
			uiManager.ShakeCamera(shakeIntensity);
		}
		
		// 为所有选项启用震动
		[ContextMenu("为所有选项启用震动")]
		public void EnableShakeForAllOptions()
		{
			if (optionShakeCamera == null || optionShakeCamera.Count == 0)
			{
				SyncTimelineLists(); // 确保列表已创建并同步
			}
			
			for (int i = 0; i < optionShakeCamera.Count; i++)
			{
				optionShakeCamera[i] = true;
			}
			
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
			if (graph != null) UnityEditor.EditorUtility.SetDirty(graph);
			#endif
			
			Debug.Log($"已为节点[{name}]的所有选项启用震动");
		}
		
		// 为所有选项禁用震动
		[ContextMenu("为所有选项禁用震动")]
		public void DisableShakeForAllOptions()
		{
			if (optionShakeCamera == null || optionShakeCamera.Count == 0) return;
			
			for (int i = 0; i < optionShakeCamera.Count; i++)
			{
				optionShakeCamera[i] = false;
			}
			
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
			if (graph != null) UnityEditor.EditorUtility.SetDirty(graph);
			#endif
			
			Debug.Log($"已为节点[{name}]的所有选项禁用震动");
		}
	}
}