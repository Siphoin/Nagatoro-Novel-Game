using SNEngine.Services;
using UnityEngine;

namespace SNEngine.Audio.Music
{
    public class SetVolumeMusicWithFadeNode : MusicInteractionNodeAsync
    {
        [XNode.Node.Input, SerializeField, Min(0)] private float _fadeDuration = 0;
        [XNode.Node.Input, SerializeField, Range(0, 1)] private float _targetVolume = 1;
        public override async void Execute()
        {
            base.Execute();
            var duration = GetInputValue(nameof(_fadeDuration), _fadeDuration);
            var volume = GetInputValue(nameof(_targetVolume), _targetVolume);
            await MusicPlayer.FadeVolumeAsync(volume, duration);
            StopTask();
        }
    }
}
