using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDoor2 : MonoBehaviour
{
    public Animator anim;
    private bool canOpen;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canOpen)
        {
            anim.Play("Cabin Door 2");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            canOpen = true;
        }
    }
}
