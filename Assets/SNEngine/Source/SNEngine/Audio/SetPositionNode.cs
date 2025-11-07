using UnityEngine;

namespace SNEngine.Audio
{
    public class SetPositionNode : AudioNodeInteraction
    {
        [Input, SerializeField] private Vector3 _position = Vector3.zero;
        protected override void Interact(AudioObject input) => input.SetPosition(_position);
    }
}
