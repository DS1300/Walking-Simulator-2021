using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bible2 : MonoBehaviour
{
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
        startColour = objectRendererToChange.material;
    }


    void Update()
    {
        if (resetTime != 0f && resetTime < Time.time)
        {
            objectRendererToChange.material = startColour;
        }
        if (canActivate)
        {
            activateState = !activateState;
            canActivate = !canActivate;
            foreach (GameObject objectToActivate in objectsToActivate)
            {
                objectToActivate.SetActive(activateState);
            }
         
            objectRendererToChange.material = activateColour;
            resetTime = Time.time + timeToReset;
        }


    }
    private void OnTriggerStay(Collider other)
    {
        if (Input.GetMouseButtonDown(0))
        {

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

            StartCoroutine(WaitBeforeDoing());

        }

        IEnumerator WaitBeforeDoing()
        {
            yield return new WaitForSeconds(5);

            anim.Play("Graveyard Gate");
        }
    }
     
}
