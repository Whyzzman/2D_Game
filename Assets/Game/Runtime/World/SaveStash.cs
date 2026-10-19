using System.IO;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The player's save files copied aside while an automated run (the screenshot tour, the
    /// performance probe) plays a fresh game, and put back when it ends.
    /// </summary>
    public sealed class SaveStash
    {
        private readonly string _folder;

        private SaveStash(string folder)
        {
            _folder = folder;
        }

        public static SaveStash Take(string name)
        {
            var stash = new SaveStash(Path.Combine(Application.temporaryCachePath, name));
            Directory.CreateDirectory(stash._folder);
            foreach (var file in new[] { SaveSystem.FilePath, SaveSystem.BackupPath })
            {
                var target = Path.Combine(stash._folder, Path.GetFileName(file));
                if (File.Exists(file))
                {
                    File.Copy(file, target, true);
                }
                else if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }

            return stash;
        }

        public void Restore()
        {
            SaveSystem.Delete();
            foreach (var file in new[] { SaveSystem.FilePath, SaveSystem.BackupPath })
            {
                var source = Path.Combine(_folder, Path.GetFileName(file));
                if (File.Exists(source))
                {
                    File.Copy(source, file, true);
                }
            }
        }
    }
}
