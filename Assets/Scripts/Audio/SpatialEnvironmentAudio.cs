using UnityEngine;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SpatialEnvironmentAudio : MonoBehaviour
    {
        public AudioClip loopClip;
        public AudioClip ignitionClip;
        public ParticleSystem activity;
        public bool playWithoutParticles;
        [Range(0f, 1f)] public float volume = .35f;
        public float minimumDistance = 3f;
        public float maximumDistance = 35f;
        public float fadeSeconds = .5f;
        public bool localLinearRolloff;
        AudioSource source;
        float fade;
        bool started;
        bool paused;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.clip = loopClip;
            source.spatialBlend = 1f;
            source.rolloffMode = localLinearRolloff ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
            source.minDistance = minimumDistance;
            source.maxDistance = maximumDistance;
            source.dopplerLevel = 0f;
            source.volume = 0f;
        }

        public void Activate()
        {
            if (source == null) return;
            if (!started && loopClip != null) source.Play();
            if (!started && ignitionClip != null)
                source.PlayOneShot(ignitionClip);
            started = true;
        }

        void Update()
        {
            bool pause = PauseMenu.paused || Time.timeScale == 0f;
            if (pause != paused)
            {
                if (pause) source.Pause();
                else source.UnPause();
                paused = pause;
            }
            if (pause) return;
            bool active = playWithoutParticles || (activity != null &&
                activity.gameObject.activeInHierarchy && activity.isPlaying);
            if (active && !started) Activate();
            fade = Mathf.MoveTowards(fade, active ? 1f : 0f,
                Time.deltaTime / Mathf.Max(.05f, fadeSeconds));
            source.volume = fade * volume * AudioPreferences.SfxVolume;
            if (!active && fade == 0f && started)
            {
                source.Stop();
                started = false;
            }
        }

        void OnDisable()
        {
            if (source != null) source.Stop();
            started = false;
            fade = 0f;
        }
    }
}
