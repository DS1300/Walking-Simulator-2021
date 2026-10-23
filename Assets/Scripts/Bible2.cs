using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bible2 : MonoBehaviour
{
    public WalkingSimulator.Presentation.SpatialEnvironmentAudio fountainSound;
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


    bool IsAimedAt()
    {
        Camera cam = Camera.main;
        if (cam == null || PauseMenu.paused || Time.timeScale <= 0f) return false;
        CrosshairGUI crosshair = FindObjectOfType<CrosshairGUI>();
        float range = crosshair != null ? crosshair.RayLength : 3f;
        RaycastHit hit;
        if (!Physics.Raycast(cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f)), out hit, range))
            return false;
        return hit.collider.GetComponentInParent<Bible2>() == this;
    }

    void Update()
    {
        if (objectRendererToChange != null && resetTime != 0f && resetTime < Time.time)
        {
            objectRendererToChange.material = startColour;
        }
        TryActivate();
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
    private void TryActivate()
    {
        if (!activated && Input.GetMouseButtonDown(0) && IsAimedAt())
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
            if (fountainSound != null) fountainSound.Activate();

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
            yield return LookAt(fountainView, 2.5f);
            yield return new WaitForSeconds(3.5f);
        }
        else yield return new WaitForSeconds(5);

        anim.Play("Graveyard Gate", 0, 0f);
        if (watching) yield return LookAt(gateView, 2.8f);
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
        float endFov = originalFieldOfView;
        Vector3 startPosition = viewCamera.transform.position;
        Vector3 endPosition = target == fountainView && fountainCameraPosition != null
            ? fountainCameraPosition.position
            : startPosition;
        Quaternion end = Quaternion.LookRotation(target.position - endPosition);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * t * (t * (t * 6f - 15f) + 10f);
            viewCamera.fieldOfView = Mathf.Lerp(startFov, endFov, eased);
            Vector3 desiredPosition = Vector3.Lerp(startPosition, endPosition, eased);
            Vector3 displacement = desiredPosition - viewCamera.transform.position;
            displacement.y = 0f;
            CharacterController body = player.GetComponent<CharacterController>();
            if (body != null && body.enabled) body.Move(displacement);
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
