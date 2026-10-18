using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrabRotate : MonoBehaviour
{
    public Transform Destination;
    public float RayLength = 3f;
    public float rotationSpeed = 10f;

    private Camera playerCam;
    private Rigidbody grabbedobjects;
    private Quaternion holdRotation;
    private MouseLook[] mouseLook;
    private bool[] mouseEnabled;
    private bool rotating;
    private bool oldGravity;
    private float oldDrag, oldAngularDrag, oldMaxAngularVelocity;
    private RigidbodyInterpolation oldInterpolation;
    private CollisionDetectionMode oldCollisionMode;
    private readonly List<Collider[]> ignoredCollisions = new List<Collider[]>();

    void Start()
    {
        playerCam = Camera.main;
        mouseLook = GetComponentsInChildren<MouseLook>();
        mouseEnabled = new bool[mouseLook.Length];
    }

    void Update()
    {
        if (Time.timeScale == 0f) { SetRotation(false); return; }
        if (playerCam == null || Destination == null) return;

        if (grabbedobjects == null && Input.GetMouseButtonDown(0))
        {
            Ray playerAim = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(playerAim, out hit, RayLength, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.CompareTag("Interact") && hit.rigidbody != null && !hit.rigidbody.isKinematic)
                PickUp(hit.rigidbody);
        }

        if (grabbedobjects == null) { SetRotation(false); return; }
        if (Input.GetMouseButtonUp(0)) { Release(); return; }
        SetRotation(Input.GetKey(KeyCode.R));
        if (rotating)
        {
            Vector2 rotationInput = Vector2.ClampMagnitude(new Vector2(
                -Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * rotationSpeed, 30f);
            holdRotation = Quaternion.RotateTowards(grabbedobjects.rotation, holdRotation, 60f);
            holdRotation = Quaternion.AngleAxis(rotationInput.x, playerCam.transform.up)
                * Quaternion.AngleAxis(rotationInput.y, playerCam.transform.right) * holdRotation;
            holdRotation = Quaternion.RotateTowards(grabbedobjects.rotation, holdRotation, 60f);
        }
    }

    void PickUp(Rigidbody body)
    {
        grabbedobjects = body;
        oldGravity = body.useGravity;
        oldDrag = body.drag;
        oldAngularDrag = body.angularDrag;
        oldMaxAngularVelocity = body.maxAngularVelocity;
        oldInterpolation = body.interpolation;
        oldCollisionMode = body.collisionDetectionMode;
        holdRotation = body.rotation;
        body.useGravity = false;
        body.drag = 8f;
        body.angularDrag = 12f;
        body.maxAngularVelocity = 3f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        foreach (var held in body.GetComponentsInChildren<Collider>())
            foreach (var player in GetComponentsInChildren<Collider>())
                if (held != player && !Physics.GetIgnoreCollision(held, player))
                {
                    Physics.IgnoreCollision(held, player, true);
                    ignoredCollisions.Add(new Collider[] { held, player });
                }
    }

    void FixedUpdate()
    {
        if (grabbedobjects == null || Destination == null) return;
        Vector3 offset = Destination.position - grabbedobjects.position;
        if (offset.sqrMagnitude > 16f) { Release(); return; }
        grabbedobjects.velocity = Vector3.ClampMagnitude(offset * 12f, 5f);
        Quaternion rotation = holdRotation * Quaternion.Inverse(grabbedobjects.rotation);
        float angle; Vector3 axis;
        rotation.ToAngleAxis(out angle, out axis);
        if (angle > 180f) angle -= 360f;
        grabbedobjects.angularVelocity = angle * angle < .01f ? Vector3.zero
            : Vector3.ClampMagnitude(axis.normalized * angle * Mathf.Deg2Rad * 8f, 3f);
    }

    void SetRotation(bool value)
    {
        if (rotating == value || mouseLook == null) return;
        for (int i = 0; i < mouseLook.Length; i++)
        {
            if (value) { mouseEnabled[i] = mouseLook[i].enabled; mouseLook[i].enabled = false; }
            else mouseLook[i].enabled = mouseEnabled[i];
        }
        rotating = value;
    }

    void Release()
    {
        SetRotation(false);
        foreach (var pair in ignoredCollisions)
            if (pair[0] != null && pair[1] != null) Physics.IgnoreCollision(pair[0], pair[1], false);
        ignoredCollisions.Clear();
        if (grabbedobjects == null) return;
        grabbedobjects.useGravity = oldGravity;
        grabbedobjects.drag = oldDrag;
        grabbedobjects.angularDrag = oldAngularDrag;
        grabbedobjects.maxAngularVelocity = oldMaxAngularVelocity;
        grabbedobjects.interpolation = oldInterpolation;
        grabbedobjects.collisionDetectionMode = oldCollisionMode;
        grabbedobjects.velocity = Vector3.ClampMagnitude(grabbedobjects.velocity, 2f);
        grabbedobjects.angularVelocity = Vector3.zero;
        grabbedobjects = null;
    }

    void OnDisable() { Release(); }
}
