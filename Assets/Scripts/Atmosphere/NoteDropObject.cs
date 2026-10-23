using System.Collections;
using UnityEngine;

// Drops a hidden object from this transform's position after a specific note is read in the world and closed,
// e.g. the child's ball falling onto the grave after "A Mother's Grief". Happens once.
public class NoteDropObject : MonoBehaviour
{
    public Notes notes;
    [Tooltip("Index of the note in Notes.pages.")]
    public int noteIndex;
    [Tooltip("Kept inactive in the scene until the drop.")]
    public Rigidbody dropObject;
    [Min(0f)] public float delay = .35f;
    public Vector3 spin = new Vector3(2f, 0f, 1.5f);
    [Header("Settle")]
    [Tooltip("Seconds after the first bounce before the object gets heavy damping, so it stays where it landed.")]
    [Min(0f)] public float settleAfterBounce = .4f;
    [Min(0f)] public float settleDrag = 3f;
    [Min(0f)] public float settleAngularDrag = 20f;

    bool dropped;

    void OnEnable()
    {
        if (notes != null) notes.NoteClosed += OnNoteClosed;
    }

    void OnDisable()
    {
        if (notes != null) notes.NoteClosed -= OnNoteClosed;
    }

    void OnNoteClosed(int index, bool openedInWorld)
    {
        if (dropped || index != noteIndex || !openedInWorld || dropObject == null) return;
        dropped = true;
        StartCoroutine(Drop());
    }

    IEnumerator Drop()
    {
        yield return new WaitForSeconds(delay);
        dropObject.transform.position = transform.position;
        dropObject.gameObject.SetActive(true);
        dropObject.velocity = Vector3.zero;
        dropObject.angularVelocity = spin;

        float timeout = Time.time + 4f;
        while (dropObject.velocity.y <= .2f && Time.time < timeout) yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(settleAfterBounce);
        dropObject.drag = settleDrag;
        dropObject.angularDrag = settleAngularDrag;
    }
}
