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
            float target = pauseMusic ? 0 : Mathf.Clamp01(musicVolume);
            source.volume = Mathf.MoveTowards(source.volume, target,
                Time.unscaledDeltaTime * Mathf.Max(musicVolume, .01f) / Mathf.Max(fadeSeconds, .01f));
            if (pauseMusic && !paused && source.volume <= 0)
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
