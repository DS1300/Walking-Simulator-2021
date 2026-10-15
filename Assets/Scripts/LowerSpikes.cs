using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LowerSpikes : MonoBehaviour
{
    public float RayLength = 3f;
    private Ray playerAim;
    private Camera playerCam;

    private GameObject player;
    private new GameObject camera;

    public Animator lever;
    public Animator spikes;

    void Start()
    {
        player = GameObject.Find("Player X");
        camera = GameObject.Find("Main Camera");
    }

    void Update()
    {
        playerCam = Camera.main;
        Ray playerAim = playerCam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(playerAim, out hit, RayLength))
        {
            if ((hit.collider.gameObject.tag == "Lever") && Input.GetMouseButtonDown(0))
            {
                lever.Play("Lever");

                StartCoroutine(WaitBeforeDoing());
            }
        }

        IEnumerator WaitBeforeDoing()
        {
            yield return new WaitForSeconds(3);

            spikes.Play("Spikes");
        }
    }
}
