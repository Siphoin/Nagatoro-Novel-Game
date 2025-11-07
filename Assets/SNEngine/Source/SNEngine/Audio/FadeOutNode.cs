using UnityEngine;

namespace SNEngine.Audio
{
    public class FadeOutNode : AudioNodeInteraction
    {
        [Input, SerializeField, Min(0f)] private float _duration = 1f;
        protected override void Interact(AudioObject input) => input.FadeOut(_duration);
    }
}
