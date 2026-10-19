using System;
using System.IO;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What the player chose, as it is written to disk.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        public int version = 1;
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.6f;
        [Range(0f, 1f)] public float effectsVolume = 0.9f;
        [Range(0f, 1f)] public float screenShake = 1f;
        [Range(0f, 1f)] public float flashIntensity = 1f;
        [Range(0f, 1f)] public float hitStop = 1f;
        /// <summary>Gamepad rumble; 0 turns it off (2026-10-19, appended so old files load with it on).</summary>
        [Range(0f, 1f)] public float vibration = 1f;
        public bool showDamageNumbers = true;
        /// <summary>Bloom and vignette; the zones' colour grade stays either way.</summary>
        public bool postEffects = true;

        /// <summary>The player's own keys: the Input System's override JSON (<see cref="KeyBindings"/>).</summary>
        public string bindingOverrides = string.Empty;
    }

    /// <summary>
    /// The player's settings, stored in their own file.
    ///
    /// Deliberately separate from the save: settings are about the person, progress is about the
    /// character. Starting a new game wipes a playthrough and must not cost someone the comfort
    /// options they needed to play at all — which is exactly what happens when the two share a
    /// file. <see cref="SaveSystem.Delete"/> never touches this one, and a test holds that line.
    ///
    /// Volume values are stored now and read by audio in a later phase; nothing plays sound yet.
    /// </summary>
    public static class GameSettings
    {
        private const string FileName = "forgotten-kingdom.settings.json";

        private static SettingsData _data;
        private static bool _isLoaded;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Raised whenever a value changes, so open screens can follow along.</summary>
        public static event Action Changed;

        public static SettingsData Current
        {
            get
            {
                if (!_isLoaded)
                {
                    Load();
                }

                return _data;
            }
        }

        /// <summary>
        /// Reads the file, or falls back to defaults. A corrupt settings file is never fatal:
        /// losing a volume slider must not stop the game from starting.
        /// </summary>
        public static void Load()
        {
            _isLoaded = true;
            _data = ReadOrDefault();
            Apply();
        }

        private static SettingsData ReadOrDefault()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var parsed = JsonUtility.FromJson<SettingsData>(File.ReadAllText(FilePath));
                    if (parsed != null)
                    {
                        return Clamped(parsed);
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Settings could not be read ({error.Message}); using defaults.");
            }

            return new SettingsData();
        }

        public static bool Save()
        {
            if (!_isLoaded)
            {
                Load();
            }

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(Clamped(_data), true));
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Settings could not be written: {error.Message}");
                return false;
            }
        }

        /// <summary>Pushes the stored values into the systems that use them.</summary>
        public static void Apply()
        {
            var data = Current;
            FeedbackSettings.ScreenShakeMultiplier = data.screenShake;
            FeedbackSettings.FlashIntensity = data.flashIntensity;
            FeedbackSettings.HitStopMultiplier = data.hitStop;
            FeedbackSettings.HapticsMultiplier = data.vibration;
            Changed?.Invoke();
        }

        /// <summary>Changes one value, applies it and writes it. What a slider calls.</summary>
        public static void Set(Action<SettingsData> change)
        {
            if (change == null)
            {
                return;
            }

            change(Current);
            _data = Clamped(_data);
            Apply();
            Save();
        }

        /// <summary>Back to defaults, written straight away.</summary>
        public static void ResetToDefaults()
        {
            _isLoaded = true;
            _data = new SettingsData();
            Apply();
            Save();
        }

        /// <summary>Forgets what is in memory; the next read comes from disk. For tests.</summary>
        public static void Unload()
        {
            _isLoaded = false;
            _data = null;
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Settings could not be deleted: {error.Message}");
            }

            Unload();
        }

        private static SettingsData Clamped(SettingsData data)
        {
            data.masterVolume = Mathf.Clamp01(data.masterVolume);
            data.musicVolume = Mathf.Clamp01(data.musicVolume);
            data.effectsVolume = Mathf.Clamp01(data.effectsVolume);
            data.screenShake = Mathf.Clamp01(data.screenShake);
            data.flashIntensity = Mathf.Clamp01(data.flashIntensity);
            data.vibration = Mathf.Clamp01(data.vibration);
            data.hitStop = Mathf.Clamp01(data.hitStop);
            return data;
        }
    }
}
