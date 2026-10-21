using UnityEngine;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxAudioSource : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 1f;
        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        public void Play(AudioClip clip, float level = 1f)
        {
            if (clip == null) return;
            source.PlayOneShot(clip, volume * level * AudioPreferences.SfxVolume);
        }

        public void PlayLoop(AudioClip clip, float level = 1f)
        {
            if (clip == null) return;
            source.clip = clip;
            source.loop = true;
            source.volume = volume * level * AudioPreferences.SfxVolume;
            if (!source.isPlaying) source.Play();
        }

        public void StopLoop()
        {
            if (source != null) source.Stop();
        }

        void Update()
        {
            if (source != null && source.loop) source.volume = volume * AudioPreferences.SfxVolume;
        }
    }
}
