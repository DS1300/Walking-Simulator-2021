using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class EncounterSilence : MonoBehaviour
{
    public EncounterAudio cinematicAudio;

    class SourceState
    {
        public AudioSource source;
        public float volume;
    }

    readonly List<SourceState> sources = new List<SourceState>();
    bool active;
    float level = 1f;
    float nextScan;

    void Scan()
    {
        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            if (cinematicAudio != null &&
                source.GetComponentInParent<EncounterAudio>() == cinematicAudio) continue;
            bool known = false;
            foreach (var state in sources)
                if (state.source == source) { known = true; break; }
            if (!known) sources.Add(new SourceState { source = source, volume = source.volume });
        }
    }

    public IEnumerator FadeToSilence(float seconds)
    {
        active = true;
        Scan();
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            level = 1f - Mathf.Clamp01(elapsed / Mathf.Max(.01f, seconds));
            yield return null;
        }
        level = 0f;
    }

    void Update()
    {
        if (!active) return;
        // Restore first so existing audio scripts can apply their normal settings.
        foreach (var state in sources)
            if (state.source != null) state.source.volume = state.volume;
        if (Time.unscaledTime >= nextScan)
        {
            Scan();
            nextScan = Time.unscaledTime + .5f;
        }
    }

    void LateUpdate()
    {
        if (!active) return;
        foreach (var state in sources)
        {
            if (state.source == null) continue;
            state.volume = state.source.volume;
            state.source.volume *= level;
        }
    }

    public void Restore()
    {
        active = false;
        foreach (var state in sources)
            if (state.source != null) state.source.volume = state.volume;
        sources.Clear();
        level = 1f;
    }

    void OnDisable() { Restore(); }
}
