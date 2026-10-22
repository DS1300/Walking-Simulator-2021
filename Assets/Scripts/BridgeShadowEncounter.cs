using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BridgeShadowEncounter : MonoBehaviour
{
    public ForestShadowEncounter encounter;
    public bool encounterEnabled = true;
    public bool HasPlayed { get { return played; } }

    BoxCollider approach;
    bool played;

    void Awake()
    {
        approach = GetComponent<BoxCollider>();
    }

    void Update()
    {
        if (played || !encounterEnabled || encounter == null || encounter.playerCamera == null ||
            !approach.enabled) return;

        Vector3 point = approach.transform.InverseTransformPoint(encounter.playerCamera.transform.position);
        point -= approach.center;
        Vector3 half = approach.size * .5f;
        if (Mathf.Abs(point.x) > half.x || Mathf.Abs(point.y) > half.y || Mathf.Abs(point.z) > half.z) return;

        if (encounter.TryStartBridge()) played = true;
    }
}
