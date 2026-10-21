

using UnityEngine;
using UnityEngine.SceneManagement;

namespace WalkingSimulator.Presentation
{
    public sealed class AudioSceneBootstrap : MonoBehaviour
    {
        private AudioSource forestSource;
        private AudioSource cricketsSource;

        private const float ForestVolume = 0.12f;
        private const float CricketsVolume = 0.06f;

        private bool wasPaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
             SceneManager.sceneLoaded -= OnSceneLoaded;
             SceneManager.sceneLoaded += OnSceneLoaded;

             if (FindObjectOfType<AudioSceneBootstrap>() == null)
             new GameObject("Audio Scene Bootstrap")
            .AddComponent<AudioSceneBootstrap>();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindObjectOfType<AudioSceneBootstrap>() == null)
             new GameObject("Audio Scene Bootstrap")
            .AddComponent<AudioSceneBootstrap>();
        }

        private void Start()
        {
            forestSource = CreateAmbience("forestAmbience", ForestVolume);
            cricketsSource = CreateAmbience("crickets", CricketsVolume);

            var player = GameObject.Find("Player X");

            if (player != null &&
                player.GetComponent<CharacterController>() != null &&
                player.GetComponent<SurfaceFootstepAudio>() == null)
            {
                player.AddComponent<SurfaceFootstepAudio>();
            }
        }

        private AudioSource CreateAmbience(string clipName, float volume)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);

            if (clip == null)
            {
                Debug.LogWarning("Missing ambience clip: " + clipName);
                return null;
            }

            GameObject audioObject = new GameObject(clipName);
            audioObject.transform.SetParent(transform, false);

            AudioSource source = audioObject.AddComponent<AudioSource>();

            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume * AudioPreferences.SfxVolume;

            source.Play();

            return source;
        }

        private void Update()
        {
            if (forestSource != null)
            {
                forestSource.volume =
                    ForestVolume * AudioPreferences.SfxVolume;
            }

            if (cricketsSource != null)
            {
                cricketsSource.volume =
                    CricketsVolume * AudioPreferences.SfxVolume;
            }

            bool isPaused = PauseMenu.paused;

            if (isPaused == wasPaused)
                return;

            wasPaused = isPaused;

            if (isPaused)
            {
                if (forestSource != null)
                    forestSource.Pause();

                if (cricketsSource != null)
                    cricketsSource.Pause();
            }
            else
            {
                if (forestSource != null)
                    forestSource.UnPause();

                if (cricketsSource != null)
                    cricketsSource.UnPause();
            }
        }
    }
}
