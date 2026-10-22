using UnityEngine;

namespace WalkingSimulator.Tutorial
{
    // Scene anchor for a world-contextual tutorial hint. Move it freely in the Scene View.
    public sealed class TutorialHint : MonoBehaviour
    {
        public enum Kind { Vault, PickUp, Crouch }

        public Kind kind;
        [TextArea(1, 3)] public string text = "SPACE — Vault";
        [Tooltip("The hint appears while the player is within this distance of the anchor.")]
        public float activationDistance = 7f;
        [Tooltip("Where the hint text is drawn, relative to the anchor.")]
        public Vector3 labelOffset = new Vector3(0f, 1.2f, 0f);
        [Tooltip("Crouch only: the passage volume, in anchor space, the player must crouch through.")]
        public Vector3 passageSize = new Vector3(2f, 1.5f, 3f);
        [Tooltip("Crouch only: distance the player must travel crouched inside the passage.")]
        public float requiredCrouchTravel = 1f;

        public bool Completed { get; set; }

        public Vector3 LabelPosition { get { return transform.position + transform.rotation * labelOffset; } }

        public bool ContainsPoint(Vector3 world)
        {
            Vector3 local = Quaternion.Inverse(transform.rotation) * (world - transform.position);
            Vector3 half = passageSize * .5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        void OnDrawGizmos()
        {
            Color color = kind == Kind.Vault ? new Color(.95f, .75f, .3f)
                : kind == Kind.PickUp ? new Color(.45f, .8f, .95f) : new Color(.6f, .9f, .5f);
            Gizmos.color = new Color(color.r, color.g, color.b, .35f);
            Gizmos.DrawWireSphere(transform.position, activationDistance);
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, .15f);
            Gizmos.DrawLine(transform.position, LabelPosition);
            Gizmos.DrawWireCube(LabelPosition, new Vector3(.6f, .2f, .02f));
            if (kind == Kind.Crouch)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, passageSize);
                Gizmos.matrix = Matrix4x4.identity;
            }
#if UNITY_EDITOR
            UnityEditor.Handles.Label(LabelPosition, text);
#endif
        }
    }
}
