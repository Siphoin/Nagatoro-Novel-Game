using UnityEngine;

namespace SNEngine.Audio.Music
{
    public class SetVolumeMusicNode : MusicInteractionNode
    {
        [XNode.Node.Input, SerializeField, Range(0, 1)] private float _targetVolume = 1;
        public override void Execute()
        {
            var volume = GetInputValue(nameof(_targetVolume), _targetVolume);
            MusicPlayer.Volume = volume;
        }
    }
}
