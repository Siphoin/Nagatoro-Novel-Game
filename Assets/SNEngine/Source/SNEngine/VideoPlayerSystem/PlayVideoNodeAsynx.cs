using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using UnityEngine;
using UnityEngine.Video;

namespace SNEngine.VideoPlayerSystem
{
    public class PlayVideoNodeAsynx : VideoInteractionNodeAsync
    {
        [SerializeField] private VideoClip _video;
        protected override async UniTask Interact(NovelVideoPlayer input)
        {
            if (_video is null)
            {
                NovelGameDebug.LogError($"video not seted for node {GUID}");
                StopTask();
                return;
            }
            input.Clip = _video;
            input.Show();
            input.Play();
            await UniTask.WaitUntil(() => input.IsPlaying);
            await UniTask.WaitUntil(() => !input.IsPlaying);
            StopTask();
        }
    }
}
