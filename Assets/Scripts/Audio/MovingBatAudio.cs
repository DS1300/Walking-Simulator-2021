using UnityEngine;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MovingBatAudio : MonoBehaviour
    {
        public AudioClip swarmClip;

        [Range(0f, 1f)]
        public float volume = .2f;

        public ParticleSystem swarm;

        AudioSource source;
        AudioListener listener;

        float fade;
        bool paused;

        void Awake()
        {
            source = GetComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.clip = swarmClip;
            source.volume = 0f;

            listener = FindObjectOfType<AudioListener>();

            if (swarm == null)
                swarm = GetComponent<ParticleSystem>();
        }

        void Update()
        {
            bool isPaused =
                PauseMenu.paused || Time.timeScale <= 0f;

            if (isPaused != paused)
            {
                if (isPaused)
                    source.Pause();
                else
                    source.UnPause();

                paused = isPaused;
            }

            if (isPaused)
                return;

            if (listener == null)
                listener = FindObjectOfType<AudioListener>();

            bool hasParticles =
                swarm != null &&
                swarm.gameObject.activeInHierarchy &&
                swarm.particleCount > 0;

            bool inRange = false;

            if (listener != null)
            {
                float maxDistance = source.maxDistance;

                inRange =
                    (listener.transform.position - transform.position)
                    .sqrMagnitude <= maxDistance * maxDistance;
            }

            bool shouldPlay =
                swarmClip != null &&
                hasParticles &&
                inRange;

            fade = Mathf.MoveTowards(
                fade,
                shouldPlay ? 1f : 0f,
                Time.deltaTime * 0.5f
            );

            source.volume =
                volume * fade * AudioPreferences.SfxVolume;

            if (shouldPlay)
            {
                if (!source.isPlaying)
                {
                    source.clip = swarmClip;
                    source.Play();
                }
            }
            else if (fade <= 0f && source.isPlaying)
            {
                source.Stop();
            }
        }

        void OnDisable()
        {
            if (source != null)
                source.Stop();

            fade = 0f;
            paused = false;
        }
    }
}

