using System.Collections;
using UnityEngine;

public class CabinShadowEncounter : MonoBehaviour
{
    [Header("Scene")]
    public Notes journal;
    public GameObject cabinNote;
    public ShadowEncounterActor actor;
    public ShadowEncounterView view;
    public bool guidedLook = true;
    public bool facePlayer = true;
    public Collider[] windowColliders;

    [Header("Timing")]
    public float cameraTurnDuration = .9f;
    public float idleHold = 1f;
    public float headTiltHold = 3.5f;
    [Range(0f, 1f)] public float aggressiveGlitchIntensity = .8f;
    public float aggressiveGlitchDuration = 1.4f;

    [Header("Audio")]
    public EncounterSound encounterHorrorSting = new EncounterSound();
    public EncounterSound appearSfx = new EncounterSound();
    public EncounterSound headTiltSfx = new EncounterSound();
    public EncounterSound aggressiveGlitchSfx = new EncounterSound();
    public EncounterSound disappearSfx = new EncounterSound();

    bool pending;
    bool played;

    void OnEnable() { journal.NoteClosed += NoteClosed; }
    void Start() { actor.HideImmediately(); }

    void NoteClosed(int index, bool inWorld)
    {
        if (!played && inWorld && index >= 0 && index < journal.pages.Length &&
            journal.pages[index].noteObject == cabinNote) pending = true;
    }

    void Update()
    {
        if (!pending || played || PauseMenu.paused || Time.timeScale <= 0f ||
            journal.IsReading || ShadowEncounterView.InUse) return;
        if (guidedLook && !view.TakeControl()) return;
        pending = false;
        played = true;
        StartCoroutine(Encounter());
    }

    IEnumerator Encounter()
    {
        if (facePlayer) actor.Face(view.viewCamera.transform);
        if (guidedLook) yield return view.LookAt(actor.lookTarget, cameraTurnDuration);
        Coroutine discovery = StartCoroutine(DiscoverySting());
        yield return actor.Appear(appearSfx, actor.appearDuration);
        yield return new WaitForSeconds(idleHold);
        yield return actor.HeadTilt(headTiltSfx, headTiltHold);
        yield return actor.Glitch(aggressiveGlitchSfx, true,
            aggressiveGlitchIntensity, aggressiveGlitchDuration);
        StopCoroutine(discovery);
        yield return actor.Disappear(disappearSfx);
        view.Release();
    }

    IEnumerator DiscoverySting()
    {
        yield return new WaitForSeconds(.6f);
        while (actor.entity != null)
        {
            if (!PauseMenu.paused && actor.entity.gameObject.activeInHierarchy)
            {
                Vector3 point = view.viewCamera.WorldToViewportPoint(actor.lookTarget.position);
                bool visible = point.z > 0f && point.x > .05f && point.x < .95f &&
                    point.y > .05f && point.y < .95f;
                if (visible && (guidedLook || WindowVisible()))
                {
                    actor.sounds.PlaySting(encounterHorrorSting);
                    yield break;
                }
            }
            yield return null;
        }
    }

    bool WindowVisible()
    {
        Vector3 origin = view.viewCamera.transform.position;
        Vector3 delta = actor.lookTarget.position - origin;
        foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude,
            ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(actor.entity.transform) ||
                hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (windowColliders != null && System.Array.IndexOf(windowColliders, hit.collider) >= 0) continue;
            return false;
        }
        return true;
    }

    void OnDisable()
    {
        if (journal != null) journal.NoteClosed -= NoteClosed;
        StopAllCoroutines();
        if (view != null) view.Release();
        if (actor != null) actor.HideImmediately();
    }
}
