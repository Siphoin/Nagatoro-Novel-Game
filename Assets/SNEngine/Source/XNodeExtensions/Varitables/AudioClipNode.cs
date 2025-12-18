using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    public class AudioClipNode : UnityVaritableNode<AudioClip, AudioClipLibrary>
    {
        protected override void OnValueChanged(AudioClip oldValue, AudioClip newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<AudioClipLibrary>(newValue);
        }
    }
}
