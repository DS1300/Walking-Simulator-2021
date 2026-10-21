
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DS_PickUp : MonoBehaviour
{
    public Transform Destination;
    public bool canpick;
    public bool picked;
    private GameObject player;
    private new GameObject camera;

    private void Start()
    {
        player = GameObject.Find("Player X");
        camera = GameObject.Find("Main Camera");
    }

    private void Update()
    {
        if (Input.GetKey("r") && !canpick)
        {
            var rotationX = Input.GetAxis("Mouse X") * 10;
            var rotationY = Input.GetAxis("Mouse Y") * 10;

            transform.Rotate(Destination.up, -rotationX, Space.Self);
            transform.Rotate(Destination.right, rotationY, Space.Self);

            player.GetComponent<MouseLook>().enabled = false;
            camera.GetComponent<MouseLook>().enabled = false;
        }

        if (Input.GetKeyUp("r"))
        {
            player.GetComponent<MouseLook>().enabled = true;
            camera.GetComponent<MouseLook>().enabled = true;
        }
    }

    void OnMouseDown()
    {
        GetComponent<Rigidbody>().useGravity = false;
        this.transform.position = Destination.position;
        this.transform.parent = GameObject.Find("Destination").transform;
        picked = true;
        canpick = false;
    }

    void OnMouseUp()
    {
        this.transform.parent = null;
        GetComponent<Rigidbody>().useGravity = true;
        picked = false;
        canpick = true;
    }
}
