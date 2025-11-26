using SNEngine.Audio;
using SNEngine.Audio.Models;
using SNEngine.Debugging;
using SNEngine.Polling;
using System;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Audio Service")]
    public class AudioService : ServiceBase
    {
        private PoolMono<AudioObject> _audioObjects;
        [SerializeField, Min(1)] private int _sizePool = 9;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnFXVolumeChanged;

        public event Action<bool> OnMusicMuteChanged;
        public event Action<bool> OnFXMuteChanged;

        public AudioData AudioData => NovelGame.Instance.GetService<UserDataService>().Data.AudioData;

        public override void Initialize()
        {
            AudioObject _prefab = Resources.Load<AudioObject>("Audio/AudioObject");
            Transform container = new GameObject($"{nameof(AudioObject)}_Container").transform;
            DontDestroyOnLoad(container.gameObject);
            _audioObjects = new PoolMono<AudioObject>(_prefab, container, _sizePool, true);
        }

        public IAudioObject PlaySound (AudioClip clip)
        {
            var newSound = GetFreeAudioObject();
            newSound.CurrentSound = clip;
            newSound.Play();
            return newSound;
        }

        public void StopSound (IAudioObject audioObject) => audioObject?.Stop();
        public void SetMuteSoundState (IAudioObject audioObject, bool mute)
        {
            if (audioObject is null)
            {
                NovelGameDebug.LogError("audio object is null");
                return;
            }
            audioObject.Mute = mute;
        }

        public IAudioObject GetFreeAudioObject ()
        {
            var element = _audioObjects.GetFreeElement();
            element.gameObject.SetActive(true);
            return element;
        }

        public void SetVolumeMusic (float volume)
        {
            AudioData.MusicVolumw = Mathf.Clamp01((float)volume);
            OnMusicVolumeChanged?.Invoke(volume);
        }

        public void SetVolumeFX(float volume)
        {
            AudioData.FXVolume = Mathf.Clamp01((float)volume);
            OnFXVolumeChanged?.Invoke(volume);
        }

        public override void ResetState()
        {
            foreach (var audio in _audioObjects.Objects)
            {
               audio.ResetState();
            }
        }
    }
}
