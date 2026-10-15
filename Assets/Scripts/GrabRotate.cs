using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrabRotate : MonoBehaviour
{
    public Transform Destination;

    public float RayLength = 3f;   
    private Ray playerAim;
    private Camera playerCam;

    public float rotationSpeed;

    private GameObject player;
    private new GameObject camera;
    private GameObject grabbedobjects;

    private bool canpick;
    private bool picked;

 
    private void Start()
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
            if ((hit.collider.gameObject.tag == "Interact") && Input.GetMouseButton(0))
            {
                grabbedobjects = hit.collider.gameObject;
                grabbedobjects.GetComponent<Rigidbody>().useGravity = false;
                grabbedobjects.transform.position = Destination.position;
                grabbedobjects.transform.parent = GameObject.Find("Destination").transform;

                player.GetComponent<CrosshairGUI>().m_DefaultReticle = true;
                player.GetComponent<CrosshairGUI>().m_UseReticle = false;

                canpick = true;
                picked = false;
               
            }
           
        }

        if (Input.GetMouseButtonUp(0))
        {
            grabbedobjects.transform.parent = null;
            grabbedobjects.GetComponent<Rigidbody>().useGravity = true;

            canpick = true;
            picked = false;
        }

        if (Input.GetKey("r"))
        {
            var rotationX = Input.GetAxis("Mouse X") * rotationSpeed;
            var rotationY = Input.GetAxis("Mouse Y") * rotationSpeed;

            grabbedobjects.transform.Rotate(Destination.up, Mathf.Deg2Rad * rotationX);
            grabbedobjects.transform.Rotate(Destination.right, -Mathf.Deg2Rad * rotationY);

            player.GetComponent<MouseLook>().enabled = false;
            camera.GetComponent<MouseLook>().enabled = false;
          
        }

        if (Input.GetKeyUp("r"))
        {
            player.GetComponent<MouseLook>().enabled = true;
            camera.GetComponent<MouseLook>().enabled = true;
        }
        
    }   

}
