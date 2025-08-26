using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using DialogueSystem;

[TrackColor(0.2f, 0.8f, 0.2f)]
[TrackClipType(typeof(DialogueTimelineClip))]
[TrackBindingType(typeof(DialogueUIManager))]
public class DialogueTimelineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<DialogueTimelineMixerBehaviour>.Create(graph, inputCount);
    }
}
