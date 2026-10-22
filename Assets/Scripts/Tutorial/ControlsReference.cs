using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WalkingSimulator.Tutorial
{
    // Read-only Controls page shown on top of the Settings page.
    // Settings stays active underneath, so the existing Escape and Back handling keep working.
    public sealed class ControlsReference : MonoBehaviour
    {
        public GameObject settingsPanel;
        public GameObject controlsPanel;
        public Selectable openButton;
        public Selectable backButton;

        void Start()
        {
            if (controlsPanel != null) controlsPanel.SetActive(false);
        }

        void Update()
        {
            if (controlsPanel != null && controlsPanel.activeSelf &&
                (settingsPanel == null || !settingsPanel.activeInHierarchy))
                controlsPanel.SetActive(false);
        }

        public void Show()
        {
            controlsPanel.SetActive(true);
            Select(backButton);
        }

        public void Hide()
        {
            controlsPanel.SetActive(false);
            Select(openButton);
        }

        static void Select(Selectable target)
        {
            if (target != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
