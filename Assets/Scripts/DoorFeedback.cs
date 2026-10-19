using UnityEngine;

public class DoorFeedback : MonoBehaviour
{
    public Collider unlockTrigger;
    private CrosshairGUI crosshair;
    private bool opened;
    public bool IsLocked { get { return unlockTrigger != null && !unlockTrigger.enabled; } }

    void Start()
    {
        crosshair = FindObjectOfType<CrosshairGUI>();
    }

    public bool CanOpen { get { return !IsLocked && CanInteract; } }

    public void MarkOpened()
    {
        opened = true;
    }

    public bool CanInteract
    {
        get
        {
            if (opened || Camera.main == null) return false;
            Ray aim = Camera.main.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            RaycastHit hit;
            float range = crosshair != null ? crosshair.RayLength : 3f;
            return Physics.Raycast(aim, out hit, range, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<DoorFeedback>() == this;
        }
    }
}
