using System.Collections;
using UnityEngine;
using WalkingSimulator.Presentation;

[System.Serializable]
public class EncounterSound
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = .7f;
    [Min(0f)] public float delay;
    [Range(0f, 1f)] public float spatialBlend;
    [Min(0f)] public float fadeIn;
}

public class EncounterAudio : MonoBehaviour
{
    public AudioSource[] voices;
    public float minimumDistance = 2f;
    public float maximumDistance = 35f;
    [Range(0f, 1f)] public float stingMusicLevel = .18f;

    float[] levels;
    float[] fades;
    float[] ages;
    bool[] busy;
    bool paused;
    float envelope = 1f;

    void Awake()
    {
        int count = voices.Length;
        levels = new float[count];
        fades = new float[count];
        ages = new float[count];
        busy = new bool[count];
        foreach (var voice in voices)
        {
            voice.playOnAwake = false;
            voice.loop = false;
            voice.dopplerLevel = 0f;
            voice.minDistance = minimumDistance;
            voice.maxDistance = maximumDistance;
            voice.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    public void Play(EncounterSound sound)
    {
        if (sound != null && sound.clip != null)
            StartCoroutine(PlayDelayed(sound));
    }

    public void PlaySting(EncounterSound sound)
    {
        if (sound != null && sound.clip != null)
            StartCoroutine(PlayDelayed(sound, true));
    }

    public void PlayStingWhenSeen(EncounterSound sound, Camera camera,
        Transform target, Transform creature, float revealDelay = 0f, float timeout = 5f)
    {
        StartCoroutine(WaitForSight(sound, camera, target, creature, revealDelay, timeout));
    }

    IEnumerator WaitForSight(EncounterSound sound, Camera camera,
        Transform target, Transform creature, float revealDelay, float timeout)
    {
        if (revealDelay > 0f) yield return new WaitForSeconds(revealDelay);
        float elapsed = 0f;
        while (elapsed < timeout && camera != null && target != null && creature != null)
        {
            if (!PauseMenu.paused && Time.timeScale > 0f)
            {
                elapsed += Time.deltaTime;
                Vector3 screen = camera.WorldToViewportPoint(target.position);
                Vector3 direction = target.position - camera.transform.position;
                bool visible = creature.gameObject.activeInHierarchy && screen.z > 0f &&
                    screen.x > .05f && screen.x < .95f && screen.y > .05f && screen.y < .95f;
                if (visible)
                {
                    foreach (var hit in Physics.RaycastAll(camera.transform.position,
                        direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.IsChildOf(creature) ||
                            hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                        visible = false;
                        break;
                    }
                }
                if (visible)
                {
                    PlaySting(sound);
                    yield break;
                }
            }
            yield return null;
        }
    }

    IEnumerator PlayDelayed(EncounterSound sound, bool sting = false)
    {
        if (sound.delay > 0f) yield return new WaitForSeconds(sound.delay);
        while (PauseMenu.paused) yield return null;
        int index = -1;
        for (int i = 0; i < voices.Length; i++)
            if (!busy[i]) { index = i; break; }
        if (index < 0) yield break;

        busy[index] = true;
        levels[index] = sound.volume;
        fades[index] = sound.fadeIn;
        ages[index] = 0f;
        voices[index].clip = sound.clip;
        voices[index].spatialBlend = sting ? 0f : sound.spatialBlend;
        if (sting)
        {
            voices[index].spatialize = false;
            voices[index].panStereo = 0f;
            voices[index].pitch = 1f;
            voices[index].priority = 0;
            SoundtrackPlayback.DuckFor(sound.clip.length, stingMusicLevel);
        }
        voices[index].volume = sound.fadeIn > 0f ? 0f :
            sound.volume * AudioPreferences.SfxVolume * envelope;
        voices[index].Play();
    }

    void Update()
    {
        bool pause = PauseMenu.paused || Time.timeScale <= 0f;
        for (int i = 0; i < voices.Length; i++)
        {
            if (pause != paused && busy[i])
            {
                if (pause) voices[i].Pause();
                else voices[i].UnPause();
            }
            if (!pause && busy[i])
            {
                ages[i] += Time.deltaTime;
                if (!voices[i].isPlaying) busy[i] = false;
            }
            float fade = fades[i] <= 0f ? 1f : Mathf.Clamp01(ages[i] / fades[i]);
            voices[i].volume = levels[i] * fade * envelope * AudioPreferences.SfxVolume;
        }
        paused = pause;
    }

    public IEnumerator FadeOut(float seconds)
    {
        float start = envelope;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            envelope = Mathf.Lerp(start, 0f, elapsed / Mathf.Max(.01f, seconds));
            yield return null;
        }
        envelope = 0f;
    }

    public void StopSounds()
    {
        StopAllCoroutines();
        if (voices != null)
            foreach (var voice in voices) if (voice != null) voice.Stop();
        if (busy != null) System.Array.Clear(busy, 0, busy.Length);
        envelope = 1f;
        paused = false;
    }

    void OnDisable() { StopSounds(); }
}
