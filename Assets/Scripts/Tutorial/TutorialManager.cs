
using UnityEngine;
using TMPro;

namespace WalkingSimulator.Tutorial
{
    // Shows contextual control hints and hides each one after the player performs it.
    // Reads existing player state only; it never blocks input, raycasts or physics.
    public sealed class TutorialManager : MonoBehaviour
    {
        [Header("Player Systems")]
        public PlayerController player;
        public PlayerInput playerInput;
        public GrabRotate grab;
        public Notes notes;
        public TowerShadowEncounter towerEncounter;
        public Camera viewCamera;

        [Header("World Hints")]
        public TutorialHint[] hints;

        [Tooltip("Layers that hide a world hint when they stand between the camera and the hint.")]
        public LayerMask occlusionMask = ~0;

        [Header("Screen Hints")]
        public RectTransform canvasRect;
        public CanvasGroup moveHint;
        public CanvasGroup lookHint;
        public CanvasGroup zoomHint;
        public CanvasGroup worldHint;
        public TMP_Text worldHintText;
        public CanvasGroup rotateHint;

        [Header("Timing")]
        public float startDelay = 1.5f;

        [Tooltip("Alpha change per second.")]
        public float fadeSpeed = 2.5f;

        [Range(0f, 1f)]
        public float maxAlpha = .85f;

        [Tooltip("Mouse movement needed before the look hint counts as done.")]
        public float lookThreshold = 3f;

        [Tooltip("Seconds of actual held-object rotation needed before the rotate hint counts as done.")]
        public float rotateThreshold = .4f;

        bool moved, looked, zoomed, rotated;
        float lookAmount, rotateTime, startTime;

        Status lastStatus;
        Vector3 crouchStart;
        bool crouchTracking;

        static bool ReducedMotion
        {
            get
            {
                return PlayerPrefs.GetInt(
                    Presentation.GamePreferences.Prefix + "ReducedMotion", 0
                ) == 1;
            }
        }

        void Start()
        {
            startTime = Time.time;

            if (viewCamera == null)
                viewCamera = Camera.main;

            if (player != null)
                lastStatus = player.status;

            foreach (var group in new[]
            {
                moveHint,
                lookHint,
                zoomHint,
                worldHint,
                rotateHint
            })
            {
                if (group != null)
                    group.alpha = 0f;
            }
        }

        bool Suspended
        {
            get
            {
                return Time.timeScale == 0f ||
                       PauseMenu.paused ||
                       ShadowEncounterView.InUse ||
                       (notes != null && notes.IsReading) ||
                       (towerEncounter != null && towerEncounter.ControlsCamera) ||
                       player == null ||
                       !player.enabled ||
                       viewCamera == null;
            }
        }

        void LateUpdate()
        {
            bool suspended = Suspended;

            if (!suspended)
                TrackActions();

            bool started = Time.time - startTime >= startDelay;

            Fade(moveHint, started && !moved, suspended);
            Fade(lookHint, started && !looked, suspended);

            bool holding = grab != null && grab.IsHolding;

            // The rotate hint uses the same screen slot while holding.
            Fade(zoomHint, started && !zoomed && !holding, suspended);

            Fade(rotateHint, holding && !rotated, suspended);

            TutorialHint target = suspended
                ? null
                : FindActiveHint(holding);

            Fade(
                worldHint,
                target != null && PlaceWorldHint(target),
                suspended
            );
        }

        void TrackActions()
        {
            if (playerInput != null &&
                playerInput.raw.sqrMagnitude > .01f)
            {
                moved = true;
            }

            lookAmount +=
                Mathf.Abs(Input.GetAxis("Mouse X")) +
                Mathf.Abs(Input.GetAxis("Mouse Y"));

            if (lookAmount >= lookThreshold)
                looked = true;

            // Same button CameraZoom reads.
            if (Input.GetButton("Fire2"))
                zoomed = true;

            Status status = player.status;

            if (lastStatus == Status.vaulting &&
                status != Status.vaulting)
            {
                Complete(TutorialHint.Kind.Vault);
            }

            lastStatus = status;

            if (grab != null && grab.IsHolding)
            {
                Complete(TutorialHint.Kind.PickUp);

                if (grab.IsRotating &&
                    (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f))
                {
                    rotateTime += Time.deltaTime;
                }

                if (rotateTime >= rotateThreshold)
                    rotated = true;
            }

            TrackCrouchPassage(status);
        }

        void TrackCrouchPassage(Status status)
        {
            TutorialHint passage = null;

            foreach (var hint in hints)
            {
                if (hint != null &&
                    hint.kind == TutorialHint.Kind.Crouch &&
                    !hint.Completed &&
                    hint.ContainsPoint(player.transform.position))
                {
                    passage = hint;
                }
            }

            if (passage == null || status != Status.crouching)
            {
                crouchTracking = false;
                return;
            }

            if (!crouchTracking)
            {
                crouchTracking = true;
                crouchStart = player.transform.position;
            }

            Vector3 travel =
                player.transform.position - crouchStart;

            travel.y = 0f;

            if (travel.magnitude >= passage.requiredCrouchTravel)
            {
                passage.Completed = true;
                crouchTracking = false;
            }
        }

        void Complete(TutorialHint.Kind kind)
        {
            foreach (var hint in hints)
            {
                if (hint != null && hint.kind == kind)
                    hint.Completed = true;
            }
        }

        TutorialHint FindActiveHint(bool holding)
        {
            TutorialHint best = null;
            float bestDistance = float.MaxValue;

            Vector3 position = player.transform.position;

            foreach (var hint in hints)
            {
                if (hint == null ||
                    hint.Completed ||
                    !hint.isActiveAndEnabled)
                {
                    continue;
                }

                if (hint.kind == TutorialHint.Kind.PickUp && holding)
                    continue;

                if (hint.kind == TutorialHint.Kind.Vault &&
                    player.status == Status.vaulting)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    position,
                    hint.transform.position
                );

                if (distance <= hint.activationDistance &&
                    distance < bestDistance)
                {
                    best = hint;
                    bestDistance = distance;
                }
            }

            return best;
        }

        bool PlaceWorldHint(TutorialHint hint)
        {
            Vector3 world = hint.LabelPosition;
            Vector3 screen = viewCamera.WorldToScreenPoint(world);

            if (screen.z <= 0f ||
                screen.x < 0f ||
                screen.x > Screen.width ||
                screen.y < 0f ||
                screen.y > Screen.height)
            {
                return false;
            }

            if (Physics.Linecast(
                viewCamera.transform.position,
                world,
                occlusionMask,
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector2 local;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screen,
                null,
                out local))
            {
                return false;
            }

            worldHintText.rectTransform.anchoredPosition = local;

            if (worldHintText.text != hint.text)
                worldHintText.text = hint.text;

            return true;
        }

        // Hints vanish when another interface or cinematic takes over.
        void Fade(CanvasGroup group, bool visible, bool suspended)
        {
            if (group == null)
                return;

            float target = visible && !suspended
                ? maxAlpha
                : 0f;

            group.alpha = suspended || ReducedMotion
                ? target
                : Mathf.MoveTowards(
                    group.alpha,
                    target,
                    fadeSpeed * Time.unscaledDeltaTime
                );
        }
    }
}
