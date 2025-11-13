using SNEngine.Debugging;
using UnityEngine;
using UnityEngine.Video;

namespace SNEngine.VideoPlayerSystem
{
    public class PlayVideoNode : VideoInteractionNode
    {
        [SerializeField] private VideoClip _video;
        protected override void Interact(NovelVideoPlayer input)
        {
            if (_video is null)
            {
                NovelGameDebug.LogError($"video not seted for node {GUID}");
                return;
            }
            input.Clip = _video;
            input.Show();
            input.Play();
        }
    }
}
