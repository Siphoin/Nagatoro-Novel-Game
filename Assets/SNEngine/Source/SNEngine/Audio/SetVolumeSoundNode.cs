using System;
using UnityEngine;
namespace SNEngine.Audio
{
    public class SetVolumeSoundNode : AudioNodeInteraction
    {
        [Input, SerializeField, Range(0f, 1f)] private float _volume = 1;
        protected override void Interact(AudioObject input)
        {
            var value = GetInputValue(nameof(_volume), _volume);
            input.Volume = value;
        }

    }
}
