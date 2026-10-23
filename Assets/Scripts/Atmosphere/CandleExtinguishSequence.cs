using System.Collections;
using UnityEngine;

// Puts out candles one left/right pair at a time, like something passing through the room.
// Encounters start it with Play(); the candles stay out until the scene is reloaded.
public class CandleExtinguishSequence : MonoBehaviour
{
    [System.Serializable]
    public class Candle
    {
        public ParticleSystem flame;
        public Light light;
    }

    [System.Serializable]
    public class CandlePair
    {
        public Candle left = new Candle();
        public Candle right = new Candle();
    }

    [Tooltip("Played in order. Either side of a pair may be left empty.")]
    public CandlePair[] pairs;

    [Header("Timing")]
    [Min(0f)] public float startDelay = .8f;
    [Min(0f)] public float pairInterval = .45f;
    [Min(0f)] public float intervalVariation = .08f;
    [Min(.01f)] public float lightFadeDuration = .3f;

    [Header("Smoke")]
    public ParticleSystem smokePrefab;
    public Vector3 smokeOffset = new Vector3(0f, .02f, 0f);

    [Header("Audio")]
    [Tooltip("One soft puff per pair, at the pair's centre. The same clip is not used twice in a row.")]
    public AudioClip[] extinguishSounds;
    [Range(0f, 1f)] public float extinguishVolume = .2f;

    bool played;
    int lastSound = -1;

    public void Play()
    {
        if (played)
            return;

        played = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < pairs.Length; i++)
        {
            Extinguish(pairs[i].left);
            Extinguish(pairs[i].right);
            PlayPairSound(pairs[i]);

            if (i < pairs.Length - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f,
                    pairInterval + Random.Range(-intervalVariation, intervalVariation)));
            }
        }
    }

    void Extinguish(Candle candle)
    {
        if (candle == null)
            return;

        if (candle.flame != null)
        {
            candle.flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (smokePrefab != null)
            {
                Instantiate(
                    smokePrefab,
                    candle.flame.transform.position + smokeOffset,
                    Quaternion.identity
                );
            }
        }

        if (candle.light != null)
            StartCoroutine(FadeLight(candle.light));
    }

    void PlayPairSound(CandlePair pair)
    {
        if (extinguishSounds == null || extinguishSounds.Length == 0)
            return;

        int count = 0;
        Vector3 position = Vector3.zero;
        foreach (var candle in new[] { pair.left, pair.right })
        {
            if (candle != null && candle.flame != null)
            {
                position += candle.flame.transform.position;
                count++;
            }
        }

        if (count == 0)
            return;

        int index = Random.Range(0, extinguishSounds.Length);
        if (index == lastSound && extinguishSounds.Length > 1)
            index = (index + 1) % extinguishSounds.Length;
        lastSound = index;

        AudioSource.PlayClipAtPoint(extinguishSounds[index], position / count,
            extinguishVolume * WalkingSimulator.Presentation.AudioPreferences.SfxVolume);
    }

    IEnumerator FadeLight(Light candleLight)
    {
        float start = candleLight.intensity;

        for (float t = 0f; t < lightFadeDuration; t += Time.deltaTime)
        {
            candleLight.intensity = Mathf.Lerp(start, 0f, t / lightFadeDuration);
            yield return null;
        }

        candleLight.intensity = 0f;
        candleLight.enabled = false;
    }
}
