using UnityEngine;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Services;

namespace SNEngine.Audio
{
    public class CreateAudioNode : BaseNodeInteraction
    {
        [Output(ShowBackingValue.Never), SerializeField] private AudioObject _result;

        public override void Execute()
        {
            var service = NovelGame.Instance.GetService<AudioService>();
            _result = service.GetFreeAudioObject() as AudioObject;
        }
    }
}
