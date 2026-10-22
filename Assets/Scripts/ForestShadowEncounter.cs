
using System.Collections;
using UnityEngine;

public class ForestShadowEncounter : MonoBehaviour
{
    [Header("Scene")]
    public Camera playerCamera;
    public Notes journal;
    public BoxCollider forestTrigger;
    public Transform[] spawnMarkers;
    public Transform entityPosition;
    public ShadowEncounterActor actor;

    [Header("Appearances")]
    public bool alreadyThere = true;
    public bool smokeManifestation = true;
    public bool optionalRareEncounter = true;
    [Range(0f, 1f)] public float rareProbability = .25f;
    public float encounterCooldown = 60f;
    public int maximumEncounters = 3;
    public int randomSeed = 137;

    [Tooltip("Zero-based marker indices: Position 5, then 1, 2, 3, 4.")]
    public int[] encounterOrder = { 4, 0, 1, 2, 3 };

    public float unavailableMarkerTimeout = 35f;
    public float encounterTimeout = 18f;
    public float gazeLossTolerance = .45f;

    [Header("Visibility")]
    public float minimumDistance = 4f;
    public float maximumDistance = 38f;
    public float gazeAngle = 25f;
    public float observationDuration = 1.5f;
    public float selectionInterval = .35f;
    public LayerMask obstructionMask = ~0;
    public Transform[] vegetationRoots;

    [Header("Bridge Crossing")]
    public bool bridgeCrossing = true;
    public Transform bridgeMarker;
    public Transform bridgeStart;
    public Transform bridgeEnd;
    [Min(.5f)] public float bridgeCrossingDuration = 2f;

    [Header("Normal Glitch")]
    [Range(0f, 1f)] public float glitchIntensity = .35f;
    public float glitchDuration = .6f;

    [Header("Audio")]
    public EncounterSound encounterHorrorSting = new EncounterSound();
    public EncounterSound appearSfx = new EncounterSound();
    public EncounterSound normalGlitchSfx = new EncounterSound();
    public EncounterSound disappearSfx = new EncounterSound();

    System.Random random;
    Terrain[] terrains;
    readonly RaycastHit[] sightHits = new RaycastHit[64];

    int encounters;
    int orderIndex;
    bool running;
    bool bridgeRunning;
    bool discovered;

    float nextSelection;
    float markerWait;

    Coroutine forestRoutine;
    Coroutine stingWatch;
    TowerShadowEncounter tower;

    public bool IsActive { get { return running; } }
    public Collider[] sightIgnoredColliders;

    void Start()
    {
        random = new System.Random(randomSeed);
        terrains = Terrain.activeTerrains;
        tower = FindObjectOfType<TowerShadowEncounter>();

        if (actor != null)
            actor.HideImmediately();
    }

    bool Available()
    {
        return !PauseMenu.paused &&
               Time.timeScale > 0f &&
               !ShadowEncounterView.InUse &&
               (tower == null || !tower.ControlsCamera) &&
               (journal == null || !journal.IsReading);
    }

    bool InsideForest()
    {
        Vector3 position = playerCamera.transform.position;

        if (forestTrigger != null)
        {
            Vector3 p =
                forestTrigger.transform.InverseTransformPoint(position)
                - forestTrigger.center;

            Vector3 half = forestTrigger.size * .5f;

            if (Mathf.Abs(p.x) < half.x &&
                Mathf.Abs(p.y) < half.y &&
                Mathf.Abs(p.z) < half.z)
                return true;
        }

        if (spawnMarkers != null)
        {
            foreach (var marker in spawnMarkers)
            {
                if (marker != null &&
                    Vector3.Distance(position, marker.position) <= maximumDistance)
                    return true;
            }
        }

        return false;
    }

    bool TerrainBlocks(Vector3 origin, Vector3 target)
    {
        int samples = Mathf.Max(
            1,
            Mathf.CeilToInt(Vector3.Distance(origin, target) / .5f)
        );

        for (int i = 1; i < samples; i++)
        {
            Vector3 point = Vector3.Lerp(
                origin, target, (float)i / samples
            );

            foreach (var terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null)
                    continue;

                Vector3 local = point - terrain.transform.position;
                Vector3 size = terrain.terrainData.size;

                if (local.x < 0f || local.z < 0f ||
                    local.x > size.x || local.z > size.z)
                    continue;

                if (terrain.SampleHeight(point) +
                    terrain.transform.position.y > point.y + .05f)
                    return true;
            }
        }

        return false;
    }

    bool BlocksView(RaycastHit hit)
    {
        if (hit.collider is TerrainCollider ||
            hit.transform.IsChildOf(actor.entity.transform) ||
            hit.collider.GetComponentInParent<PlayerController>() != null)
            return false;

        if (sightIgnoredColliders != null &&
            System.Array.IndexOf(sightIgnoredColliders, hit.collider) >= 0)
            return false;

        if (vegetationRoots != null)
        {
            foreach (var root in vegetationRoots)
            {
                if (root != null && hit.transform.IsChildOf(root))
                    return false;
            }
        }

        return true;
    }

    bool ClearView(Vector3 point)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 delta = point - origin;

        if (TerrainBlocks(origin, point))
            return false;

        if (delta.sqrMagnitude < .0001f)
            return true;

        int mask = obstructionMask.value;

        int count = Physics.RaycastNonAlloc(
            origin,
            delta.normalized,
            sightHits,
            delta.magnitude,
            mask,
            QueryTriggerInteraction.Ignore
        );

        if (count == sightHits.Length)
        {
            foreach (var hit in Physics.RaycastAll(
                origin,
                delta.normalized,
                delta.magnitude,
                mask,
                QueryTriggerInteraction.Ignore))
            {
                if (BlocksView(hit))
                    return false;
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                if (BlocksView(sightHits[i]))
                    return false;
            }
        }

        return true;
    }

    bool InFrame(Vector3 point, float margin = 0f)
    {
        Vector3 screen = playerCamera.WorldToViewportPoint(point);

        return screen.z > 0f &&
               screen.x > -margin && screen.x < 1f + margin &&
               screen.y > -margin && screen.y < 1f + margin;
    }

    bool Observed()
    {
        Vector3 point = actor.lookTarget.position;
        Vector3 delta = point - playerCamera.transform.position;

        return delta.magnitude <= maximumDistance &&
               InFrame(point) &&
               Vector3.Angle(
                   playerCamera.transform.forward, delta
               ) <= Mathf.Min(
                   gazeAngle,
                   playerCamera.fieldOfView * .45f
               ) &&
               ClearView(point);
    }

    void AdvanceMarker()
    {
        orderIndex++;
        markerWait = 0f;
    }

    void Update()
    {
        if (running ||
            playerCamera == null ||
            actor == null ||
            !Available() ||
            spawnMarkers == null ||
            encounterOrder == null ||
            orderIndex >= encounterOrder.Length ||
            encounters >= maximumEncounters ||
            !InsideForest() ||
            Time.time < nextSelection)
            return;

        nextSelection = Time.time + Mathf.Max(.1f, selectionInterval);

        int index = encounterOrder[orderIndex];

        if (index < 0 ||
            index >= spawnMarkers.Length ||
            spawnMarkers[index] == null)
        {
            AdvanceMarker();
            return;
        }

        Transform marker = spawnMarkers[index];
        Vector3 target = marker.position + Vector3.up * .8f;

        float distance = Vector3.Distance(
            playerCamera.transform.position, target
        );

        if (bridgeCrossing && marker == bridgeMarker)
        {
            AdvanceMarker();
            return;
        }

        if (distance > maximumDistance)
        {
            for (int later = orderIndex + 1;
                 later < encounterOrder.Length;
                 later++)
            {
                int next = encounterOrder[later];

                if (next >= 0 &&
                    next < spawnMarkers.Length &&
                    spawnMarkers[next] != null &&
                    Vector3.Distance(
                        playerCamera.transform.position,
                        spawnMarkers[next].position
                    ) < maximumDistance * .6f)
                {
                    AdvanceMarker();
                    break;
                }
            }

            return;
        }

        markerWait += Mathf.Max(.1f, selectionInterval);

        if (markerWait >= Mathf.Max(5f, unavailableMarkerTimeout))
        {
            AdvanceMarker();
            return;
        }

        if (distance < minimumDistance || !ClearView(target))
            return;

        bool idle = alreadyThere && !InFrame(target, .2f);
        bool smoke = smokeManifestation && InFrame(target);

        if (!idle && !smoke)
            return;

        bool rare = encounters >= 2;

        if (rare &&
            (!optionalRareEncounter ||
             random.NextDouble() > rareProbability))
        {
            AdvanceMarker();
            nextSelection = Time.time + encounterCooldown;
            return;
        }

        running = true;
        forestRoutine = StartCoroutine(Haunt(marker, idle));
    }

    IEnumerator WatchSting(float revealDelay)
    {
        if (revealDelay > 0f)
            yield return new WaitForSeconds(revealDelay);

        while (running)
        {
            if (Available() && Observed())
            {
                actor.sounds.PlaySting(encounterHorrorSting);
                discovered = true;
                yield break;
            }

            yield return null;
        }
    }

    IEnumerator Haunt(Transform marker, bool idle)
    {
        discovered = false;

        entityPosition.SetPositionAndRotation(
            marker.position, marker.rotation
        );

        actor.Face(playerCamera.transform);

        if (idle)
        {
            yield return actor.ActivateHidden();

            float wait = 0f;

            while (wait < 4f &&
                   (!Available() || InFrame(actor.lookTarget.position)))
            {
                if (Available())
                    wait += Time.deltaTime;

                yield return null;
            }

            if (InFrame(actor.lookTarget.position) &&
                smokeManifestation)
            {
                stingWatch = StartCoroutine(WatchSting(.6f));

                yield return actor.Appear(
                    appearSfx,
                    actor.appearDuration
                );
            }
            else
            {
                actor.entity.ResetToIdle();
                stingWatch = StartCoroutine(WatchSting(0f));
            }
        }
        else
        {
            stingWatch = StartCoroutine(WatchSting(.6f));

            yield return actor.Appear(
                appearSfx,
                actor.appearDuration
            );
        }

        float elapsed = 0f;
        float observed = 0f;
        float lost = 0f;

        while (elapsed < encounterTimeout &&
               observed < observationDuration)
        {
            if (Available())
            {
                elapsed += Time.deltaTime;

                if (Observed())
                {
                    observed += Time.deltaTime;
                    lost = 0f;
                }
                else
                {
                    lost += Time.deltaTime;

                    if (lost > gazeLossTolerance)
                        observed = 0f;
                }
            }

            yield return null;
        }

        if (observed >= observationDuration)
        {
            yield return actor.Glitch(
                normalGlitchSfx,
                false,
                glitchIntensity,
                glitchDuration
            );
        }

        if (stingWatch != null)
        {
            StopCoroutine(stingWatch);
            stingWatch = null;
        }

        yield return actor.Disappear(disappearSfx);

        actor.entity.gameObject.SetActive(false);

        if (discovered)
            encounters++;

        AdvanceMarker();

        nextSelection = Time.time +
            Mathf.Max(1f, encounterCooldown);

        running = false;
        forestRoutine = null;
    }

    // Bridge encounters have priority over regular forest encounters.
    public bool TryStartBridge()
    {
        if (!bridgeCrossing ||
            bridgeRunning ||
            !Available() ||
            actor == null ||
            entityPosition == null ||
            bridgeStart == null ||
            bridgeEnd == null)
            return false;

        // Cancel an ordinary forest apparition if it conflicts with
        // the dedicated bridge crossing. Preserve its marker progress.
        if (running)
        {
            if (forestRoutine == null)
                return false;

            StopCoroutine(forestRoutine);
            forestRoutine = null;

            if (stingWatch != null)
            {
                StopCoroutine(stingWatch);
                stingWatch = null;
            }

            actor.HideImmediately();
            running = false;
        }

        running = true;
        bridgeRunning = true;

        StartCoroutine(BridgeEncounter());
        return true;
    }

    IEnumerator BridgeEncounter()
    {
        discovered = false;

        entityPosition.position = bridgeStart.position;

        // Do not play the smoke manifestation here.
        // Reveal the physical creature before starting FinalLunge.
        yield return actor.ActivateHidden();

        actor.entity.ResetToIdle();

        // Allow the Animator to apply its visible Idle state.
        yield return null;

        stingWatch = StartCoroutine(WatchSting(0f));

        yield return CrossBridge();

        if (stingWatch != null)
        {
            StopCoroutine(stingWatch);
            stingWatch = null;
        }

        // Disappear only after reaching the destination.
        yield return actor.Disappear(disappearSfx);

        actor.entity.gameObject.SetActive(false);

        nextSelection = Time.time +
            Mathf.Max(1f, encounterCooldown);

        bridgeRunning = false;
        running = false;
    }

    IEnumerator CrossBridge()
    {
        Vector3 start = entityPosition.position;
        Vector3 destination = bridgeEnd.position;

        Vector3 direction = destination - start;
        direction.y = 0f;

        if (direction.sqrMagnitude < .01f)
            yield break;

        actor.entity.transform.rotation =
            Quaternion.LookRotation(direction);

        Animator animator =
            actor.entity.GetComponent<Animator>();

        int glitchLayer = -1;
        float previousGlitchWeight = 0f;
        bool previousRootMotion = false;

        if (animator != null)
        {
            glitchLayer = animator.GetLayerIndex("Glitch");

            if (glitchLayer >= 0)
            {
                previousGlitchWeight =
                    animator.GetLayerWeight(glitchLayer);

                // Prevent glitch overlay during the crossing.
                animator.SetLayerWeight(glitchLayer, 0f);
            }

            previousRootMotion = animator.applyRootMotion;
            animator.applyRootMotion = false;
        }

        actor.entity.PlayFinalLunge();

        float elapsed = 0f;
        float duration = Mathf.Max(.5f, bridgeCrossingDuration);

        while (elapsed < duration)
        {
            if (Available())
            {
                elapsed += Time.deltaTime;

                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration)
                );

                entityPosition.position =
                    Vector3.Lerp(start, destination, t);
            }

            yield return null;
        }

        entityPosition.position = destination;

        if (animator != null)
        {
            animator.applyRootMotion = previousRootMotion;

            if (glitchLayer >= 0)
                animator.SetLayerWeight(
                    glitchLayer,
                    previousGlitchWeight
                );
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (actor != null)
            actor.HideImmediately();

        forestRoutine = null;
        stingWatch = null;
        bridgeRunning = false;
        running = false;
    }
}
