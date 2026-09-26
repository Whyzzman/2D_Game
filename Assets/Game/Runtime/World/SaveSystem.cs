using System;
using System.IO;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The only place that touches the save file. Gameplay never opens a stream: it changes
    /// <see cref="GameProgress"/>, which asks this to write. Writes go through a temporary file so
    /// a crash mid-write cannot destroy a good save, and the previous file is kept as a backup.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "forgotten-kingdom.save.json";
        private const string BackupName = "forgotten-kingdom.save.bak";
        private const string TempName = "forgotten-kingdom.save.tmp";

        /// <summary>Raised around every write so a future HUD can show a saving indicator.</summary>
        public static event Action SaveStarted;
        public static event Action SaveCompleted;
        public static event Action<string> SaveFailed;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        public static string BackupPath => Path.Combine(Application.persistentDataPath, BackupName);
        private static string TempPath => Path.Combine(Application.persistentDataPath, TempName);

        public static bool Exists => File.Exists(FilePath);

        /// <summary>True when there is a file that actually parses; what a Continue button needs.</summary>
        public static bool HasValidSave()
        {
            return TryParse(FilePath, out _) || TryParse(BackupPath, out _);
        }

        public static bool Write(GameProgressData data)
        {
            if (data == null)
            {
                return false;
            }

            SaveStarted?.Invoke();
            try
            {
                data.version = GameProgressData.CurrentVersion;
                data.lastSavedAtUtc = DateTime.UtcNow.ToString("o");
                var json = JsonUtility.ToJson(data, true);

                // Write aside, prove it parses, then swap it in. The old file becomes the backup.
                File.WriteAllText(TempPath, json);
                if (!TryParse(TempPath, out _))
                {
                    throw new IOException("The file written did not read back as valid save data.");
                }

                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, BackupPath, true);
                }

                File.Copy(TempPath, FilePath, true);
                File.Delete(TempPath);
                SaveCompleted?.Invoke();
                return true;
            }
            catch (Exception exception) when (exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                Debug.LogError($"Could not write the save file: {exception.Message}");
                SaveFailed?.Invoke(exception.Message);
                return false;
            }
        }

        /// <summary>
        /// Reads the save, falling back to the backup when the main file is missing or damaged, and
        /// to a fresh game when neither can be read. It never overwrites a damaged file on its own.
        /// </summary>
        public static GameProgressData Read()
        {
            if (TryParse(FilePath, out var data))
            {
                return data;
            }

            if (File.Exists(FilePath))
            {
                Debug.LogWarning("The save file could not be read; trying the backup.");
            }

            if (TryParse(BackupPath, out var backup))
            {
                Debug.LogWarning("Recovered progress from the backup save.");
                return backup;
            }

            return GameProgressData.CreateNew();
        }

        /// <summary>
        /// Bumped by every <see cref="Delete"/>. A save scheduled for later remembers the value it
        /// was scheduled under and is dropped if a delete came in between — a new game must not
        /// be undone by the old session's debounced write landing a moment afterwards.
        /// </summary>
        public static int Generation { get; private set; }

        public static void Delete()
        {
            Generation++;
            TryDelete(FilePath);
            TryDelete(BackupPath);
            TryDelete(TempPath);
        }

        private static bool TryParse(string path, out GameProgressData data)
        {
            data = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                var parsed = JsonUtility.FromJson<GameProgressData>(File.ReadAllText(path));
                if (parsed == null)
                {
                    return false;
                }

                parsed.Migrate();
                data = parsed;
                return true;
            }
            catch (Exception exception) when (exception is IOException
                || exception is ArgumentException)
            {
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception) when (exception is IOException
                || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not delete '{path}': {exception.Message}");
            }
        }
    }
}
