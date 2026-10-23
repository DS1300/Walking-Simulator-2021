
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShadowEntity;

public class TowerShadowEncounter : MonoBehaviour
{
    [Header("Existing scene references")]
    public Key2 towerKey;
    public BoxCollider unlockedDoor;
    public PlayerController player;
    public Camera playerCamera;
    public Collider staircase;
    public Collider upperPlatform;
    public BoxCollider ascentZone;
    public BoxCollider landingTrigger;
    public Transform stairExit;
    public Transform cameraFocus;
    public ShadowEntityController entity;

    [Header("Automatic step before reveal")]
    [Range(0f, 2f)]
    public float forwardStepDistance = 1f;

    public float forwardStepDuration = .75f;

    [Tooltip("Optional marker for the exact final player position.")]
    public Transform forwardStepTarget;

    [Tooltip("Stop if no walkable surface is detected beneath the next position.")]
    public bool requireGroundUnderStep = true;

    [Header("Timing")]
    public float turnDuration = 1.25f;
    public float revealHold = .8f;
    public float lookUpHold = 1.1f;
    public float firstGlitchDuration = .3f;
    public float glitchPause = .2f;
    public float headTiltHold = 3.5f;
    public float secondGlitchDuration = .45f;
    public float disappearHold = 2.2f;

    [Range(0f, 1f)]
    public float firstGlitchIntensity = .25f;

    [Range(0f, 1f)]
    public float secondGlitchIntensity = .45f;

    [Header("Audio")]
    public EncounterAudio sounds;
    public EncounterSound encounterHorrorSting = new EncounterSound();
    public EncounterSound firstGlitchSfx = new EncounterSound();
    public EncounterSound secondGlitchSfx = new EncounterSound();
    public EncounterSound disappearSfx = new EncounterSound();

    CharacterController capsule;
    PlayerMovement movement;

    readonly List<Behaviour> heldControls =
        new List<Behaviour>();

    readonly RaycastHit[] groundHits = new RaycastHit[16];

    Vector3 previousPosition;

    bool keyAssigned;
    bool keyCollected;
    bool ascended;
    bool crossedExit;
    bool played;
    bool watching;
    public bool ControlsCamera { get { return watching; } }

    void Awake()
    {
        // Hide the creature before gameplay begins.
        if (entity != null)
            entity.gameObject.SetActive(false);
    }

    void Start()
    {
        keyAssigned = towerKey != null;

        if (player == null ||
            playerCamera == null ||
            entity == null ||
            unlockedDoor == null ||
            staircase == null ||
            upperPlatform == null ||
            ascentZone == null ||
            landingTrigger == null ||
            stairExit == null ||
            cameraFocus == null)
        {
            Debug.LogError(
                "Tower encounter has an unassigned scene reference.",
                this
            );

            enabled = false;
            return;
        }

        // PlayerController belongs to Player X.
        // Use the CharacterController on that same object.
        capsule = player.GetComponent<CharacterController>();
        movement = player.GetComponent<PlayerMovement>();

        if (capsule == null || movement == null)
        {
            Debug.LogError(
                "Tower encounter cannot find CharacterController or PlayerMovement on Player X.",
                this
            );

            enabled = false;
            return;
        }

        previousPosition = capsule.bounds.center;
    }

    bool Inside(BoxCollider box, Vector3 position)
    {
        Vector3 p =
            box.transform.InverseTransformPoint(position) -
            box.center;

        Vector3 half = box.size * .5f;

        return Mathf.Abs(p.x) <= half.x &&
               Mathf.Abs(p.y) <= half.y &&
               Mathf.Abs(p.z) <= half.z;
    }

    void Update()
    {
        if (played ||
            PauseMenu.paused ||
            Time.timeScale <= 0f ||
            capsule == null)
        {
            return;
        }

        Vector3 position = capsule.bounds.center;
        Vector3 change = position - previousPosition;

        // Preserve the existing tower key detection.
        if (!keyCollected &&
            keyAssigned &&
            towerKey == null &&
            unlockedDoor.enabled)
        {
            keyCollected = true;

            // Already present in Idle. No Appear animation.
            entity.gameObject.SetActive(true);
        }

        if (!keyCollected)
        {
            previousPosition = position;
            return;
        }

        if (Inside(ascentZone, position) &&
            player.status == Status.climbingLadder)
        {
            if (change.y > .001f)
                ascended = true;

            if (change.y < -.01f)
            {
                ascended = false;
                crossedExit = false;
            }
        }

        float before = Vector3.Dot(
            previousPosition - stairExit.position,
            stairExit.forward
        );

        float after = Vector3.Dot(
            position - stairExit.position,
            stairExit.forward
        );

        if (ascended &&
            before < 0f &&
            after >= 0f &&
            Vector3.Dot(change, stairExit.forward) > 0f)
        {
            crossedExit = true;
        }

        float radius = capsule.radius *
            Mathf.Max(
                Mathf.Abs(player.transform.lossyScale.x),
                Mathf.Abs(player.transform.lossyScale.z)
            );

        if (ascended &&
            crossedExit &&
            Inside(landingTrigger, position) &&
            after >= radius + .01f &&
            player.status != Status.climbingLadder &&
            movement.grounded)
        {
            RaycastHit hit;

            Vector3 feet = new Vector3(
                position.x,
                capsule.bounds.min.y + .12f,
                position.z
            );

            if (Physics.Raycast(
                    feet,
                    Vector3.down,
                    out hit,
                    .4f,
                    ~0,
                    QueryTriggerInteraction.Ignore) &&
                hit.collider == upperPlatform)
            {
                played = true;
                StartCoroutine(Encounter());
            }
        }

        if (capsule.bounds.min.y <
                stairExit.position.y - .6f &&
            !Inside(ascentZone, position))
        {
            ascended = false;
            crossedExit = false;
        }

        previousPosition = position;
    }

    IEnumerator Encounter()
    {
        watching = true;

        // Preserve the original control lock.
        // Do not disable the existing zoom system.
        foreach (var control in
            player.GetComponentsInChildren<MonoBehaviour>())
        {
            if (control.enabled &&
                (control is PlayerController ||
                 control is PlayerMovement ||
                 control is PlayerInput ||
                 control is MovementType ||
                 control is InterpolatedTransformUpdater ||
                 control is MouseLook ||
                 control is HeadBob ||
                 control is AnimateLean ||
                 control is AnimateCameraLevel))
            {
                heldControls.Add(control);
                control.enabled = false;
            }
        }

        // Move Player X using its actual CharacterController.
        yield return StartCoroutine(StepForward());

        // Recalculate the camera target after the movement.
        Quaternion start = playerCamera.transform.rotation;

        Vector3 toFocus =
            cameraFocus.position -
            playerCamera.transform.position;

        Quaternion target = Quaternion.LookRotation(toFocus);

        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            if (PauseMenu.paused || Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(.01f, turnDuration)
            );

            float eased = t * t * t *
                (t * (t * 6f - 15f) + 10f);

            playerCamera.transform.rotation =
                Quaternion.Slerp(start, target, eased);

            yield return null;
        }

        playerCamera.transform.rotation = target;
        if (sounds != null)
            sounds.PlayStingWhenSeen(encounterHorrorSting, playerCamera,
                cameraFocus, entity.transform);

        ShadowPresenceEffect.Begin(this);

        yield return new WaitForSeconds(revealHold);

        entity.SetLookUp(true);

        yield return new WaitForSeconds(lookUpHold);

        if (sounds != null) sounds.Play(firstGlitchSfx);
        entity.PlayGlitch(
            firstGlitchIntensity,
            firstGlitchDuration
        );

        yield return new WaitForSeconds(
            firstGlitchDuration + glitchPause
        );

        entity.PlayHeadTilt();

        yield return new WaitForSeconds(headTiltHold);

        if (sounds != null) sounds.Play(secondGlitchSfx);
        entity.PlayGlitch(
            secondGlitchIntensity,
            secondGlitchDuration
        );

        yield return new WaitForSeconds(
            secondGlitchDuration + .1f
        );

        if (sounds != null) sounds.Play(disappearSfx);
        entity.PlayDisappear();
        ShadowPresenceEffect.End(this);

        yield return new WaitForSeconds(disappearHold);

        Animator animator = entity.GetComponent<Animator>();

        if (animator != null)
        {
            while (!animator.GetCurrentAnimatorStateInfo(0)
                       .IsName("Hidden"))
            {
                yield return null;
            }
        }

        RestoreControls();
    }

    IEnumerator StepForward()
    {
        if (capsule == null ||
            !capsule.enabled ||
            forwardStepDistance <= 0f)
        {
            yield break;
        }

        // Direction is taken from the stair exit unless
        // an exact destination marker is assigned.
        Vector3 direction;

        if (forwardStepTarget != null)
        {
            direction =
                forwardStepTarget.position -
                capsule.transform.position;
        }
        else
        {
            direction = stairExit.position + stairExit.forward * forwardStepDistance - capsule.transform.position;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < .0001f)
        {
            Debug.LogWarning(
                "Tower encounter: forward step has no valid direction.",
                this
            );

            yield break;
        }

        float destinationDistance = direction.magnitude;
        direction.Normalize();

        float wantedDistance = forwardStepTarget == null ? destinationDistance : forwardStepDistance;

        // An optional marker specifies the exact destination.
        if (forwardStepTarget != null)
        {
            Vector3 remaining =
                forwardStepTarget.position -
                capsule.transform.position;

            remaining.y = 0f;

            wantedDistance = Mathf.Min(
                wantedDistance,
                remaining.magnitude
            );
        }

        float duration = Mathf.Max(
            .05f,
            forwardStepDuration
        );

        float elapsed = 0f;
        float previousEased = 0f;
        float totalMoved = 0f;

        while (elapsed < duration)
        {
            if (PauseMenu.paused || Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(
                duration,
                elapsed + Time.deltaTime
            );

            float t = Mathf.Clamp01(elapsed / duration);

            float eased = t * t * (3f - 2f * t);

            float requestedMovement =
                wantedDistance * (eased - previousEased);

            previousEased = eased;

            if (requestedMovement <= .00001f)
            {
                yield return null;
                continue;
            }

            Vector3 displacement =
                direction * requestedMovement;

            // Check for any walkable collision surface,
            // not exclusively the assigned pCube1 collider.
            if (requireGroundUnderStep &&
                !HasGroundAtNextPosition(displacement))
            {
                Debug.LogWarning(
                    "Tower encounter: forward step stopped because no walkable platform was detected ahead.",
                    this
                );

                break;
            }

            // Move the actual CharacterController on Player X.
            Vector3 before = capsule.transform.position;

            capsule.Move(displacement);

            Vector3 actual =
                capsule.transform.position - before;

            float moved = Vector3.Dot(actual, direction);

            totalMoved += Mathf.Max(0f, moved);

            if (moved < requestedMovement * .25f)
            {
                Debug.LogWarning(
                    "Tower encounter: forward movement was blocked by collision.",
                    this
                );

                break;
            }

            yield return null;
        }

        Debug.Log(
            "Tower encounter: automatic forward step moved " +
            totalMoved.ToString("F2") + " metres.",
            this
        );
    }

    bool HasGroundAtNextPosition(Vector3 displacement)
    {
        Bounds bounds = capsule.bounds;

        // Probe from just above the player's feet at
        // the next horizontal position.
        Vector3 origin = new Vector3(
            bounds.center.x + displacement.x,
            bounds.min.y + .8f,
            bounds.center.z + displacement.z
        );

        int count = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            1.35f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];

            if (hit.collider == null)
                continue;

            // Ignore the player and its children.
            if (hit.collider.transform.IsChildOf(
                    player.transform))
            {
                continue;
            }

            // Require a reasonably walkable surface.
            if (hit.normal.y < .5f)
                continue;

            // Avoid treating terrain far below the tower
            // as supporting the player's next step.
            if (Mathf.Abs(
                    hit.point.y - bounds.min.y) > .55f)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    void RestoreControls()
    {
        if (!watching)
            return;

        watching = false;

        if (player != null && playerCamera != null)
        {
            Quaternion view =
                playerCamera.transform.rotation;

            Vector3 forward =
                playerCamera.transform.forward;

            forward.y = 0f;

            if (forward.sqrMagnitude > .001f)
            {
                player.transform.rotation =
                    Quaternion.LookRotation(forward);
            }

            playerCamera.transform.rotation = view;

            foreach (var look in
                player.GetComponentsInChildren<MouseLook>())
            {
                look.MatchCurrentRotation();
            }

            if (movement != null)
                movement.ForgetPreviousTransforms();
        }

        foreach (var control in heldControls)
        {
            if (control != null)
                control.enabled = true;
        }

        heldControls.Clear();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (sounds != null) sounds.StopSounds();
        RestoreControls();
    }
}
