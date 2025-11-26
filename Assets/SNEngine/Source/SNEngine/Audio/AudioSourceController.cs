using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SNEngine.Audio.Music;
using SNEngine.Debugging;
using SNEngine.Services;
using System;
using UnityEngine;

namespace SNEngine.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceController : MonoBehaviour
    {
        private AudioType? _type;
        private AudioService _service;
        [SerializeField, ReadOnly] private AudioSource _audioSource;

        private void OnEnable()
        {

            if (!_service)
            {
                _service = NovelGame.Instance.GetService<AudioService>();
            }
            if (_type is null)
            {
                _type = TryGetComponent(out MusicPlayer _) ? AudioType.Music : AudioType.FX;
            }
            switch (_type.Value)
            {
                case AudioType.Music:
                    _service.OnMusicMuteChanged += OnMusicMuteChanged;
                    _service.OnMusicVolumeChanged += OnMusicVolumeChanged;
                    break;
                case AudioType.FX:
                    _service.OnFXMuteChanged += OnFXMuteChanged;
                    _service.OnFXVolumeChanged += OnFXVolumeChanged;
                    break;
                default:
                    NovelGameDebug.LogError($"unkown type of audio type: {_type.Value}");
                    break;
            }
        }

        private void OnDisable()
        {
            switch (_type.Value)
            {
                case AudioType.Music:
                    _service.OnMusicMuteChanged -= OnMusicMuteChanged;
                    _service.OnMusicVolumeChanged -= OnMusicVolumeChanged;
                    break;
                case AudioType.FX:
                    _service.OnFXMuteChanged -= OnFXMuteChanged;
                    _service.OnFXVolumeChanged -= OnFXVolumeChanged;
                    break;
                default:
                    NovelGameDebug.LogError($"unkown type of audio type: {_type.Value}");
                    break;
            }
        }

        private void OnValidate()
        {
            if (!_audioSource)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnFXVolumeChanged(float value)
        {
            _audioSource.volume = value;
        }

        private void OnMusicVolumeChanged(float volume)
        {
            _audioSource.volume = volume;
        }

        private void OnFXMuteChanged(bool mute)
        {
            _audioSource.mute = mute;
        }

        private void OnMusicMuteChanged(bool mute)
        {
            _audioSource.mute = mute;
        }
    }

}
