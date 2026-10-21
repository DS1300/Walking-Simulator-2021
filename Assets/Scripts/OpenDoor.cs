
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public Animator anim;
    public AudioClip creak;
    [Range(0f, 1f)] public float creakVolume = .7f;

    AudioSource audioSource;
    private bool canOpen;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && anim != null)
            audioSource = anim.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            GameObject soundObject = anim != null ? anim.gameObject : gameObject;
            audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void Update()
    {
        if (anim == null) return;

        var feedback = anim.GetComponent<DoorFeedback>();

        if (Input.GetMouseButtonDown(0) && (feedback != null ? feedback.CanOpen : canOpen))
        {
            if (feedback != null)
                feedback.MarkOpened();

            if (creak != null && audioSource != null)
                audioSource.PlayOneShot(creak, creakVolume * WalkingSimulator.Presentation.AudioPreferences.SfxVolume);

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

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            canOpen = false;
        }
    }
}


