
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SurfaceFootstepAudio : MonoBehaviour
    {
        public List<AudioClip> grass = new List<AudioClip>();
        public List<AudioClip> stone = new List<AudioClip>();
        public List<AudioClip> wood = new List<AudioClip>();
        public List<AudioClip> water = new List<AudioClip>();

        public float walkInterval = .48f;
        public float runInterval = .32f;
        [Min(.1f)] public float crouchStepDistance = 1.55f;

        [Range(0f, 1f)]
        public float volume = .55f;

        public TerrainLayer[] stoneLayers;
        public TerrainLayer[] woodLayers;
        public Material[] stoneMaterials;
        public Material[] woodMaterials;
        public Collider[] stoneSurfaces;
        public Collider[] woodSurfaces;

        public LayerMask groundMask = ~0;
        public float groundProbe = .35f;
        public Vector2 pitchRange = new Vector2(.97f, 1.03f);
        public string currentSurface = "Grass";

        AudioSource source;
        CharacterController controller;
        PlayerMovement movement;
        PlayerController player;

        readonly RaycastHit[] hits = new RaycastHit[16];

        readonly Dictionary<Collider, Material[]> materialCache =
            new Dictionary<Collider, Material[]>();

        float progress;
        AudioClip previous;
        bool paused;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            player = GetComponent<PlayerController>();

            var audioObject = new GameObject("Footsteps");
            audioObject.transform.SetParent(transform, false);

            source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 12f;
            source.dopplerLevel = 0f;
        }

        void Update()
        {
            bool pause = Time.timeScale <= 0f || PauseMenu.paused;

            source.volume = volume * AudioPreferences.SfxVolume;

            if (pause != paused)
            {
                if (pause)
                    source.Pause();
                else
                    source.UnPause();

                paused = pause;
            }

            Vector3 velocity = controller.velocity;
            if (player != null && player.status == Status.climbingLadder)
            {
                source.Stop();
                progress = 0f;
                return;
            }
            float speed = new Vector2(velocity.x, velocity.z).magnitude;

            bool walking = player == null || (player.enabled &&
                (player.status == Status.walking ||
                 player.status == Status.sprinting ||
                 player.status == Status.crouching));

            bool supported = controller.isGrounded ||
                (movement != null && movement.grounded);

            if (pause || !controller.enabled || !supported || !walking ||
                (movement != null && !movement.enabled) || speed < .15f)
            {
                progress = 0f;
                return;
            }

            float walkSpeed = movement != null ? movement.walkSpeed : 4f;
            float runSpeed = movement != null ? movement.runSpeed : 8f;

            float interval = Mathf.Lerp(
                walkInterval,
                runInterval,
                Mathf.InverseLerp(
                    walkSpeed,
                    Mathf.Max(walkSpeed + .1f, runSpeed),
                    speed
                )
            );

            if (player != null && player.status == Status.crouching)
                progress += speed * Time.deltaTime / Mathf.Max(.1f, crouchStepDistance);
            else
                progress += Time.deltaTime *
                    Mathf.Min(1f, speed / Mathf.Max(.1f, walkSpeed)) /
                    Mathf.Max(.1f, interval);

            if (progress < 1f)
                return;

            progress %= 1f;

            var clips = SelectSurface();

            if (clips == null || clips.Count == 0)
                return;

            int index = UnityEngine.Random.Range(0, clips.Count);

            if (clips.Count > 1 && clips[index] == previous)
            {
                index = (index +
                    UnityEngine.Random.Range(1, clips.Count)) % clips.Count;
            }

            var clip = clips[index];

            if (clip == null)
                return;

            previous = clip;

            source.pitch = UnityEngine.Random.Range(
                pitchRange.x,
                pitchRange.y
            );

            source.PlayOneShot(
                clip,
                UnityEngine.Random.Range(.92f, 1f)
            );
        }

        List<AudioClip> SelectSurface()
        {
            Bounds bounds = controller.bounds;

            Vector3 origin = new Vector3(
                bounds.center.x,
                bounds.min.y + .2f,
                bounds.center.z
            );

            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                hits,
                .2f + groundProbe,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            int nearest = -1;
            float distance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform) ||
                    hits[i].normal.y < .35f)
                {
                    continue;
                }

                if (hits[i].distance < distance)
                {
                    nearest = i;
                    distance = hits[i].distance;
                }
            }

            // Keep the last detected surface when the ground ray
            // briefly misses an edge or a gap between floor meshes.
            if (nearest < 0)
            {
                if (currentSurface == "Stone")
                    return stone;

                if (currentSurface == "Wood")
                    return wood;

                return grass;
            }

            RaycastHit hit = hits[nearest];

            if (Contains(woodSurfaces, hit.collider))
                return Choose("Wood", wood);

            if (Contains(stoneSurfaces, hit.collider))
                return Choose("Stone", stone);

            string surfaceName = hit.collider.gameObject.name;
            if (surfaceName == "Battlement" ||
                surfaceName.StartsWith("Battlement (", StringComparison.Ordinal))
                return Choose("Stone", stone);

            var terrain = hit.collider.GetComponent<Terrain>();

            if (terrain != null)
            {
                TerrainData data = terrain.terrainData;
                Vector3 point = hit.point - terrain.transform.position;

                int x = Mathf.Clamp(
                    Mathf.FloorToInt(
                        point.x / data.size.x * data.alphamapWidth
                    ),
                    0,
                    data.alphamapWidth - 1
                );

                int z = Mathf.Clamp(
                    Mathf.FloorToInt(
                        point.z / data.size.z * data.alphamapHeight
                    ),
                    0,
                    data.alphamapHeight - 1
                );

                float[,,] weights = data.GetAlphamaps(x, z, 1, 1);

                int dominant = 0;

                for (int i = 1; i < weights.GetLength(2); i++)
                {
                    if (weights[0, 0, i] >
                        weights[0, 0, dominant])
                    {
                        dominant = i;
                    }
                }

                var layers = data.terrainLayers;

                if (dominant < layers.Length)
                {
                    if (Contains(stoneLayers, layers[dominant]))
                        return Choose("Stone", stone);

                    if (Contains(woodLayers, layers[dominant]))
                        return Choose("Wood", wood);
                }

                return Choose("Grass", grass);
            }

            Material[] materials;

            if (!materialCache.TryGetValue(hit.collider, out materials))
            {
                var renderer = hit.collider.GetComponent<Renderer>();

                if (renderer == null)
                    renderer = hit.collider.GetComponentInParent<Renderer>();

                materials = renderer == null
                    ? new Material[0]
                    : renderer.sharedMaterials;

                materialCache.Add(hit.collider, materials);
            }

            // Explicit collider assignments take precedence
            // for mixed-material meshes.
            foreach (var material in materials)
            {
                if (Contains(woodMaterials, material))
                    return Choose("Wood", wood);
            }

            foreach (var material in materials)
            {
                if (Contains(stoneMaterials, material))
                    return Choose("Stone", stone);
            }

            return Choose("Grass", grass);
        }

        List<AudioClip> Choose(string surface, List<AudioClip> clips)
        {
            currentSurface = surface;
            return clips;
        }

        static bool Contains<T>(T[] values, T value)
        {
            return values != null &&
                   Array.IndexOf(values, value) >= 0;
        }
    }
}
