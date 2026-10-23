
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace WalkingSimulator.Presentation
{
    public sealed class MainMenu : MonoBehaviour
    {
        public Texture2D backdrop;
        public TMP_FontAsset headingFont;
        public TMP_FontAsset buttonFont, italicFont, valueFont;
        public bool isPauseMenu;
        public PauseMenu pauseController;
        public Notes journal;
        public GameObject journalPanel;
        public Button[] journalButtons;
        public static bool AudioSettingsOpen { get; private set; }
        public TMP_FontAsset bodyFont;
        public string gameScene = "The Game";
        public OpeningStory openingStory;

        [SerializeField] GameObject ui, home, settings, confirmation;
        [SerializeField] TMP_Text status, resolutionText, fullscreenText, qualityText, confirmText;
        [SerializeField] Slider volume, musicVolume, sfxVolume, sensitivity;
        [SerializeField] TMP_Text volumeValue, musicVolumeValue, sfxVolumeValue, sensitivityValue;
        [SerializeField] Button playButton, backButton, keepButton;

        TMP_Text limitText, motionText;
        int[] limits = { 60, 90, 120, 144, -1 };
        int limitIndex;
        bool reducedMotion;

        Vector2Int[] resolutions;
        int resolutionIndex, qualityIndex, previousWidth, previousHeight;
        bool fullscreen, previousFullscreen, loading;
        float confirmationEnd;

        [SerializeField] Texture2D shade;

        static readonly Color Ink = new Color(212f / 255, 208f / 255, 196f / 255);
        static readonly Color Muted = new Color(116f / 255, 134f / 255, 154f / 255);
        static readonly Color Gold = new Color(209f / 255, 147f / 255, 78f / 255);

        void Awake()
        {
            Build();

            if (Application.isPlaying)
                BindSavedInterface();
        }

        public void Build()
        {
            if (ui != null) return;

            if (Application.isPlaying && !isPauseMenu)
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (bodyFont == null) return;
            if (headingFont == null) headingFont = bodyFont;

            ui = new GameObject("Menu UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));

            var bg = new GameObject("Cemetery Backdrop", typeof(RectTransform),
                typeof(RawImage), typeof(AspectRatioFitter));

            bg.transform.SetParent(ui.transform, false);
            Stretch(bg.GetComponent<RectTransform>());

            bg.GetComponent<RawImage>().texture = backdrop;
            bg.GetComponent<RawImage>().color = new Color(.63f, .68f, .70f);
            bg.GetComponent<RawImage>().raycastTarget = false;

            var aspect = bg.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 16f / 9f;

            shade = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            shade.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < 128; i++)
                shade.SetPixel(i, 0, new Color(.015f, .025f, .03f,
                    Mathf.Lerp(.96f, .1f, Mathf.SmoothStep(0, 1, i / 127f))));

            shade.Apply();

            var wash = new GameObject("Shade", typeof(RectTransform), typeof(RawImage));
            wash.transform.SetParent(ui.transform, false);
            Stretch(wash.GetComponent<RectTransform>());
            wash.GetComponent<RawImage>().texture = shade;
            wash.GetComponent<RawImage>().raycastTarget = false;

            home = Panel("Home");
            settings = Panel("Settings");
            confirmation = Panel("Display Confirmation");

            Label(home.transform, "A WALK THROUGH THE DARK", 140, 165, 750, 32, 16, Gold);
            Label(home.transform, "WALKING\nSIMULATOR", 136, 235, 720, 150, 58, Ink, headingFont);
            Line(home.transform, 140, 423, 90, Gold);
            Label(home.transform, "Beyond the gate, the silence waits.",
                140, 453, 780, 42, 25, Muted, italicFont);

            playButton = ButtonAt(home.transform,
                isPauseMenu ? "Resume" : "Play Now", 140, 566, 390, PlayNow);

            ButtonAt(home.transform, "Settings", 140, 646, 390, ShowSettings);
            ButtonAt(home.transform, "Exit", 140, 726, 390, Exit);

            status = Label(home.transform, "", 140, 853, 800, 50, 20, Ink);

            Label(home.transform, "W A L K I N G   S I M U L A T O R   /   2 0 2 1",
                140, 985, 1000, 35, 15, Muted);

            Label(settings.transform, "SETTINGS", 140, 154, 900, 80, 56, Ink, headingFont);
            Label(settings.transform, "Tune the experience.", 140, 246, 700, 40, 23, Muted);

            volume = SliderAt(settings.transform, "Master Volume", 340, 0, 1);
            musicVolume = SliderAt(settings.transform, "Music Volume", 420, 0, 1);
            sfxVolume = SliderAt(settings.transform, "Sound Effects", 500, 0, 1);
            sensitivity = SliderAt(settings.transform, "Mouse Sensitivity", 580, 1f, 20f);

            Label(settings.transform, "Fullscreen", 140, 665, 350, 40, 24, Ink);

            fullscreenText = ButtonAt(settings.transform, "", 560, 652, 440,
                () => { fullscreen = !fullscreen; RefreshChoices(); })
                .GetComponentInChildren<TMP_Text>();

            fullscreenText.transform.parent.name = "Fullscreen Choice";

            Label(settings.transform, "Resolution", 140, 755, 350, 40, 24, Ink);

            resolutionText = ButtonAt(settings.transform, "", 560, 742, 440,
                () => {
                    resolutionIndex = (resolutionIndex + 1) % resolutions.Length;
                    RefreshChoices();
                }).GetComponentInChildren<TMP_Text>();

            resolutionText.transform.parent.name = "Resolution Choice";

            Label(settings.transform, "Graphics Quality", 140, 845, 350, 40, 24, Ink);

            qualityText = ButtonAt(settings.transform, "", 560, 832, 440,
                () => {
                    qualityIndex = (qualityIndex + 1) % QualitySettings.names.Length;
                    RefreshChoices();
                }).GetComponentInChildren<TMP_Text>();

            qualityText.transform.parent.name = "Quality Choice";

            ButtonAt(settings.transform, "Apply", 140, 1040, 350, ApplySettings);
            backButton = ButtonAt(settings.transform, "Back", 560, 1040, 350, ShowHome);

            Label(settings.transform, "Frame Limit", 140, 915, 350, 40, 24, Ink);

            limitText = ButtonAt(settings.transform, "", 560, 902, 440, CycleLimit)
                .GetComponentInChildren<TMP_Text>();

            limitText.transform.parent.name = "Frame Limit Choice";

            Label(settings.transform, "Reduced Menu Motion", 140, 985, 400, 40, 24, Ink);

            motionText = ButtonAt(settings.transform, "", 560, 972, 440, ToggleMotion)
                .GetComponentInChildren<TMP_Text>();

            motionText.transform.parent.name = "Motion Choice";

            var curtain = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            curtain.transform.SetParent(confirmation.transform, false);
            Stretch(curtain.GetComponent<RectTransform>());
            curtain.GetComponent<Image>().color = new Color(.015f, .025f, .03f, .97f);

            Label(confirmation.transform, "KEEP DISPLAY SETTINGS?",
                270, 330, 1300, 80, 50, Ink, headingFont);

            confirmText = Label(confirmation.transform, "",
                270, 450, 1200, 70, 26, Muted);

            keepButton = ButtonAt(confirmation.transform, "Keep",
                270, 590, 370, KeepDisplay);

            ButtonAt(confirmation.transform, "Revert", 710, 590, 370, RevertDisplay);

            settings.SetActive(false);
            confirmation.SetActive(false);

            if (Application.isPlaying)
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        void Update()
        {
            if (confirmation != null && confirmation.activeSelf)
            {
                float remaining = confirmationEnd - Time.unscaledTime;

                confirmText.text = "Reverting automatically in " +
                    Mathf.CeilToInt(Mathf.Max(0, remaining)) + " seconds.";

                if (remaining <= 0 ||
                    (!isPauseMenu && Input.GetKeyDown(KeyCode.Escape)))
                    RevertDisplay();
            }
            else if (!isPauseMenu && !loading &&
                     settings != null && settings.activeSelf &&
                     Input.GetKeyDown(KeyCode.Escape))
            {
                ShowHome();
            }
        }

        void BindSavedInterface()
        {
            if (!isPauseMenu)
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            foreach (var button in ui.GetComponentsInChildren<Button>(true))
            {
                button.onClick.RemoveAllListeners();

                switch (button.name)
                {
                    case "Resume":
                    case "Play Now":
                        button.onClick.AddListener(PlayNow);
                        break;

                    case "Settings":
                        button.onClick.AddListener(ShowSettings);
                        break;

                    case "Journal":
                        button.onClick.AddListener(ShowJournal);
                        break;

                    case "Journal Back":
                        button.onClick.AddListener(ShowHome);
                        break;

                    case "Exit":
                        button.onClick.AddListener(Exit);
                        break;

                    case "Apply":
                        button.onClick.AddListener(ApplySettings);
                        break;

                    case "Back":
                        button.onClick.AddListener(ShowHome);
                        break;

                    case "Keep":
                        button.onClick.AddListener(KeepDisplay);
                        break;

                    case "Revert":
                        button.onClick.AddListener(RevertDisplay);
                        break;

                    case "Fullscreen Choice":
                        button.onClick.AddListener(() => {
                            fullscreen = !fullscreen;
                            RefreshChoices();
                        });
                        break;

                    case "Resolution Choice":
                        button.onClick.AddListener(() => {
                            resolutionIndex = (resolutionIndex + 1) % resolutions.Length;
                            RefreshChoices();
                        });
                        break;

                    case "Frame Limit Choice":
                        limitText = button.GetComponentInChildren<TMP_Text>();
                        button.onClick.AddListener(CycleLimit);
                        break;

                    case "Motion Choice":
                        motionText = button.GetComponentInChildren<TMP_Text>();
                        button.onClick.AddListener(ToggleMotion);
                        break;

                    case "Quality Choice":
                        button.onClick.AddListener(() => {
                            qualityIndex = (qualityIndex + 1) % QualitySettings.names.Length;
                            RefreshChoices();
                        });
                        break;
                }
            }

            // Keep the existing menu working when new audio sliders are not assigned.
            if (volume != null)
            {
                volume.onValueChanged.RemoveAllListeners();
                volume.onValueChanged.AddListener(ChangeVolume);
            }

            if (musicVolume != null)
            {
                musicVolume.onValueChanged.RemoveAllListeners();
                musicVolume.onValueChanged.AddListener(ChangeMusicVolume);
            }

            if (sfxVolume != null)
            {
                sfxVolume.onValueChanged.RemoveAllListeners();
                sfxVolume.onValueChanged.AddListener(ChangeSfxVolume);
            }

            if (sensitivity != null)
            {
                sensitivity.onValueChanged.RemoveAllListeners();
                sensitivity.onValueChanged.AddListener(v => {
                    if (sensitivityValue != null)
                        sensitivityValue.text = v.ToString("F1");
                });
            }

            if (journalPanel != null)
                journalPanel.SetActive(false);

            if (journalButtons != null)
            {
                for (int i = 0; i < journalButtons.Length; i++)
                {
                    int index = i;
                    journalButtons[i].onClick.AddListener(() => journal.OpenCollectedNote(index));
                }
            }

            settings.SetActive(false);
            confirmation.SetActive(false);
            home.SetActive(true);

            if (isPauseMenu)
                ui.SetActive(false);
            else
                Select(playButton.gameObject);
        }

        public void ShowSettings()
        {
            if (loading) return;

            AudioSettingsOpen = isPauseMenu;

            if (journalPanel != null)
                journalPanel.SetActive(false);

            if (volume != null)
            {
                volume.SetValueWithoutNotify(GamePreferences.Volume);
                volume.onValueChanged.Invoke(volume.value);
            }

            if (musicVolume != null)
            {
                musicVolume.SetValueWithoutNotify(AudioPreferences.MusicVolume);
                musicVolume.onValueChanged.Invoke(musicVolume.value);
            }

            if (sfxVolume != null)
            {
                sfxVolume.SetValueWithoutNotify(AudioPreferences.SfxVolume);
                sfxVolume.onValueChanged.Invoke(sfxVolume.value);
            }

            if (sensitivity != null)
            {
                sensitivity.SetValueWithoutNotify(GamePreferences.Sensitivity);
                sensitivity.onValueChanged.Invoke(sensitivity.value);
            }

            limitIndex = System.Array.IndexOf(limits,
                PlayerPrefs.GetInt(GamePreferences.Prefix + "FrameLimit", 90));

            if (limitIndex < 0)
                limitIndex = 1;

            reducedMotion = PlayerPrefs.GetInt(
                GamePreferences.Prefix + "ReducedMotion", 0) == 1;

            fullscreen = Screen.fullScreen;
            qualityIndex = QualitySettings.GetQualityLevel();

            var list = Screen.resolutions
                .Select(r => new Vector2Int(r.width, r.height))
                .Distinct()
                .ToList();

            var current = new Vector2Int(Screen.width, Screen.height);

            if (!list.Contains(current))
                list.Add(current);

            resolutions = list.OrderBy(r => r.x)
                .ThenBy(r => r.y)
                .ToArray();

            resolutionIndex = System.Array.IndexOf(resolutions, current);

            RefreshChoices();

            home.SetActive(false);
            settings.SetActive(true);

            if (volume != null)
                Select(volume.gameObject);
        }

        void CycleLimit()
        {
            limitIndex = (limitIndex + 1) % limits.Length;
            RefreshChoices();
        }

        void ToggleMotion()
        {
            reducedMotion = !reducedMotion;
            RefreshChoices();
        }

        void RefreshChoices()
        {
            limitText.text = limits[limitIndex] < 0
                ? "Uncapped"
                : "" + limits[limitIndex] + " FPS";

            motionText.text = reducedMotion ? "On" : "Off";

            fullscreenText.text = fullscreen ? "On  >" : "Off  >";

            resolutionText.text = resolutions[resolutionIndex].x +
                " x " + resolutions[resolutionIndex].y + "  >";

            qualityText.text = QualitySettings.names[qualityIndex] + "  >";
        }

        void ChangeVolume(float value)
        {
            if (volumeValue != null)
                volumeValue.text = Mathf.RoundToInt(value * 100) + "%";

            GamePreferences.SetVolume(value);
        }

        void ChangeMusicVolume(float value)
        {
            if (musicVolumeValue != null)
                musicVolumeValue.text = Mathf.RoundToInt(value * 100) + "%";

            AudioPreferences.SetMusicVolume(value);
        }

        void ChangeSfxVolume(float value)
        {
            if (sfxVolumeValue != null)
                sfxVolumeValue.text = Mathf.RoundToInt(value * 100) + "%";

            AudioPreferences.SetSfxVolume(value);
        }

        public void ShowJournal()
        {
            if (journal == null || journalPanel == null) return;

            AudioSettingsOpen = false;
            home.SetActive(false);
            settings.SetActive(false);
            journalPanel.SetActive(true);

            int row = 0;

            for (int i = 0; i < journalButtons.Length; i++)
            {
                bool found = journal.IsCollected(i);
                journalButtons[i].gameObject.SetActive(found);

                if (found)
                    journalButtons[i].GetComponent<RectTransform>().anchoredPosition =
                        new Vector2(140, -310 - 66 * row++);
            }

            Select(journalPanel.transform.Find("Journal Back").gameObject);
        }

        public void ShowHome()
        {
            AudioSettingsOpen = false;
            PlayerPrefs.Save();

            if (journalPanel != null)
                journalPanel.SetActive(false);

            if (confirmation.activeSelf)
                RevertDisplay();

            settings.SetActive(false);
            home.SetActive(true);
            Select(playButton.gameObject);
        }

        public void ApplySettings()
        {
            if (volume != null)
                GamePreferences.SetVolume(volume.value);

            if (musicVolume != null)
                AudioPreferences.SetMusicVolume(musicVolume.value);

            if (sfxVolume != null)
                AudioPreferences.SetSfxVolume(sfxVolume.value);

            foreach (var mouse in FindObjectsOfType<MouseLook>())
                mouse.SetSensitivity(sensitivity.value);

            GamePreferences.SetQuality(qualityIndex);

            if (volume != null)
                PlayerPrefs.SetFloat(GamePreferences.Prefix + "Volume", volume.value);

            PlayerPrefs.SetFloat("Sensitivity", sensitivity.value);
            PlayerPrefs.SetInt(GamePreferences.Prefix + "Quality", qualityIndex);
            PlayerPrefs.SetInt(GamePreferences.Prefix + "FrameLimit", limits[limitIndex]);
            PlayerPrefs.SetInt(GamePreferences.Prefix + "ReducedMotion", reducedMotion ? 1 : 0);

            GamePreferences.ApplyFrameLimit();
            PlayerPrefs.Save();

            previousWidth = Screen.width;
            previousHeight = Screen.height;
            previousFullscreen = Screen.fullScreen;

            var r = resolutions[resolutionIndex];

            if (r.x != previousWidth ||
                r.y != previousHeight ||
                fullscreen != previousFullscreen)
            {
                Screen.SetResolution(r.x, r.y, fullscreen);
                confirmationEnd = Time.unscaledTime + 15;

                confirmation.SetActive(true);
                EventSystem.current.SetSelectedGameObject(keepButton.gameObject);
            }
            else
            {
                status.text = "Settings saved.";
                ShowHome();
            }
        }

        public void KeepDisplay()
        {
            var r = resolutions[resolutionIndex];

            PlayerPrefs.SetInt(GamePreferences.Prefix + "Width", r.x);
            PlayerPrefs.SetInt(GamePreferences.Prefix + "Height", r.y);
            PlayerPrefs.SetInt(GamePreferences.Prefix + "Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.Save();

            confirmation.SetActive(false);
            status.text = "Settings saved.";
            ShowHome();
        }

        public void RevertDisplay()
        {
            Screen.SetResolution(previousWidth, previousHeight, previousFullscreen);
            confirmation.SetActive(false);

            fullscreen = previousFullscreen;

            resolutionIndex = System.Array.FindIndex(resolutions,
                r => r.x == previousWidth && r.y == previousHeight);

            if (resolutionIndex < 0)
                resolutionIndex = 0;

            RefreshChoices();
            EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }

        public void PlayNow()
        {
            if (isPauseMenu)
            {
                pauseController.PauseGame();
                return;
            }

            if (!loading)
                StartCoroutine(LoadGame());
        }

        public void SetPauseVisible(bool visible)
        {
            if (journal != null && journal.IsReading)
                journal.CloseNote();

            AudioSettingsOpen = false;
            ui.SetActive(visible);

            if (visible)
                ShowHome();
        }

        public void HandlePauseEscape()
        {
            if (journal != null && journal.IsReading)
                journal.CloseNote();
            else if (journalPanel != null && journalPanel.activeSelf)
                ShowHome();
            else if (confirmation.activeSelf)
                RevertDisplay();
            else if (settings.activeSelf)
                ShowHome();
            else
                pauseController.PauseGame();
        }

        IEnumerator LoadGame()
        {
            loading = true;

            foreach (var button in home.GetComponentsInChildren<Button>())
                button.interactable = false;

            if (openingStory != null)
            {
                openingStory.Begin(gameScene);
                yield break;
            }

            status.text = "Entering the cemetery...";
            yield return null;

            var operation = SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Single);

            while (!operation.isDone)
            {
                status.text = "Entering the cemetery... " +
                    Mathf.RoundToInt(Mathf.Clamp01(operation.progress / .9f) * 100) + "%";

                yield return null;
            }
        }

        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GameObject Panel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(ui.transform, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        static void Select(GameObject target)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static RectTransform Rect(GameObject go, Transform parent,
            float x, float y, float width, float height)
        {
            go.transform.SetParent(parent, false);

            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(width, height);

            return r;
        }

        TMP_Text Label(Transform parent, string text, float x, float y,
            float w, float h, int size, Color color, TMP_FontAsset font = null)
        {
            var go = new GameObject(
                text.Length > 30 ? text.Substring(0, 30) : text,
                typeof(RectTransform), typeof(TextMeshProUGUI));

            Rect(go, parent, x, y, w, h);

            var label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.font = font == null ? bodyFont : font;
            label.fontSize = size;
            label.color = color;
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;

            return label;
        }

        void Line(Transform parent, float x, float y, float width, Color color)
        {
            var go = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            Rect(go, parent, x, y, width, 1);

            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
        }

        Button ButtonAt(Transform parent, string text, float x, float y,
            float width, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(
                text.Length == 0 ? "Choice" : text,
                typeof(RectTransform), typeof(Image), typeof(Button));

            Rect(go, parent, x, y, width, 64);

            go.GetComponent<Image>().color = Color.white;

            var b = go.GetComponent<Button>();
            b.targetGraphic = go.GetComponent<Image>();

            var colors = b.colors;
            colors.normalColor = new Color(.094f, .137f, .122f, .5f);
            colors.highlightedColor = new Color(.42f, .29f, .15f, .85f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.52f, .36f, .19f, .9f);
            colors.fadeDuration = 0;
            b.colors = colors;

            var caption = Label(
                go.transform, text, 0, 10, width, 46,
                parent == settings.transform && text.Length == 0 ? 24 : 32,
                Ink,
                parent == settings.transform && text.Length == 0 ? valueFont : buttonFont);

            go.GetComponent<Image>().color = Color.clear;
            b.targetGraphic = caption;

            colors.normalColor = Ink;
            colors.highlightedColor = Gold;
            colors.selectedColor = Gold;
            colors.pressedColor = Muted;
            b.colors = colors;

            b.onClick.AddListener(action);
            return b;
        }

        Slider SliderAt(Transform parent, string title, float y, float min, float max)
        {
            Label(parent, title, 140, y, 350, 45, 24, Ink);

            var value = Label(parent, "", 1000, y, 150, 45, 22, Muted, valueFont);

            var go = new GameObject(title, typeof(RectTransform), typeof(Slider));
            Rect(go, parent, 560, y + 8, 400, 24);

            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;

            var bg = new GameObject("Track", typeof(RectTransform), typeof(Image));
            Rect(bg, go.transform, 0, 10, 400, 4);
            bg.GetComponent<Image>().color = Muted;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            Rect(handle, go.transform, 0, 0, 16, 24);
            handle.GetComponent<Image>().color = Gold;

            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.pivot = new Vector2(.5f, .5f);
            handleRect.sizeDelta = new Vector2(16, 0);
            handleRect.anchoredPosition = Vector2.zero;

            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;

            if (title == "Master Volume")
                volumeValue = value;
            else if (title == "Music Volume")
                musicVolumeValue = value;
            else if (title == "Sound Effects")
                sfxVolumeValue = value;
            else
                sensitivityValue = value;

            slider.onValueChanged.AddListener(v =>
                value.text = max == 1
                    ? Mathf.RoundToInt(v * 100) + "%"
                    : v.ToString("F1"));

            return slider;
        }

        void OnDestroy()
        {
            if (isPauseMenu)
                AudioSettingsOpen = false;

            if (Application.isPlaying && ui != null)
                Destroy(ui);
        }
    }
}


