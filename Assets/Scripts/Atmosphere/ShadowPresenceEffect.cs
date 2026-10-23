using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WalkingSimulator.Presentation;

// Darkens and cools the image while a Shadow Entity is visibly present.
// Encounters call Begin when the entity is revealed and End when it starts to disappear.
// Only post-processing is changed: lights, camera, FOV and zoom are left alone.
[RequireComponent(typeof(Volume))]
public class ShadowPresenceEffect : MonoBehaviour
{
    [Tooltip("The scene's global night volume. Its motion blur is turned off when Reduced Motion is on.")]
    public Volume globalVolume;

    [Min(.01f)] public float fadeInDuration = .6f;
    [Min(.01f)] public float fadeOutDuration = 1.2f;

    [Header("Lens Distortion Pulse")]
    [Range(-1f, 0f)] public float pulseIntensity = -.18f;
    [Min(.01f)] public float pulseDuration = .35f;

    static readonly List<Object> owners = new List<Object>();

    Volume volume;
    LensDistortion lens;
    MotionBlur motionBlur;

    float blend;
    float pulseTime = -1f;
    bool wasPresent;

    public static void Begin(Object owner)
    {
        if (owner != null && !owners.Contains(owner))
            owners.Add(owner);
    }

    public static void End(Object owner)
    {
        owners.Remove(owner);
    }

    static bool ReducedMotion
    {
        get { return PlayerPrefs.GetInt(GamePreferences.Prefix + "ReducedMotion", 0) == 1; }
    }

    void Awake()
    {
        volume = GetComponent<Volume>();
        volume.weight = 0f;

        if (globalVolume != null)
        {
            // Runtime copy, so the profile asset is never changed in Play Mode.
            VolumeProfile profile = globalVolume.profile;
            profile.TryGet(out lens);
            profile.TryGet(out motionBlur);
        }
    }

    void OnDestroy()
    {
        owners.Clear();
    }

    void Update()
    {
        // An interrupted encounter must never leave the scene darkened.
        owners.RemoveAll(owner =>
            owner == null ||
            (owner is Behaviour && !((Behaviour)owner).isActiveAndEnabled));

        bool present = owners.Count > 0;
        bool reduced = ReducedMotion;

        if (motionBlur != null)
            motionBlur.active = !reduced;

        blend = Mathf.MoveTowards(
            blend,
            present ? 1f : 0f,
            Time.deltaTime / (present ? fadeInDuration : fadeOutDuration)
        );

        volume.weight = blend * blend * (3f - 2f * blend);

        if (present && !wasPresent && !reduced)
            pulseTime = 0f;

        wasPresent = present;

        UpdatePulse(reduced);
    }

    void UpdatePulse(bool reduced)
    {
        if (lens == null || pulseTime < 0f)
            return;

        pulseTime += Time.deltaTime;

        float t = pulseTime / pulseDuration;

        if (t >= 1f || reduced)
        {
            lens.intensity.value = 0f;
            pulseTime = -1f;
            return;
        }

        lens.intensity.value = pulseIntensity * Mathf.Sin(t * Mathf.PI);
    }
}
