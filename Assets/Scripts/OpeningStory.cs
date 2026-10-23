using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WalkingSimulator.Presentation
{
    public sealed class OpeningStory : MonoBehaviour
    {
        public Canvas canvas;
        public CanvasGroup curtain;
        public CanvasGroup textGroup;
        public RectTransform viewport;
        public TMP_Text storyText;
        [Min(1f)] public float scrollSpeed = 32f;
        [Min(0f)] public float fadeDuration = .8f;
        [Min(5f)] public float stationaryPageSeconds = 22f;

        bool playing, skipRequested;
        float skipAfter, previousTimeScale;
        readonly List<Behaviour> heldControls = new List<Behaviour>();
        readonly List<GameObject> heldTutorials = new List<GameObject>();
        bool holdingGameplay;
        CarArrivalAudio arrival;

        void Awake()
        {
            canvas.enabled = false;
            curtain.alpha = 0;
            textGroup.alpha = 0;
        }

        public void Begin(string sceneName)
        {
            if (playing) return;
            playing = true;
            skipAfter = Time.unscaledTime + .35f;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(Play(sceneName));
        }

        void Update()
        {
            if (playing && Time.unscaledTime >= skipAfter &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) ||
                 Input.GetMouseButtonDown(0)))
                Skip();
        }

        public void Skip()
        {
            if (playing) skipRequested = true;
        }

        IEnumerator Play(string sceneName)
        {
            canvas.enabled = true;
            curtain.blocksRaycasts = true;
            yield return Fade(curtain, 1);
            Canvas.ForceUpdateCanvases();
            bool reducedMotion = PlayerPrefs.GetInt(GamePreferences.Prefix + "ReducedMotion", 0) == 1;
            var rect = storyText.rectTransform;
            float height = viewport.rect.height;

            if (reducedMotion)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height - 240);
                rect.anchoredPosition = new Vector2(0, height * .5f - 120);
                storyText.overflowMode = TextOverflowModes.Page;
                storyText.ForceMeshUpdate();
                int pages = Mathf.Max(1, storyText.textInfo.pageCount);
                for (int page = 1; page <= pages && !skipRequested; page++)
                {
                    storyText.pageToDisplay = page;
                    yield return Fade(textGroup, 1);
                    float end = Time.unscaledTime + stationaryPageSeconds;
                    while (!skipRequested && Time.unscaledTime < end) yield return null;
                    yield return Fade(textGroup, 0);
                }
            }
            else
            {
                storyText.overflowMode = TextOverflowModes.Overflow;
                float textHeight = storyText.GetPreferredValues(storyText.text, rect.rect.width, 0).y;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, textHeight);
                rect.anchoredPosition = new Vector2(0, -height * .35f);
                if (!skipRequested) yield return Fade(textGroup, 1);
                float end = height * .5f + textHeight - 160;
                while (!skipRequested && rect.anchoredPosition.y < end)
                {
                    rect.anchoredPosition += Vector2.up * Mathf.Max(1, scrollSpeed) * Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return Fade(textGroup, 0);
            }

            SceneManager.sceneLoaded += HoldGameplay;
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone) yield return null;
            yield return null;
            if (arrival != null) yield return arrival.PlaySequence();
            yield return Fade(curtain, 0);
            ResumeGameplay();
            Destroy(gameObject);
        }

        IEnumerator Fade(CanvasGroup group, float target)
        {
            float start = group.alpha;
            float duration = Mathf.Max(0, fadeDuration);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, elapsed / duration));
                yield return null;
            }
            group.alpha = target;
        }

        void HoldGameplay(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= HoldGameplay;
            previousTimeScale = Time.timeScale;
            holdingGameplay = true;
            Time.timeScale = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                // The car arrival sounds play while the screen is still black (see Play).
                if (arrival == null)
                {
                    arrival = root.GetComponentInChildren<CarArrivalAudio>();
                    if (arrival != null) arrival.Claim();
                }
                foreach (var tutorial in root.GetComponentsInChildren<Tutorial.TutorialManager>(true))
                {
                    Hold(tutorial);
                    if (tutorial.canvasRect != null && tutorial.canvasRect.gameObject.activeSelf)
                    {
                        heldTutorials.Add(tutorial.canvasRect.gameObject);
                        tutorial.canvasRect.gameObject.SetActive(false);
                    }
                }
                foreach (var control in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (control is PlayerController || control is PlayerInput || control is CameraMovement ||
                        control is MouseLook || control is CameraZoom || control is PauseMenu || control is GrabRotate)
                        Hold(control);
            }
        }

        void Hold(Behaviour control)
        {
            if (!control.enabled) return;
            heldControls.Add(control);
            control.enabled = false;
        }

        void ResumeGameplay()
        {
            if (!holdingGameplay) return;
            Time.timeScale = previousTimeScale;
            foreach (var control in heldControls) if (control != null) control.enabled = true;
            foreach (var tutorial in heldTutorials) if (tutorial != null) tutorial.SetActive(true);
            heldControls.Clear();
            heldTutorials.Clear();
            holdingGameplay = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= HoldGameplay;
            ResumeGameplay();
        }
    }
}
