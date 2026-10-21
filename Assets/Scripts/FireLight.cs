using UnityEngine;

public class FireLight : MonoBehaviour
{
    public ParticleSystem fire;
    public Light fireLight;
    public float intensity = 1.6f;

    void Update()
    {
        if (fireLight == null) return;
        bool burning = fire != null && fire.isPlaying;
        fireLight.enabled = burning;
        if (burning) fireLight.intensity = intensity;
    }
}
