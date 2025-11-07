using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SNEngine.Audio.Music
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour, IMusicPlayer
    {
        [SerializeField] private AudioSource _audioSource;
        private CancellationTokenSource _fadeCts;
        private Queue<AudioClip> _currentQueue;

        public float Volume
        {
            get => _audioSource.volume;
            set => _audioSource.volume = Mathf.Clamp01(value);
        }

        public bool IsPlaying => _audioSource.isPlaying;
        public AudioClip CurrentTrack => _audioSource.clip;
        public bool Mute
        {
            get => _audioSource.mute;
            set => _audioSource.mute = value;
        }

        private bool _loop;
        public bool Loop
        {
            get => _loop;
            set
            {
                if (_currentQueue.Count > 1 && value)
                {
                    NovelGameDebug.LogError("Looping works only for a single track playlist.");
                    _loop = false;
                    return;
                }
                _loop = value;
                _audioSource.loop = value;
            }
        }

        private void Awake()
        {
            _currentQueue = new Queue<AudioClip>();
        }

        public void SetPlaylist(IEnumerable<AudioClip> playlist)
        {
            if (playlist is null) return;

            _currentQueue.Clear();
            foreach (var clip in playlist)
            {
                _currentQueue.Enqueue(clip);
            }

            PlayNextTrackAsync().Forget();
        }

        public void Pause()
        {
            if (_audioSource.isPlaying)
                _audioSource.Pause();
        }

        public void UnPause()
        {
            if (!_audioSource.isPlaying)
                _audioSource.UnPause();
        }

        public async UniTask StopAsync(float fadeDuration = 1f)
        {
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            await FadeRoutineAsync(_audioSource.volume, 0f, fadeDuration, _fadeCts.Token, stopAfterFade: true);
        }

        private async UniTaskVoid PlayNextTrackAsync()
        {
            if (_currentQueue.Count == 0)
            {
                _audioSource.Stop();
                return;
            }

            var clip = _currentQueue.Dequeue();
            _audioSource.clip = clip;
            _audioSource.Play();

            try
            {
                while (_audioSource.isPlaying)
                    await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

                if (_loop && _currentQueue.Count == 0)
                {
                    _currentQueue.Enqueue(clip);
                    PlayNextTrackAsync().Forget();
                }
                else
                {
                    PlayNextTrackAsync().Forget();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async UniTask FadeVolumeAsync(float targetVolume, float duration)
        {
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            await FadeRoutineAsync(_audioSource.volume, targetVolume, duration, _fadeCts.Token);
        }

        private async UniTask FadeRoutineAsync(float from, float to, float duration, CancellationToken token, bool stopAfterFade = false)
        {
            float elapsed = 0f;
            if (!_audioSource.isPlaying && to > 0f)
                _audioSource.Play();

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _audioSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                _audioSource.volume = to;

                if (stopAfterFade)
                    _audioSource.Stop();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelFade()
        {
            if (_fadeCts != null && !_fadeCts.IsCancellationRequested)
            {
                _fadeCts.Cancel();
                _fadeCts.Dispose();
            }
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        public void ResetState()
        {
            Volume = 1;
            Mute = false;
            Loop = false;
            StopAsync(0f).Forget();
            SetPlaylist(Enumerable.Empty<AudioClip>());

        }
    }
}
