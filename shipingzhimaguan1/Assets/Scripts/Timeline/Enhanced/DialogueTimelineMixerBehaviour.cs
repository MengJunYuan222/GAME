using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using DialogueSystem;

public class DialogueTimelineMixerBehaviour : PlayableBehaviour
{
    private DialogueUIManager dialogueUIManager;
    private bool hasTriggered = false;
    private List<DialogueTimelineBehaviour> activeBehaviours = new List<DialogueTimelineBehaviour>();

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        dialogueUIManager = playerData as DialogueUIManager;
        if (dialogueUIManager == null) return;

        activeBehaviours.Clear();
        
        // 检查所有输入的对话行为
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<DialogueTimelineBehaviour> inputPlayable = (ScriptPlayable<DialogueTimelineBehaviour>)playable.GetInput(i);
            DialogueTimelineBehaviour input = inputPlayable.GetBehaviour();

            // 只处理权重大于0的输入
            if (inputWeight > 0f)
            {
                activeBehaviours.Add(input);
            }
        }

        // 处理当前活跃的对话
        foreach (var behaviour in activeBehaviours)
        {
            behaviour.ProcessDialogue(dialogueUIManager, playable);
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        activeBehaviours.Clear();
    }
}

