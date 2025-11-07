using UnityEngine;

namespace SNEngine.Audio
{
    public class FadeInNode : AudioNodeInteraction
    {
        [Input, SerializeField, Min(0f)] private float _duration = 1f;
        [Input, SerializeField, Range(0f, 1f)] private float _targetVolume = 1f;

        protected override void Interact(AudioObject input) => input.FadeIn(_duration, _targetVolume);
    }
}
