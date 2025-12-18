using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables.Set
{
    public class SetAudioClipNode : SetVaritableNode<AudioClip>
    {
        protected override void OnSetTargetValueChanged(VaritableNode<AudioClip> targetNode, AudioClip newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<AudioClipLibrary>(newValue);
        }
    }
}
