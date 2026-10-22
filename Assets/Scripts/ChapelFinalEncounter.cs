
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ChapelFinalEncounter : MonoBehaviour
{
    [Header("Scene")]
    public Notes journal;
    public Transform coffin;
    public Transform coffinLookTarget;
    public Transform entranceLookTarget;
    public ShadowEncounterActor actor;
    public ShadowEncounterView view;
    public EncounterSilence silence;
    public PauseMenu pauseMenu;
    public TextMeshProUGUI interactionPrompt;
    public CanvasGroup blackScreen;
    public CanvasGroup endingTitleGroup;
    public CanvasGroup returnPromptGroup;
    public TextMeshProUGUI endingTitleText;
    public string mainMenuScene = "Main Menu";

    [Header("Interaction")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

#if UNITY_EDITOR
    [Header("Testing (Editor Only)")]
    [Tooltip("Test the ending without collecting all 8 notes.")]
    public bool testEndingWithoutNotes = false;
#endif

    [Header("Reveal")]
    public float coffinTurnDuration = .65f;
    public float coffinHold = 1f;
    public float entranceTurnDuration = 1.8f;
    public float ambienceFadeDuration = .9f;
    public float silenceHold = 1.25f;

    [Tooltip("How long the creature remains still after appearing.")]
    public float postAppearHold = 1f;

    public bool facePlayer = true;

    [Header("Final lunge")]
    public float lungeDuration = 2.6f;
    public float lungeClipDuration = 2.6f;
    public float maximumTravelDistance = 14f;
    public float stopDistance = 2.3f;
    public float focusHeight = .8f;

    [Range(0f, 1f)]
    public float impactMoment = .9f;

    [Header("Ending")]
    public string endingTitle = "TO BE CONTINUED...";
    public float fadeToBlackDuration = .9f;
    public float endingAudioFade = 1f;
    public float blackHold = .6f;
    public float titleFadeDuration = 1.2f;
    public float titleHold = 3.5f;

    [Header("Audio")]
    public EncounterSound payRespectsSfx =
        new EncounterSound();

    public EncounterSound silenceTransitionSfx =
        new EncounterSound();

    public EncounterSound rapidAppearSfx =
        new EncounterSound();

    public EncounterSound finalLungeScreech =
        new EncounterSound();

    public EncounterSound finalImpactSfx =
        new EncounterSound();

    public EncounterSound endingSfx =
        new EncounterSound();

    // Original Shadow Entity Appear animation duration.
    const float NormalAppearDuration = 2f;

    bool played;
    bool canReturn;
    bool pauseWasEnabled;
    bool pauseCaptured;

    void Start()
    {
        actor.HideImmediately();

        blackScreen.alpha = 0f;
        endingTitleGroup.alpha = 0f;
        returnPromptGroup.alpha = 0f;
        interactionPrompt.enabled = false;
    }

    bool AllNotesCollected()
    {
#if UNITY_EDITOR
        if (testEndingWithoutNotes)
            return true;
#endif

        if (journal == null ||
            journal.pages == null ||
            journal.pages.Length != 8)
        {
            return false;
        }

        for (int i = 0; i < journal.pages.Length; i++)
        {
            if (!journal.IsCollected(i))
                return false;
        }

        return true;
    }

    void Update()
    {
        if (canReturn && Input.anyKeyDown)
        {
            canReturn = false;

            PauseMenu.paused = false;
            Time.timeScale = 1f;

            silence.Restore();
            SceneManager.LoadScene(mainMenuScene);
            return;
        }

        interactionPrompt.enabled = false;

        if (played ||
            PauseMenu.paused ||
            Time.timeScale <= 0f ||
            ShadowEncounterView.InUse ||
            journal == null ||
            journal.IsReading ||
            !AllNotesCollected())
        {
            return;
        }

        Ray ray = view.viewCamera.ViewportPointToRay(
            new Vector3(.5f, .5f, 0f)
        );

        RaycastHit hit;

        if (!Physics.Raycast(
                ray,
                out hit,
                interactionDistance,
                ~0,
                QueryTriggerInteraction.Ignore) ||
            !hit.transform.IsChildOf(coffin))
        {
            return;
        }

        interactionPrompt.enabled = true;

        if (!Input.GetKeyDown(interactionKey) ||
            !view.TakeControl())
        {
            return;
        }

        played = true;
        interactionPrompt.enabled = false;

        StartCoroutine(Ending());
    }

    IEnumerator Ending()
    {
        // Begin suppressing ordinary environmental audio.
        actor.sounds.Play(payRespectsSfx);
        actor.sounds.Play(silenceTransitionSfx);

        StartCoroutine(
            silence.FadeToSilence(ambienceFadeDuration)
        );

        // Look toward the coffin.
        yield return view.LookAt(
            coffinLookTarget,
            coffinTurnDuration
        );

        yield return new WaitForSeconds(coffinHold);

        // Slowly turn toward the chapel entrance.
        yield return view.LookAt(
            entranceLookTarget,
            entranceTurnDuration
        );

        // Hold on the apparently empty entrance.
        yield return new WaitForSeconds(silenceHold);

        if (facePlayer)
            actor.Face(view.viewCamera.transform);

        // Play the ORIGINAL, full-length smoke appearance.
        // Do not use the accelerated 0.35-second version.
        yield return actor.Appear(
            rapidAppearSfx,
            NormalAppearDuration
        );

        // Let the player see the creature standing still.
        // No head tilt, glitch, or movement here.
        yield return new WaitForSeconds(
            Mathf.Max(0f, postAppearHold)
        );

        // The scream begins immediately BEFORE the attack.
        actor.sounds.Play(finalLungeScreech);

        Animator animator =
            actor.entity.GetComponent<Animator>();

        if (animator != null)
        {
            animator.speed =
                lungeClipDuration /
                Mathf.Max(.1f, lungeDuration);
        }

        // Existing package's FinalLunge animation.
        actor.entity.PlayFinalLunge();

        // Move the existing actor toward the player's camera.
        Transform entityPosition = actor.transform;

        Vector3 start = entityPosition.position;
        Vector3 target = view.viewCamera.transform.position;

        Vector3 direction = target - start;
        direction.y = 0f;

        float horizontalDistance = direction.magnitude;

        float distance = Mathf.Clamp(
            horizontalDistance - stopDistance,
            0f,
            maximumTravelDistance
        );

        Vector3 end = start;

        if (horizontalDistance > .001f)
        {
            end += direction.normalized * distance;
        }

        end.y = target.y - focusHeight;

        float elapsed = 0f;
        bool impacted = false;

        while (elapsed < lungeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed /
                Mathf.Max(.1f, lungeDuration)
            );

            float eased = Mathf.SmoothStep(0f, 1f, t);

            entityPosition.position =
                Vector3.Lerp(start, end, eased);

            // Keep the existing impact synchronization.
            if (!impacted && t >= impactMoment)
            {
                impacted = true;
                actor.sounds.Play(finalImpactSfx);
            }

            yield return null;
        }

        entityPosition.position = end;

        if (animator != null)
            animator.speed = 1f;

        // Fade after the lunge has had time to play.
        yield return Fade(
            blackScreen,
            1f,
            fadeToBlackDuration
        );

        pauseWasEnabled = pauseMenu.enabled;
        pauseCaptured = true;
        pauseMenu.enabled = false;

        actor.sounds.Play(endingSfx);

        yield return actor.sounds.FadeOut(
            endingAudioFade
        );

        yield return new WaitForSeconds(blackHold);

        endingTitleText.text = endingTitle;

        yield return Fade(
            endingTitleGroup,
            1f,
            titleFadeDuration
        );

        yield return new WaitForSeconds(titleHold);

        yield return Fade(
            returnPromptGroup,
            1f,
            .5f
        );

        canReturn = true;
    }

    IEnumerator Fade(
        CanvasGroup group,
        float target,
        float seconds)
    {
        float start = group.alpha;
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;

            group.alpha = Mathf.Lerp(
                start,
                target,
                elapsed / Mathf.Max(.01f, seconds)
            );

            yield return null;
        }

        group.alpha = target;
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (view != null)
            view.Release();

        if (silence != null)
            silence.Restore();

        if (actor != null)
        {
            if (actor.entity != null)
            {
                Animator animator =
                    actor.entity.GetComponent<Animator>();

                if (animator != null)
                    animator.speed = 1f;
            }

            actor.HideImmediately();
        }

        if (pauseCaptured && pauseMenu != null)
            pauseMenu.enabled = pauseWasEnabled;

        if (blackScreen != null)
            blackScreen.alpha = 0f;

        if (endingTitleGroup != null)
            endingTitleGroup.alpha = 0f;

        if (returnPromptGroup != null)
            returnPromptGroup.alpha = 0f;

        if (interactionPrompt != null)
            interactionPrompt.enabled = false;
    }
}
