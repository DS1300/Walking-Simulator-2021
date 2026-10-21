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
            source.volume = envelope * Mathf.Clamp01(musicVolume) * AudioPreferences.MusicVolume;
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

