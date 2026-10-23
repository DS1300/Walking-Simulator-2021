using System.Collections;
using UnityEngine;

namespace WalkingSimulator.Presentation
{
    // Engine switching off, a short pause, then the car door: the protagonist has just parked.
    // After the opening story, OpeningStory claims it and plays it while the screen is still black and
    // controls are held. When the scene starts without the story (Editor), it plays on its own.
    [RequireComponent(typeof(SfxAudioSource))]
    public sealed class CarArrivalAudio : MonoBehaviour
    {
        public AudioClip engineOff;
        public AudioClip carDoor;
        [Min(0f)] public float startDelay = .6f;
        [Tooltip("Seconds from the start of the engine clip to the door.")]
        [Min(0f)] public float doorDelay = 5f;
        [Tooltip("Seconds of black screen kept after the door clip ends.")]
        [Min(0f)] public float endDelay = .3f;
        [Range(0f, 1f)] public float engineLevel = .8f;
        [Range(0f, 1f)] public float doorLevel = .7f;

        bool claimed, played;

        public void Claim()
        {
            claimed = true;
        }

        void Start()
        {
            if (!claimed) StartCoroutine(PlaySequence());
        }

        // Uses real time, so it also runs while gameplay is held at timeScale 0.
        public IEnumerator PlaySequence()
        {
            if (played) yield break;
            played = true;
            var source = GetComponent<SfxAudioSource>();
            yield return new WaitForSecondsRealtime(startDelay);
            source.Play(engineOff, engineLevel);
            yield return new WaitForSecondsRealtime(doorDelay);
            source.Play(carDoor, doorLevel);
            yield return new WaitForSecondsRealtime((carDoor != null ? carDoor.length : 0f) + endDelay);
        }
    }
}
