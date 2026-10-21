using UnityEngine;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ForestOwlAudio : MonoBehaviour
    {
        public AudioClip call;
        public Transform[] perches;
        public Vector2 interval = new Vector2(45f, 110f);
        [Range(0f, 1f)] public float volume = .32f;
        AudioSource source;
        float nextCall;
        float callLevel = 1f;
        bool left;
        bool paused;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            left = Random.value < .5f;
            nextCall = Time.time + Random.Range(interval.x, interval.y);
        }

        void Update()
        {
            source.volume = volume * callLevel * AudioPreferences.SfxVolume;
            bool pause = PauseMenu.paused || Time.timeScale <= 0f;
            if (pause != paused)
            {
                if (pause) source.Pause();
                else source.UnPause();
                paused = pause;
            }
            if (pause || call == null || source.isPlaying || Time.time < nextCall) return;
            source.panStereo = (left ? -1f : 1f) * Random.Range(.45f, .8f);
            left = !left;
            callLevel = Random.Range(.75f, 1f);
            source.volume = volume * callLevel * AudioPreferences.SfxVolume;
            source.pitch = Random.Range(.97f, 1.03f);
            source.PlayOneShot(call);
            nextCall = Time.time + call.length / source.pitch + Random.Range(interval.x, interval.y);
        }
    }
}
