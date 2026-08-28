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
    /// 打印列表保存在用户目录，而不是存档里：
    /// 同一台机器上的所有存档共用一份勾选列表，也不会因为读旧档而丢失。
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
