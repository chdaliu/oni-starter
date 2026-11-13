using System;
using System.IO;
using UnityEngine;

namespace ONIStarter
{
    [Serializable]
    public sealed class StarterSettingsData
    {
        public bool foodFresh;
        public bool simplifyDuplicants;
        public bool autoPrint;
        public bool skillPointCap;
        public bool printPodRefresh;
        public bool mechanicalAscension;
        public bool autoShatterMeteors;
        public bool noAnimalAttacks;
    }

    /// <summary>
    /// Mod settings. Everything is disabled by default so a fresh install
    /// behaves like vanilla. The settings screen is reachable only from the
    /// main-menu Mods list; no in-game entry points are added.
    /// </summary>
    public static class StarterSettings
    {
        private static StarterSettingsData data = new StarterSettingsData();
        private static bool loaded;

        public static bool FoodFresh
        {
            get
            {
                EnsureLoaded();
                return data.foodFresh;
            }
            set
            {
                EnsureLoaded();
                data.foodFresh = value;
            }
        }

        public static bool SimplifyDuplicants
        {
            get
            {
                EnsureLoaded();
                return data.simplifyDuplicants;
            }
            set
            {
                EnsureLoaded();
                data.simplifyDuplicants = value;
            }
        }

        public static bool AutoPrint
        {
            get
            {
                EnsureLoaded();
                return data.autoPrint;
            }
            set
            {
                EnsureLoaded();
                data.autoPrint = value;
            }
        }

        public static bool SkillPointCap
        {
            get
            {
                EnsureLoaded();
                return data.skillPointCap;
            }
            set
            {
                EnsureLoaded();
                data.skillPointCap = value;
            }
        }

        public static bool PrintPodRefresh
        {
            get
            {
                EnsureLoaded();
                return data.printPodRefresh;
            }
            set
            {
                EnsureLoaded();
                data.printPodRefresh = value;
            }
        }

        public static bool MechanicalAscension
        {
            get
            {
                EnsureLoaded();
                return data.mechanicalAscension;
            }
            set
            {
                EnsureLoaded();
                data.mechanicalAscension = value;
            }
        }

        public static bool AutoShatterMeteors
        {
            get
            {
                EnsureLoaded();
                return data.autoShatterMeteors;
            }
            set
            {
                EnsureLoaded();
                data.autoShatterMeteors = value;
            }
        }

        public static bool NoAnimalAttacks
        {
            get
            {
                EnsureLoaded();
                return data.noAnimalAttacks;
            }
            set
            {
                EnsureLoaded();
                data.noAnimalAttacks = value;
            }
        }

        public static void Load()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;

            try
            {
                if (!File.Exists(FilePath))
                {
                    data = new StarterSettingsData();
                    return;
                }

                string text = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(text))
                {
                    data = new StarterSettingsData();
                    return;
                }

                StarterSettingsData loadedData = JsonUtility.FromJson<StarterSettingsData>(text);
                data = loadedData ?? new StarterSettingsData();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 读取设置失败：" + exception);
                data = new StarterSettingsData();
            }
        }

        public static void Save()
        {
            EnsureLoaded();

            try
            {
                string directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 保存设置失败：" + exception);
            }
        }

        private static void EnsureLoaded()
        {
            if (!loaded)
            {
                Load();
            }
        }

        private static string FilePath
        {
            get
            {
                string root = Application.persistentDataPath;
                if (string.IsNullOrEmpty(root))
                {
                    root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }

                return Path.Combine(root, "ONI-Starter", "settings.json");
            }
        }
    }
}
