using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public Animator anim;
    private bool canOpen;


    void Start()
    {
        
    }

    
    void Update()
    {
        if(Input.GetMouseButtonDown(0) && canOpen)
        {
            anim.Play("Cabin Door 1");
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
