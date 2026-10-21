using UnityEngine;

namespace WalkingSimulator.Presentation
{
    public static class AudioPreferences
    {
        public const string MusicKey = GamePreferences.Prefix + "MusicVolume";
        public const string SfxKey = GamePreferences.Prefix + "SfxVolume";

        public static float MusicVolume { get { return PlayerPrefs.GetFloat(MusicKey, 1f); } }
        public static float SfxVolume { get { return PlayerPrefs.GetFloat(SfxKey, 1f); } }

        public static void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        }

        public static void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static float ApplySfx(float volume)
        {
            return volume * SfxVolume;
        }
    }
}
