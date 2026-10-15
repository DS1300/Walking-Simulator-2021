using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notes : MonoBehaviour
{
    public float RayLength = 3f;
    private Ray playerAim;
    private Camera playerCam;

    private GameObject player;
    private new GameObject camera;

    public Canvas note;

    void Start()
    {
        player = GameObject.Find("Player X");
        camera = GameObject.Find("Main Camera");

        note.enabled = false;
    }

    void Update()
    {
        playerCam = Camera.main;
        Ray playerAim = playerCam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(playerAim, out hit, RayLength))
        {
            if ((hit.collider.gameObject.tag == "Note") && Input.GetMouseButtonDown(0))
            {
                ShowNote();
            }

            if (Input.GetKeyDown("e"))
            {
                HideNote();
            }
        }

        void ShowNote()
        {
            note.enabled = true;

            player.GetComponent<MouseLook>().enabled = false;
            camera.GetComponent<MouseLook>().enabled = false;

            player.GetComponent<CrosshairGUI>().enabled = false;

            player.GetComponent<PlayerController>().enabled = false;

            camera.GetComponent<HeadBob>().enabled = false;
        }

        void HideNote()
        {
            note.enabled = false;

            player.GetComponent<MouseLook>().enabled = true;
            camera.GetComponent<MouseLook>().enabled = true;

            player.GetComponent<CrosshairGUI>().enabled = true;

            player.GetComponent<PlayerController>().enabled = true;

            camera.GetComponent<HeadBob>().enabled = true;
        }
    }
}
