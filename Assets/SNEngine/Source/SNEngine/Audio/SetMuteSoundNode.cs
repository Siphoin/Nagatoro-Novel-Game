using UnityEngine;

namespace SNEngine.Audio
{
    public class SetMuteSoundNode : AudioNodeInteraction
    {
        [Input, SerializeField] private bool _mute;
        protected override void Interact(AudioObject input)
        {
            var value = GetInputValue<bool>(nameof(_mute), _mute);
            input.Mute = value;
        }
    }
}
