using System.Collections;
using UnityEngine;

namespace WalkingSimulator.Presentation
{
    [RequireComponent(typeof(ParticleSystem))]
    public class RoamingBatSwarm : MonoBehaviour
    {
        public Transform[] waypoints;
        public float flightSeconds = 28;
        public Vector2 waitingSeconds = new Vector2(18, 35);
        public float initialDelay;
        public bool varyDirection = true;
        ParticleSystem swarm;

        IEnumerator Start()
        {
            swarm = GetComponent<ParticleSystem>();
            swarm.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (waypoints == null || waypoints.Length < 2) yield break;
            foreach (Transform point in waypoints) if (point == null) yield break;
            yield return new WaitForSeconds(initialDelay);
            while (true)
            {
                bool reverse = varyDirection && Random.value > .5f;
                transform.position = Position(reverse ? 1 : 0);
                var emission = swarm.emission;
                emission.enabled = true;
                swarm.Play();
                float elapsed = 0;
                float duration = Mathf.Max(flightSeconds, swarm.main.startLifetime.constantMax + 2);
                while (elapsed < duration)
                {
                    float progress = Mathf.Clamp01(elapsed / duration);
                    transform.position = Position(reverse ? 1 - progress : progress);
                    if (duration - elapsed <= swarm.main.startLifetime.constantMax) emission.enabled = false;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                swarm.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                yield return new WaitForSeconds(Random.Range(waitingSeconds.x, waitingSeconds.y));
            }
        }

        public Vector3 Position(float progress)
        {
            float value = Mathf.Clamp01(progress) * (waypoints.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(value), waypoints.Length - 2);
            float t = value - i;
            Vector3 a = waypoints[Mathf.Max(0, i - 1)].position;
            Vector3 b = waypoints[i].position;
            Vector3 c = waypoints[i + 1].position;
            Vector3 d = waypoints[Mathf.Min(waypoints.Length - 1, i + 2)].position;
            return .5f * ((2 * b) + (-a + c) * t +
                (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t);
        }

        void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            foreach (Transform point in waypoints) if (point == null) return;
            Gizmos.color = new Color(.6f, .8f, 1);
            Vector3 previous = Position(0);
            for (int i = 1; i <= 60; i++)
            {
                Vector3 next = Position(i / 60f);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
