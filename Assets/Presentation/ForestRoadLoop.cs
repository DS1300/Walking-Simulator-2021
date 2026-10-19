using UnityEngine;

namespace WalkingSimulator.Presentation
{
    public class ForestRoadLoop : MonoBehaviour
    {
        public Transform[] roadSections;
        public Transform[] wheels = new Transform[0];
        public float speed = 4f;
        public float sectionLength = 80f;
        public float wheelRadius = .45f;
        private Vector3[] startingPositions;
        private float distance;
        private float windDistance;

        void Start()
        {
            startingPositions = new Vector3[roadSections.Length];
            for (int i = 0; i < roadSections.Length; i++) startingPositions[i] = roadSections[i].localPosition;
        }

        void Update()
        {
            if (PlayerPrefs.GetInt(GamePreferences.Prefix + "ReducedMotion", 0) == 1) return;
            float travel = speed * Time.unscaledDeltaTime;
            windDistance += travel;
            Shader.SetGlobalVector("_CTI_WindPositionOffset", transform.TransformVector(Vector3.forward * windDistance));
            distance = Mathf.Repeat(distance + travel, sectionLength * roadSections.Length);
            for (int i = 0; i < roadSections.Length; i++)
            {
                Vector3 position = startingPositions[i];
                position.z = Mathf.Repeat(position.z - distance + sectionLength, sectionLength * roadSections.Length) - sectionLength;
                roadSections[i].localPosition = position;
            }
            foreach (var wheel in wheels)
                if (wheel != null) wheel.Rotate(Vector3.right, travel / Mathf.Max(.1f, wheelRadius) * Mathf.Rad2Deg, Space.Self);
        }

        void OnDisable()
        {
            Shader.SetGlobalVector("_CTI_WindPositionOffset", Vector4.zero);
        }
    }
}
