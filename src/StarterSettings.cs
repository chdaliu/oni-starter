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
    }

    /// <summary>
    /// Mod 自身的设置。默认全部关闭，首次安装保持原版行为。
    /// 只从主菜单的 Mod 列表进入设置，不在游戏内增加入口。
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
