
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BridgeShadowEncounter : MonoBehaviour
{
    [Header("Bridge Trigger")]
    public bool encounterEnabled = true;
    public Camera playerCamera;

    [Header("Independent Shadow Entity")]
    public GameObject bridgeEntity;
    public Transform bridgeStart;
    public Transform bridgeEnd;

    [Header("Timing")]
    [Min(0.5f)]
    public float crossingDuration = 2.6f;

    [Min(0f)]
    public float disappearDuration = 1.2f;

    [Header("Optional Audio")]
    public AudioSource audioSource;
    public AudioClip horrorSting;
    public AudioClip disappearSound;

    public bool HasPlayed
    {
        get { return played; }
    }

    BoxCollider approach;
    bool played;

    void Awake()
    {
        approach = GetComponent<BoxCollider>();
        approach.isTrigger = true;

        if (bridgeEntity != null)
            bridgeEntity.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            TryStart();
    }

    void Update()
    {
        if (played || !encounterEnabled || !approach.enabled)
            return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            return;

        // Fallback for CharacterController-based player movement.
        // Only the player's position inside the trigger matters.
        Vector3 local =
            approach.transform.InverseTransformPoint(
                playerCamera.transform.position
            ) - approach.center;

        Vector3 half = approach.size * 0.5f;

        if (Mathf.Abs(local.x) <= half.x &&
            Mathf.Abs(local.y) <= half.y &&
            Mathf.Abs(local.z) <= half.z)
        {
            TryStart();
        }
    }

    void TryStart()
    {
        if (played ||
            !encounterEnabled ||
            bridgeEntity == null ||
            bridgeStart == null ||
            bridgeEnd == null)
        {
            return;
        }

        Vector3 direction = bridgeEnd.position - bridgeStart.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        played = true;
        StartCoroutine(PlayCrossing());
    }

    IEnumerator PlayCrossing()
    {
        Transform entity = bridgeEntity.transform;

        Vector3 start = bridgeStart.position;
        Vector3 end = bridgeEnd.position;

        Vector3 direction = end - start;
        direction.y = 0f;

        entity.position = start;
        entity.rotation = Quaternion.LookRotation(direction);

        bridgeEntity.SetActive(true);

        Animator animator = bridgeEntity.GetComponent<Animator>();

        if (animator != null)
        {
            animator.enabled = true;
            animator.applyRootMotion = false;

            // Reset all previous animation and material state.
            animator.Rebind();
            animator.Update(0f);

            // Disable additive glitch effects for this crossing.
            int glitchLayer = animator.GetLayerIndex("Glitch");

            if (glitchLayer >= 0)
                animator.SetLayerWeight(glitchLayer, 0f);

            int headTiltLayer = animator.GetLayerIndex("HeadTilt");

            if (headTiltLayer >= 0)
                animator.SetLayerWeight(headTiltLayer, 0f);

            // Begin in the visible state, not Hidden or Appear.
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);

            // Play the actual lunge immediately.
            animator.Play("FinalLunge", 0, 0f);
            animator.Update(0f);
        }

        if (audioSource != null && horrorSting != null)
            audioSource.PlayOneShot(horrorSting);

        ShadowPresenceEffect.Begin(this);

        float elapsed = 0f;
        float duration = Mathf.Max(0.5f, crossingDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            entity.position = Vector3.Lerp(start, end, t);

            yield return null;
        }

        entity.position = end;

        ShadowPresenceEffect.End(this);

        // The creature only dissolves after completing the crossing.
        if (animator != null)
            animator.Play("Disappear", 0, 0f);

        if (audioSource != null && disappearSound != null)
            audioSource.PlayOneShot(disappearSound);

        if (disappearDuration > 0f)
            yield return new WaitForSeconds(disappearDuration);

        bridgeEntity.SetActive(false);
    }
}
