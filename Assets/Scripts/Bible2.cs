using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bible2 : MonoBehaviour
{
    public bool activateState = false;
    public List<GameObject> objectsToActivate;
    private bool canActivate = false;


    public GameObject objectToChange;
    public Renderer objectRendererToChange;
    public Material activateColour;
    public float timeToReset = 1.5f;
    private float resetTime;
    private Material startColour;

    public Animator anim;
    public GameObject Lock;
    public Transform fountainView;
    public Transform gateView;
    public Transform fountainCameraPosition;
    private Vector3 cameraLocalPosition;
    public float fountainFieldOfView = 75f;
    private float originalFieldOfView;
    private bool activated;
    private bool watching;
    private Camera viewCamera;
    private PlayerController player;
    private List<Behaviour> pausedControls = new List<Behaviour>();

    [SerializeField] ParticleSystem Water1 = null;
    [SerializeField] ParticleSystem Water2 = null;
    [SerializeField] ParticleSystem Water3 = null;
    [SerializeField] ParticleSystem Water4 = null;

    [SerializeField] ParticleSystem WaterLeak1 = null;
    [SerializeField] ParticleSystem WaterLeak2 = null;
    [SerializeField] ParticleSystem WaterLeak3 = null;
    [SerializeField] ParticleSystem WaterLeak4 = null;


    void Start()
    {
        if (objectRendererToChange != null)
            startColour = objectRendererToChange.material;
    }


    void Update()
    {
        if (objectRendererToChange != null && resetTime != 0f && resetTime < Time.time)
        {
            objectRendererToChange.material = startColour;
        }
        if (canActivate)
        {
            activateState = !activateState;
            canActivate = !canActivate;
            foreach (GameObject objectToActivate in objectsToActivate)
            {
                objectToActivate.SetActive(activateState);
            }
         
            if (objectRendererToChange != null && activateColour != null)
            {
                objectRendererToChange.material = activateColour;
                resetTime = Time.time + timeToReset;
            }
        }


    }
    private void OnTriggerStay(Collider other)
    {
        if (!activated && Input.GetMouseButtonDown(0))
        {
            activated = true;
            canActivate = true;

            Destroy(Lock);

            Water1.Play();
            Water2.Play();
            Water3.Play();
            Water4.Play();

            WaterLeak1.Play();
            WaterLeak2.Play();
            WaterLeak3.Play();
            WaterLeak4.Play();

            StartCoroutine(WaitBeforeDoing());

        }

    }

    IEnumerator WaitBeforeDoing()
    {
        viewCamera = Camera.main;
        player = viewCamera != null ? viewCamera.GetComponentInParent<PlayerController>() : null;
        if (player != null && fountainView != null && gateView != null)
        {
            watching = true;
            cameraLocalPosition = viewCamera.transform.localPosition;
            originalFieldOfView = viewCamera.fieldOfView;
            foreach (var control in player.GetComponentsInChildren<MonoBehaviour>())
            {
                if (control.enabled && (control is PlayerController || control is PlayerMovement ||
                    control is PlayerInput || control is InterpolatedTransformUpdater || control is MouseLook ||
                    control is HeadBob || control is CameraZoom || control is GrabRotate || control is AnimateLean || control is AnimateCameraLevel))
                {
                    pausedControls.Add(control);
                    control.enabled = false;
                }
            }
            yield return LookAt(fountainView, 1.5f);
            yield return new WaitForSeconds(3.5f);
        }
        else yield return new WaitForSeconds(5);

        anim.Play("Graveyard Gate", 0, 0f);
        if (watching) yield return LookAt(gateView, 1.9f);
        yield return null;
        while (anim.GetCurrentAnimatorStateInfo(0).IsName("Graveyard Gate") &&
            anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;
        RestoreControls();
    }

    IEnumerator LookAt(Transform target, float duration)
    {
        Quaternion start = viewCamera.transform.rotation;
        float startFov = viewCamera.fieldOfView;
        float endFov = target == fountainView ? fountainFieldOfView : originalFieldOfView;
        Vector3 startPosition = viewCamera.transform.position;
        Vector3 endPosition = target == fountainView && fountainCameraPosition != null
            ? fountainCameraPosition.position
            : viewCamera.transform.parent.TransformPoint(cameraLocalPosition);
        Quaternion end = Quaternion.LookRotation(target.position - endPosition);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * t * (t * (t * 6f - 15f) + 10f);
            viewCamera.fieldOfView = Mathf.Lerp(startFov, endFov, eased);
            viewCamera.transform.position = Vector3.Lerp(startPosition, endPosition, eased);
            viewCamera.transform.rotation = Quaternion.Slerp(start, end, eased);
            yield return null;
        }
    }

    void RestoreControls()
    {
        if (!watching) return;
        watching = false;
        if (player == null || viewCamera == null)
        {
            foreach (var control in pausedControls) if (control != null) control.enabled = true;
            pausedControls.Clear();
            return;
        }
        viewCamera.fieldOfView = originalFieldOfView;
        viewCamera.transform.localPosition = cameraLocalPosition;
        Quaternion view = viewCamera.transform.rotation;
        Vector3 forward = viewCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > .001f) player.transform.rotation = Quaternion.LookRotation(forward);
        viewCamera.transform.rotation = view;
        foreach (var look in player.GetComponentsInChildren<MouseLook>()) look.MatchCurrentRotation();
        foreach (var control in pausedControls) if (control != null) control.enabled = true;
        pausedControls.Clear();
        player.GetComponent<PlayerMovement>().ForgetPreviousTransforms();
    }

    void OnDisable()
    {
        RestoreControls();
    }
     
}
