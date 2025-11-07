using SNEngine.Audio;
using SNEngine.Debugging;
using SNEngine.Polling;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Audio Service")]
    public class AudioService : ServiceBase
    {
        private PoolMono<AudioObject> _audioObjects;
        [SerializeField, Min(1)] private int _sizePool = 9;

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

        public override void ResetState()
        {
            foreach (var audio in _audioObjects.Objects)
            {
               audio.ResetState();
            }
        }
    }
}
