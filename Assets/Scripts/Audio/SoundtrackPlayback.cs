using UnityEngine;
using UnityEngine.SceneManagement;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundtrackPlayback : MonoBehaviour
    {
        public float musicVolume = .25f;
        public float fadeSeconds = .35f;
        static SoundtrackPlayback instance;
        AudioSource source;
        bool paused;
        float envelope;
        float duckRemaining;
        float duckTarget = 1f;
        float duckEnvelope = 1f;
        [Min(.01f)] public float duckAttack = .15f;
        [Min(.01f)] public float duckRecovery = 1.2f;

        public static void DuckFor(float seconds, float level)
        {
            if (instance == null) return;
            instance.duckTarget = instance.duckRemaining > 0f
                ? Mathf.Min(instance.duckTarget, Mathf.Clamp01(level)) : Mathf.Clamp01(level);
            instance.duckRemaining = Mathf.Max(instance.duckRemaining, seconds);
        }

        void Awake()
        {
            source = GetComponent<AudioSource>();
            if (instance != null && instance != this)
            {
                source.Stop();
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            source.volume = 0;
            source.Play();
        }

        void Update()
        {
            if (instance != this) return;
            bool pauseMusic = SceneManager.GetActiveScene().name == "The Game" && PauseMenu.paused && !MainMenu.AudioSettingsOpen;
            if (!pauseMusic && paused)
            {
                source.UnPause();
                paused = false;
            }
            float target = pauseMusic ? 0f : 1f;
            envelope = Mathf.MoveTowards(envelope, target,
                Time.unscaledDeltaTime / Mathf.Max(fadeSeconds, .01f));
            if (!PauseMenu.paused && Time.timeScale > 0f)
                duckRemaining = Mathf.Max(0f, duckRemaining - Time.deltaTime);
            float duck = duckRemaining > 0f ? duckTarget : 1f;
            float duration = duck < duckEnvelope ? duckAttack : duckRecovery;
            duckEnvelope = Mathf.MoveTowards(duckEnvelope, duck,
                Time.unscaledDeltaTime / Mathf.Max(.01f, duration));
            source.volume = envelope * duckEnvelope * Mathf.Clamp01(musicVolume) * AudioPreferences.MusicVolume;
            if (pauseMusic && !paused && envelope <= 0)
            {
                source.Pause();
                paused = true;
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}

