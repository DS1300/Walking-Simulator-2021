
using System.Collections;
using UnityEngine;
using ShadowEntity;

public class ShadowEncounterActor : MonoBehaviour
{
    public ShadowEntityController entity;
    public EncounterAudio sounds;
    public Transform lookTarget;

    public float appearDuration = 2f;
    public float disappearDuration = 2.8f;

    [Header("Appearance Fix")]
    [Tooltip("Force the Animator into Hidden before Appear. Enable only for encounters that pop into view instead of materializing.")]
    public bool forceHiddenBeforeAppear = false;

    public IEnumerator ActivateHidden()
    {
        if (entity == null)
            yield break;

        entity.gameObject.SetActive(true);

        Animator animator = entity.GetComponent<Animator>();

        if (forceHiddenBeforeAppear && animator != null)
        {
            // Explicitly prepare the original Hidden state
            // before triggering the smoke appearance.
            animator.speed = 1f;
            animator.ResetTrigger("Appear");
            animator.ResetTrigger("Disappear");
            animator.Play("Hidden", 0, 0f);

            // Evaluate immediately to avoid one visible
            // frame in Idle after reactivating the entity.
            animator.Update(0f);
        }

        yield return null;

        entity.SetLookUp(false);
    }

    public IEnumerator Appear(
        EncounterSound sound,
        float duration)
    {
        yield return ActivateHidden();

        if (entity == null)
            yield break;

        Animator animator = entity.GetComponent<Animator>();

        if (animator == null)
            yield break;

        float previousSpeed = animator.speed;

        animator.speed =
            2f / Mathf.Max(.1f, duration);

        if (sounds != null)
            sounds.Play(sound);

        entity.PlayAppear();

        yield return new WaitForSeconds(duration);

        animator.speed = previousSpeed;
    }

    public IEnumerator Glitch(
        EncounterSound sound,
        bool aggressive,
        float intensity,
        float duration)
    {
        if (sounds != null)
            sounds.Play(sound);

        if (aggressive)
            entity.PlayAggressiveGlitch(intensity, duration);
        else
            entity.PlayGlitch(intensity, duration);

        yield return new WaitForSeconds(duration + .1f);
    }

    public IEnumerator HeadTilt(
        EncounterSound sound,
        float duration)
    {
        if (sounds != null)
            sounds.Play(sound);

        entity.PlayHeadTilt();

        yield return new WaitForSeconds(duration);
    }

    public IEnumerator Disappear(EncounterSound sound)
    {
        if (sounds != null)
            sounds.Play(sound);

        entity.PlayDisappear();

        yield return new WaitForSeconds(
            Mathf.Max(1.3f, disappearDuration)
        );
    }

    public void Face(Transform target)
    {
        if (entity == null || target == null)
            return;

        Vector3 direction =
            target.position - entity.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > .001f)
        {
            entity.transform.rotation =
                Quaternion.LookRotation(direction);
        }
    }

    public void HideImmediately()
    {
        if (entity == null)
            return;

        Animator animator = entity.GetComponent<Animator>();

        if (animator != null)
            animator.speed = 1f;

        entity.gameObject.SetActive(false);

        if (sounds != null)
            sounds.StopSounds();
    }
}
