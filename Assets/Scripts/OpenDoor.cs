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
        var feedback = anim.GetComponent<DoorFeedback>();
        if(Input.GetMouseButtonDown(0) && (feedback != null ? feedback.CanOpen : canOpen))
        {
            if (feedback != null) feedback.MarkOpened();
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
