using UnityEngine;

namespace SNEngine.Audio
{
    public interface IAudioObject : IResetable
    {
        bool Mute {  get; set; }
        bool Loop { get; set; }
        AudioClip CurrentSound { get; set; }
        void Play();
        void Stop();
        void PlayOneShot(AudioClip clip, float volumeScale = 1f);
    }
}