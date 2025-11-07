using System;
using UnityEngine;

namespace SNEngine.Audio.Music
{
    public class SetMusicMuteNode : MusicInteractionNode
    {
        [XNode.Node.Input, SerializeField] private bool _flag = false;
        public override void Execute()
        {
            var flag = GetInputValue(nameof(_flag), _flag);
            MusicPlayer.Mute = flag;
        }
    }
}
