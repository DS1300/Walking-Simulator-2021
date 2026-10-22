using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowEncounterView : MonoBehaviour
{
    public PlayerController player;
    public Camera viewCamera;
    public static bool InUse { get { return owner != null; } }

    static ShadowEncounterView owner;
    readonly List<Behaviour> controls = new List<Behaviour>();
    Quaternion view;

    public bool TakeControl()
    {
        if (owner != null || player == null || viewCamera == null) return false;
        owner = this;
        view = viewCamera.transform.rotation;
        foreach (var control in player.GetComponentsInChildren<MonoBehaviour>())
        {
            if (control.enabled && (control is PlayerController || control is PlayerMovement ||
                control is PlayerInput || control is MovementType || control is MouseLook ||
                control is InterpolatedTransformUpdater || control is HeadBob ||
                control is AnimateLean || control is AnimateCameraLevel ||
                control is CrosshairGUI || control is Notes))
            {
                controls.Add(control);
                control.enabled = false;
            }
        }
        return true;
    }

    public IEnumerator LookAt(Transform target, float seconds)
    {
        Quaternion start = viewCamera.transform.rotation;
        Quaternion end = Quaternion.LookRotation(target.position - viewCamera.transform.position);
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, seconds));
            float smooth = t * t * t * (t * (t * 6f - 15f) + 10f);
            view = Quaternion.Slerp(start, end, smooth);
            viewCamera.transform.rotation = view;
            yield return null;
        }
        view = end;
        viewCamera.transform.rotation = view;
    }

    void LateUpdate()
    {
        if (owner == this) viewCamera.transform.rotation = view;
    }

    public void Release()
    {
        if (owner != this) return;
        owner = null;
        if (player != null && viewCamera != null)
        {
            Vector3 forward = viewCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > .001f)
                player.transform.rotation = Quaternion.LookRotation(forward);
            viewCamera.transform.rotation = view;
            foreach (var look in player.GetComponentsInChildren<MouseLook>())
                look.MatchCurrentRotation();
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.ForgetPreviousTransforms();
        }
        foreach (var control in controls) if (control != null) control.enabled = true;
        controls.Clear();
    }

    void OnDisable() { Release(); }
}
