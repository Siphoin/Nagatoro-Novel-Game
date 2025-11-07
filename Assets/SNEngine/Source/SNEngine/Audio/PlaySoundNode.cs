using SNEngine.Debugging;
using SNEngine.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XNode;

namespace SNEngine.Audio
{
    public class PlaySoundNode : AudioNode
    {
        [SerializeField] private AudioClip _sound;
        [Output(ShowBackingValue.Never), SerializeField] private AudioObject _result;


        public override void Execute()
        {
            if (_sound is null)
            {
                NovelGameDebug.LogError($"sound is null on node {GUID}");
                return;
            }
            var service = NovelGame.Instance.GetService<AudioService>();
            _result = service.GetFreeAudioObject() as AudioObject;
            _result.CurrentSound = _sound;
            _result.Play();

        }

        public override object GetValue(NodePort port)
        {
            return _result;
        }
    }
}
