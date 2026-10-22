using UnityEngine;

public class TowerLadderAccess : MonoBehaviour
{
    public BoxCollider ladder;
    public BoxCollider descentEntry;
    public Transform upperLanding;
    public float contactTolerance = .2f;
    public float alignmentSpeed = 2f;

    public Vector3 Outward { get { return ladder.transform.forward; } }

    public Vector3 FacePoint(Vector3 position)
    {
        Vector3 local = ladder.transform.InverseTransformPoint(position);
        local.x = ladder.center.x;
        local.z = ladder.center.z + ladder.size.z * .5f;
        return ladder.transform.TransformPoint(local);
    }

    public bool AtEntry(Vector3 position)
    {
        Vector3 local = descentEntry.transform.InverseTransformPoint(position) - descentEntry.center;
        Vector3 half = descentEntry.size * .5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    public bool NearFace(Vector3 position, float radius)
    {
        Vector3 local = ladder.transform.InverseTransformPoint(position) - ladder.center;
        Vector3 half = ladder.size * .5f;
        float side = radius / Mathf.Abs(ladder.transform.lossyScale.x);
        float distance = Vector3.Dot(position - FacePoint(position), Outward);
        return Mathf.Abs(local.x) <= half.x + side * .5f &&
            local.y >= -half.y - side && local.y <= half.y + side * 3f &&
            distance >= -.05f && distance <= radius + contactTolerance;
    }
}
