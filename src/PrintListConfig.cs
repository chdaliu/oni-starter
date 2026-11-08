using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONIStarter
{
    [Serializable]
    public sealed class PrintListConfig
    {
        public List<string> tags = new List<string>();
    }

    /// <summary>
    /// The print list is stored in the user directory rather than inside a
    /// save file, so every save on the same machine shares one list and the
    /// list is never lost when an older save is loaded.
    /// </summary>
    public static class PrintListStore
    {
        private static string FilePath
        {
            get
            {
                string root = Application.persistentDataPath;
                if (string.IsNullOrEmpty(root))
                {
                    root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }

                return Path.Combine(root, "ONI-Starter", "print_list.json");
            }
        }

        public static PrintListConfig Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new PrintListConfig();
                }

                string text = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new PrintListConfig();
                }

                PrintListConfig config = JsonUtility.FromJson<PrintListConfig>(text);
                if (config == null)
                {
                    return new PrintListConfig();
                }

                if (config.tags == null)
                {
                    config.tags = new List<string>();
                }

                return config;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 读取打印列表失败：" + exception);
                return new PrintListConfig();
            }
        }

        public static void Save(PrintListConfig config)
        {
            if (config == null)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(FilePath, JsonUtility.ToJson(config, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 保存打印列表失败：" + exception);
            }
        }
    }
}
