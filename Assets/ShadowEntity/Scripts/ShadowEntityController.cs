using UnityEngine;

namespace ShadowEntity
{
    /// <summary>
    /// Optional, presentation-only helper for the Shadow Entity prefab.
    /// Everything also works by setting the Animator parameters directly:
    /// LookUp (bool), Appear / Disappear / Glitch / GlitchAggressive / HeadTilt / FinalLunge (triggers), GlitchSpeed (float).
    /// No AI, movement, detection or camera logic lives here.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class ShadowEntityController : MonoBehaviour
    {
        [Tooltip("Start invisible. Call PlayAppear() to materialize.")]
        public bool startHidden = false;
        [Tooltip("Start in the upward-staring LookUp pose.")]
        public bool startLookingUp = false;

        [Header("Glitch defaults")]
        [Range(0f, 1f)] public float glitchIntensity = 1f;
        [Min(0.1f)] public float glitchDuration = 1.5f;
        [Min(0.1f)] public float aggressiveGlitchDuration = 2.5f;

        static readonly int LookUpId = Animator.StringToHash("LookUp");
        static readonly int AppearId = Animator.StringToHash("Appear");
        static readonly int DisappearId = Animator.StringToHash("Disappear");
        static readonly int GlitchId = Animator.StringToHash("Glitch");
        static readonly int GlitchAggressiveId = Animator.StringToHash("GlitchAggressive");
        static readonly int GlitchSpeedId = Animator.StringToHash("GlitchSpeed");
        static readonly int HeadTiltId = Animator.StringToHash("HeadTilt");
        static readonly int FinalLungeId = Animator.StringToHash("FinalLunge");

        // Clip lengths in seconds (Glitch 45 frames, GlitchAggressive 75 frames at 30 fps).
        const float GlitchClipLength = 1.5f;
        const float AggressiveClipLength = 2.5f;

        Animator animator;
        int glitchLayer = -1;
        bool hidden;

        public bool IsHidden => hidden;
        public bool IsLookingUp => animator != null && animator.GetBool(LookUpId);

        void Awake()
        {
            animator = GetComponent<Animator>();
            glitchLayer = animator.GetLayerIndex("Glitch");
        }

        void Start()
        {
            SetLookUp(startLookingUp);
            if (startHidden)
            {
                animator.Play("Hidden", 0, 0f);
                hidden = true;
            }
        }

        /// <summary>Stare upward (true) or return to the normal floating idle (false).</summary>
        public void SetLookUp(bool enabled)
        {
            animator.SetBool(LookUpId, enabled);
        }

        /// <summary>Materialize from smoke. Only plays while hidden.</summary>
        public void PlayAppear()
        {
            animator.ResetTrigger(DisappearId);
            animator.SetTrigger(AppearId);
            hidden = false;
        }

        /// <summary>Dissolve rapidly into smoke and stay hidden.</summary>
        public void PlayDisappear()
        {
            animator.ResetTrigger(AppearId);
            animator.SetTrigger(DisappearId);
            hidden = true;
        }

        /// <summary>Supernatural glitch burst using the Inspector defaults.</summary>
        public void PlayGlitch()
        {
            PlayGlitch(glitchIntensity, glitchDuration);
        }

        /// <summary>Glitch burst. intensity 0..1 scales the distortion, duration in seconds.</summary>
        public void PlayGlitch(float intensity, float duration)
        {
            StartGlitch(GlitchId, intensity, GlitchClipLength / Mathf.Max(0.1f, duration));
        }

        /// <summary>Stronger, longer glitch for a final close-range scare.</summary>
        public void PlayAggressiveGlitch()
        {
            PlayAggressiveGlitch(glitchIntensity, aggressiveGlitchDuration);
        }

        public void PlayAggressiveGlitch(float intensity, float duration)
        {
            StartGlitch(GlitchAggressiveId, intensity, AggressiveClipLength / Mathf.Max(0.1f, duration));
        }

        /// <summary>Slow, unnatural sideways head tilt. Plays on top of Idle or LookUp.</summary>
        public void PlayHeadTilt()
        {
            animator.SetTrigger(HeadTiltId);
        }

        /// <summary>Final scare: raises both arms, reaches and lunges about 1.2 m along the prefab's
        /// forward (+Z) while glitching violently, then holds. Only the visual children move.</summary>
        public void PlayFinalLunge()
        {
            animator.SetTrigger(FinalLungeId);
            hidden = false;
        }

        /// <summary>Snap straight back to the visible floating idle (e.g. after FinalLunge).</summary>
        public void ResetToIdle()
        {
            foreach (var t in new[] { AppearId, DisappearId, GlitchId, GlitchAggressiveId, HeadTiltId, FinalLungeId }) animator.ResetTrigger(t);
            animator.Play(IsLookingUp ? "LookUp" : "Idle", 0, 0f);
            hidden = false;
        }

        void StartGlitch(int trigger, float intensity, float speed)
        {
            if (glitchLayer >= 0) animator.SetLayerWeight(glitchLayer, Mathf.Clamp01(intensity));
            animator.SetFloat(GlitchSpeedId, speed);
            animator.SetTrigger(trigger);
        }
    }
}
