using UnityEngine;
using WalkingSimulator.Presentation;

public class LadderClimbAudio : MonoBehaviour
{
    public PlayerController player;
    public PlayerMovement movement;
    public AudioSource source;
    public AudioClip[] clips;
    public float distancePerSound = .85f;
    [Range(0f, 1f)] public float volume = .5f;
    public Vector2 pitchRange = new Vector2(.96f, 1.04f);

    float distance;
    int previous = -1;
    bool paused;

    void Update()
    {
        bool pause = PauseMenu.paused || Time.timeScale <= 0f;
        source.volume = volume * AudioPreferences.SfxVolume;
        if (pause != paused)
        {
            if (pause) source.Pause();
            else source.UnPause();
            paused = pause;
        }
        if (pause) return;

        float speed = Mathf.Abs(movement.controller.velocity.y);
        if (!player.enabled || !movement.enabled || player.status != Status.climbingLadder ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) < .02f || speed < .05f)
        {
            source.Stop();
            distance = 0f;
            return;
        }
        distance += speed * Time.deltaTime;
        if (distance < distancePerSound || source.isPlaying || clips.Length == 0) return;
        distance = 0f;
        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == previous) index = (index + Random.Range(1, clips.Length)) % clips.Length;
        previous = index;
        source.clip = clips[index];
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.Play();
    }

    void OnDisable()
    {
        if (source != null) source.Stop();
        distance = 0f;
    }
}
