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

    [Header("Audio")]
    public WalkingSimulator.Presentation.SfxAudioSource leverAudio;
    public AudioClip leverSound;
    public WalkingSimulator.Presentation.SfxAudioSource spikesAudio;
    public AudioClip spikesSound;
    private float leverSoundEnd, spikesSoundEnd;

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
                leverSoundEnd = PlayOnce(leverAudio, leverSound, leverSoundEnd);

                StartCoroutine(WaitBeforeDoing());
            }
        }

        IEnumerator WaitBeforeDoing()
        {
            yield return new WaitForSeconds(3);

            spikes.Play("Spikes");
            spikesSoundEnd = PlayOnce(spikesAudio, spikesSound, spikesSoundEnd);
        }
    }

    // Repeated clicks restart the animations; the sound is not stacked while it is still playing.
    float PlayOnce(WalkingSimulator.Presentation.SfxAudioSource source, AudioClip clip, float end)
    {
        if (source == null || clip == null || Time.time < end) return end;
        source.Play(clip);
        return Time.time + clip.length;
    }
}
