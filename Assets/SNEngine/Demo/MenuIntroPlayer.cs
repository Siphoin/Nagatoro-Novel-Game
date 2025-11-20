using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CoreGame
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(VideoPlayer))]
    [RequireComponent(typeof(AspectRatioFitter))]
    public class MenuIntroPlayer : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AspectRatioFitter _aspectRatioFitter;

        private RenderTexture _renderTexture;

        [SerializeField] private VideoClip _defaultClip;
        [SerializeField] private bool _playOnAwake = true;
        [SerializeField] private bool _isLooping = true;

        private void OnValidate()
        {
            if (!_videoPlayer) _videoPlayer = GetComponent<VideoPlayer>();
            if (!_audioSource) _audioSource = GetComponent<AudioSource>();
            if (!_rawImage) _rawImage = GetComponent<RawImage>();
            if (!_aspectRatioFitter) _aspectRatioFitter = GetComponent<AspectRatioFitter>();
        }

        private void Start()
        {
            SetupVideoPlayer();

            if (_defaultClip != null)
            {
                SetClip(_defaultClip);
            }
            _videoPlayer.isLooping = _isLooping;

            if (_playOnAwake && (_videoPlayer.clip != null || !string.IsNullOrEmpty(_videoPlayer.url)))
            {
                PrepareAndPlay();
            }
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            if (_aspectRatioFitter)
            {
                _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

                float aspectRatio = (float)source.width / source.height;
                _aspectRatioFitter.aspectRatio = aspectRatio;

                if (source.clip == null && !string.IsNullOrEmpty(source.url))
                {
                    UpdateRenderTexture((int)source.width, (int)source.height);
                }
            }

            source.Play();
        }

        private void SetupVideoPlayer()
        {
            _renderTexture = new RenderTexture(1280, 720, 24);
            _renderTexture.Create();

            _rawImage.texture = _renderTexture;

            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;

            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.SetTargetAudioSource(0, _audioSource);
            _audioSource.playOnAwake = false;

            _videoPlayer.prepareCompleted += OnVideoPrepared;
        }

        private void UpdateRenderTexture(int width, int height)
        {
            if (_renderTexture == null || _renderTexture.width != width || _renderTexture.height != height)
            {
                if (_renderTexture != null)
                {
                    _renderTexture.Release();
                    Destroy(_renderTexture);
                }

                _renderTexture = new RenderTexture(width, height, 24);
                _renderTexture.Create();
                _rawImage.texture = _renderTexture;
                _videoPlayer.targetTexture = _renderTexture;
            }
        }

        private void PrepareAndPlay()
        {
            if (_videoPlayer.isPrepared)
            {
                _videoPlayer.Play();
            }
            else
            {
                _videoPlayer.Prepare();
            }
        }

        private void SetClip(VideoClip clip)
        {
            _videoPlayer.clip = clip;
            if (clip != null)
            {
                UpdateRenderTexture((int)clip.width, (int)clip.height);
            }
        }
    }
}