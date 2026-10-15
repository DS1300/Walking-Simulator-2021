using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bible1 : MonoBehaviour
{
    public float RayLength = 3f;
    private Ray playerAim;
    private Camera playerCam;

    private GameObject player;
    private new GameObject camera;

    public Component cabindoorcollider2;

    [SerializeField] ParticleSystem Fire1 = null;
    [SerializeField] ParticleSystem Fire2 = null;
    [SerializeField] ParticleSystem Fire3 = null;
    [SerializeField] ParticleSystem Fire4 = null;

    [SerializeField] ParticleSystem FireEmbers1 = null;
    [SerializeField] ParticleSystem FireEmbers2 = null;
    [SerializeField] ParticleSystem FireEmbers3 = null;
    [SerializeField] ParticleSystem FireEmbers4 = null;
    [SerializeField] ParticleSystem FireEmbers5 = null;

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
            if ((hit.collider.gameObject.tag == "Bible") && Input.GetMouseButtonDown(0))
            {
                Destroy(gameObject);

                Fire1.Play();
                Fire2.Play();
                Fire3.Play();
                Fire4.Play();

                FireEmbers1.Play();
                FireEmbers2.Play();
                FireEmbers3.Play();
                FireEmbers4.Play();
                FireEmbers5.Play();
            }
        }
    }
}
