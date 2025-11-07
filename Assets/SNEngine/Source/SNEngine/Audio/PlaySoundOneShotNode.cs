using UnityEngine;

namespace SNEngine.Audio
{
    public class PlaySoundOneShotNode : AudioNodeInteraction
    {
        [Input, SerializeField] private AudioClip _clip;
        [Input, SerializeField, Range(0f, 1f)] private float _volumeScale = 1f;

        protected override void Interact(AudioObject input)
        {
            if (_clip != null)
                input.PlayOneShot(_clip, _volumeScale);
        }
    }
}
