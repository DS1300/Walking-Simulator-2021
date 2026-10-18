using UnityEngine;

public class DoorFeedback : MonoBehaviour
{
    public Collider unlockTrigger;
    public bool IsLocked { get { return unlockTrigger != null && !unlockTrigger.enabled; } }
}
