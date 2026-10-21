using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WalkingSimulator.Presentation
{
    public sealed class GamePreferences : MonoBehaviour
    {
        public const string Prefix = "WalkingSimulator.";
        static GamePreferences instance;
        public static float Volume { get { return PlayerPrefs.GetFloat(Prefix + "Volume", 1f); } }
        public static void SetVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Prefix + "Volume", AudioListener.volume);
        }
        public static float Sensitivity { get { return PlayerPrefs.GetFloat("Sensitivity", 5f); } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (instance != null) return;
            instance = new GameObject("Game Preferences").AddComponent<GamePreferences>();
            DontDestroyOnLoad(instance.gameObject);
            SceneManager.sceneLoaded += instance.SceneLoaded;
            AudioListener.volume = Volume;
            ApplyFrameLimit();
            if (PlayerPrefs.HasKey(Prefix + "Quality")) SetQuality(PlayerPrefs.GetInt(Prefix + "Quality"));
            if (PlayerPrefs.HasKey(Prefix + "Width"))
                Screen.SetResolution(PlayerPrefs.GetInt(Prefix + "Width"), PlayerPrefs.GetInt(Prefix + "Height"), PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) == 1);
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode) { StartCoroutine(ApplyMouseSettings(scene)); }

        IEnumerator ApplyMouseSettings(Scene scene)
        {
            yield return null;
            if (scene.name != "The Game") yield break;
            foreach (var mouse in FindObjectsOfType<MouseLook>())
            {
                mouse.SetSensitivity(Sensitivity);
            }
        }

        public static void SetQuality(int index)
        {
            float existingLod = QualitySettings.lodBias;
            QualitySettings.SetQualityLevel(Mathf.Clamp(index, 0, QualitySettings.names.Length - 1), true);
            QualitySettings.lodBias = existingLod;
            ApplyFrameLimit();
        }

        public static void ApplyFrameLimit()
        {
            QualitySettings.vSyncCount=0;
            Application.targetFrameRate=PlayerPrefs.GetInt(Prefix+"FrameLimit",90);
        }

        void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= SceneLoaded;
            instance = null;
        }
    }
}
