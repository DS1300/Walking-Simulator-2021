using System.Collections.Generic;
using UnityEngine;

namespace ShadowEntity.Demo
{
    /// <summary>
    /// Demo-scene only: an on-screen panel to preview the entity's states and camera viewpoints.
    /// Uses IMGUI, so it works with either input system. Not needed in your game.
    /// </summary>
    public class ShadowEntityDemo : MonoBehaviour
    {
        public Camera viewCamera;
        public Light flashlight;
        public ShadowEntityController[] entities;
        public Transform[] viewpoints;
        public bool showPanel = true;

        [Range(0f, 1f)] public float glitchIntensity = 1f;
        public float glitchDuration = 1.5f;

        readonly List<ShadowEntityController> copies = new List<ShadowEntityController>();
        int currentView;
        bool lookUp;

        public int CurrentView => currentView;

        void Start()
        {
            if (viewpoints != null && viewpoints.Length > 0) SetView(0);
        }

        public void SetView(int index)
        {
            if (viewpoints == null || index < 0 || index >= viewpoints.Length || viewpoints[index] == null) return;
            currentView = index;
            viewCamera.transform.SetPositionAndRotation(viewpoints[index].position, viewpoints[index].rotation);
        }

        IEnumerable<ShadowEntityController> All()
        {
            if (entities != null) foreach (var e in entities) if (e != null) yield return e;
            foreach (var c in copies) if (c != null) yield return c;
        }

        public void SetLookUp(bool on) { lookUp = on; foreach (var e in All()) e.SetLookUp(on); }
        public void Glitch() { foreach (var e in All()) e.PlayGlitch(glitchIntensity, glitchDuration); }
        public void AggressiveGlitch() { foreach (var e in All()) e.PlayAggressiveGlitch(glitchIntensity, glitchDuration * 1.6f); }
        public void Disappear() { foreach (var e in All()) e.PlayDisappear(); }
        public void Appear() { foreach (var e in All()) e.PlayAppear(); }
        public void HeadTilt() { foreach (var e in All()) e.PlayHeadTilt(); }
        public void FinalLunge() { foreach (var e in All()) e.PlayFinalLunge(); }
        public void ResetAll() { foreach (var e in All()) e.ResetToIdle(); }

        public void Duplicate()
        {
            if (entities == null || entities.Length == 0 || entities[0] == null) return;
            var entity = entities[0];
            int n = copies.Count + 1;
            var pos = entity.transform.position + entity.transform.right * (2.2f * ((n + 1) / 2) * (n % 2 == 0 ? -1 : 1));
            var copy = Instantiate(entity, pos, entity.transform.rotation);
            copy.name = entity.name + " (" + n + ")";
            copy.SetLookUp(lookUp);
            copies.Add(copy);
        }

        public void ClearCopies()
        {
            foreach (var c in copies) if (c != null) Destroy(c.gameObject);
            copies.Clear();
        }

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.H) showPanel = !showPanel;
            if (!showPanel) return;

            GUILayout.BeginArea(new Rect(12, 12, 250, Screen.height - 24), GUI.skin.box);
            GUILayout.Label("<b>Shadow Entity demo</b>  (H hides this)", new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.Label("States");
            bool newLook = GUILayout.Toggle(lookUp, " LookUp (stare upward)");
            if (newLook != lookUp) SetLookUp(newLook);
            if (GUILayout.Button("Glitch")) Glitch();
            if (GUILayout.Button("Aggressive glitch")) AggressiveGlitch();
            if (GUILayout.Button("Head tilt")) HeadTilt();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Final lunge")) FinalLunge();
            if (GUILayout.Button("Reset")) ResetAll();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Disappear")) Disappear();
            if (GUILayout.Button("Appear")) Appear();
            GUILayout.EndHorizontal();

            GUILayout.Label("Glitch intensity: " + glitchIntensity.ToString("0.00"));
            glitchIntensity = GUILayout.HorizontalSlider(glitchIntensity, 0f, 1f);
            GUILayout.Label("Glitch duration: " + glitchDuration.ToString("0.0") + " s");
            glitchDuration = GUILayout.HorizontalSlider(glitchDuration, 0.3f, 4f);

            GUILayout.Space(6);
            GUILayout.Label("Viewpoints");
            if (viewpoints != null)
                for (int i = 0; i < viewpoints.Length; i++)
                    if (viewpoints[i] != null && GUILayout.Toggle(currentView == i, " " + viewpoints[i].name, GUI.skin.button)) if (currentView != i) SetView(i);

            GUILayout.Space(6);
            if (flashlight != null) flashlight.enabled = GUILayout.Toggle(flashlight.enabled, " Flashlight");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate")) Duplicate();
            if (GUILayout.Button("Clear copies")) ClearCopies();
            GUILayout.EndHorizontal();
            GUILayout.Label("Copies: " + copies.Count + "   FPS: " + (1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)).ToString("0"));
            GUILayout.EndArea();
        }
    }
}
