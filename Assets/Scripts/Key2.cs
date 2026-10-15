using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key2 : MonoBehaviour
{

    public float RayLength = 3f;
    private Ray playerAim;
    private Camera playerCam;

    private GameObject player;
    private new GameObject camera;

    public Component cabindoorcollider2;

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
            if ((hit.collider.gameObject.tag == "Key2") && Input.GetMouseButtonDown(0))
            {
                cabindoorcollider2.GetComponent<BoxCollider>().enabled = true;
                Destroy(gameObject);
            }
        }
    }
}   

