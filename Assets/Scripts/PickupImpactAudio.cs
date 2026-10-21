using UnityEngine;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PickupImpactAudio : MonoBehaviour
    {
        public AudioClip[] impacts;
        [Range(0f, 1f)] public float volume = .55f;
        public float minimumVelocity = 1.1f;
        AudioSource source;
        float lastImpact;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            if (impacts == null || impacts.Length == 0) impacts = new[] { Resources.Load<AudioClip>("Audio/woodImpact"), Resources.Load<AudioClip>("Audio/metalImpact") };
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Time.time - lastImpact < .12f || collision.relativeVelocity.magnitude < minimumVelocity || impacts == null || impacts.Length == 0) return;
            lastImpact = Time.time;
            source.pitch = Random.Range(.94f, 1.06f);
            source.PlayOneShot(impacts[Random.Range(0, impacts.Length)], volume * Mathf.Clamp01(collision.relativeVelocity.magnitude / 6f) * AudioPreferences.SfxVolume);
        }
    }
}

