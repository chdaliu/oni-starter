using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KMod;
using UnityEngine;
using UnityEngine.Events;

namespace ONIStarter
{
    public sealed class StarterSettingsScreenMarker : KMonoBehaviour
    {
    }

    /// <summary>
    /// 主菜单 Mod 列表中的“设置”按钮入口，以及独立的 Mod 设置窗口。
    /// 不向游戏内暂停菜单、选项菜单或打印舱添加入口。
    /// </summary>
    public static class StarterSettingsUI
    {
        private static readonly FieldInfo OptionsTitleField = typeof(OptionsMenuScreen).GetField(
            "title",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static GameObject currentScreen;

        public static void Open(ModsScreen modsScreen)
        {
            if (modsScreen == null)
            {
                return;
            }

            if (currentScreen != null)
            {
                currentScreen.SetActive(true);
                return;
            }

            if (ScreenPrefabs.Instance == null || ScreenPrefabs.Instance.OptionsScreen == null)
            {
                Debug.LogWarning("[ONI-Starter] 找不到原版选项界面模板，无法打开设置。");
                return;
            }

            Transform parent = modsScreen.transform.parent;
            if (parent == null)
            {
                parent = modsScreen.transform;
            }

            GameObject clone = UnityEngine.Object.Instantiate(
                ScreenPrefabs.Instance.OptionsScreen.gameObject,
                parent,
                false);
            clone.name = "ONIStarterSettingsScreen";
            clone.AddComponent<StarterSettingsScreenMarker>();
            currentScreen = clone;
            clone.SetActive(true);
        }

        public static void ClearCurrentScreen()
        {
            currentScreen = null;
        }

        public static void BuildSettingsButtons(OptionsMenuScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            LocText title = OptionsTitleField == null
                ? null
                : OptionsTitleField.GetValue(screen) as LocText;
            if (title != null)
            {
                title.SetText("新手套装设置");
            }

            List<KButtonMenu.ButtonInfo> buttons = new List<KButtonMenu.ButtonInfo>
            {
                CreateToggle("食物保鲜", () => StarterSettings.FoodFresh, value => StarterSettings.FoodFresh = value, screen),
                CreateToggle("简化复制人", () => StarterSettings.SimplifyDuplicants, value => StarterSettings.SimplifyDuplicants = value, screen),
                CreateToggle("自动打印", () => StarterSettings.AutoPrint, value => StarterSettings.AutoPrint = value, screen),
                CreateToggle("技能点满级不增加", () => StarterSettings.SkillPointCap, value => StarterSettings.SkillPointCap = value, screen)
            };

            screen.SetButtons(buttons);
            screen.RefreshButtons();
        }

        private static KButtonMenu.ButtonInfo CreateToggle(
            string label,
            Func<bool> getter,
            Action<bool> setter,
            OptionsMenuScreen screen)
        {
            KButtonMenu.ButtonInfo info = null;
            info = new KButtonMenu.ButtonInfo(
                FormatToggle(label, getter()),
                Action.NumActions,
                (UnityAction)delegate
                {
                    bool value = !getter();
                    setter(value);
                    StarterSettings.Save();
                    info.text = FormatToggle(label, value);
                    screen.RefreshButtons();
                });

            return info;
        }

        private static string FormatToggle(string label, bool value)
        {
            return label + "：" + (value ? "开启" : "关闭");
        }
    }

    [HarmonyPatch(typeof(OptionsMenuScreen), "OnSpawn")]
    public static class StarterSettingsScreenPatch
    {
        public static void Postfix(OptionsMenuScreen __instance)
        {
            if (__instance == null || __instance.GetComponent<StarterSettingsScreenMarker>() == null)
            {
                return;
            }

            StarterSettingsUI.BuildSettingsButtons(__instance);
        }
    }

    [HarmonyPatch(typeof(ModsScreen), "BuildDisplay")]
    public static class ModsScreenSettingsEntryPatch
    {
        private static readonly FieldInfo EntryParentField = typeof(ModsScreen).GetField(
            "entryParent",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Postfix(ModsScreen __instance)
        {
            if (__instance == null || Global.Instance == null || Global.Instance.modManager == null)
            {
                return;
            }

            Mod ourMod = null;
            foreach (Mod mod in Global.Instance.modManager.mods)
            {
                if (mod != null && (mod.staticID == "ONIStarter" || mod.title == "新手套装"))
                {
                    ourMod = mod;
                    break;
                }
            }

            if (ourMod == null)
            {
                return;
            }

            Transform parent = EntryParentField == null
                ? null
                : EntryParentField.GetValue(__instance) as Transform;
            if (parent == null)
            {
                return;
            }

            foreach (Transform child in parent)
            {
                if (child == null || child.name != ourMod.title)
                {
                    continue;
                }

                HierarchyReferences references = child.GetComponent<HierarchyReferences>();
                KButton manageButton = references == null
                    ? null
                    : references.GetReference<KButton>("ManageButton");
                if (manageButton == null)
                {
                    continue;
                }

                KButton settingsButton = EnsureSettingsButton(child, manageButton, __instance);
                if (settingsButton != null)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 保留官方 Manage 按钮，另外克隆一个同款按钮作为“设置”入口。
        /// </summary>
        private static KButton EnsureSettingsButton(
            Transform row,
            KButton manageButton,
            ModsScreen modsScreen)
        {
            if (row == null || manageButton == null)
            {
                return null;
            }

            Transform buttonParent = manageButton.transform.parent;
            if (buttonParent == null)
            {
                return null;
            }

            const string SettingsButtonName = "ONIStarterSettingsButton";
            Transform existing = buttonParent.Find(SettingsButtonName);
            KButton settingsButton;

            if (existing != null)
            {
                settingsButton = existing.GetComponent<KButton>();
            }
            else
            {
                GameObject clone = UnityEngine.Object.Instantiate(
                    manageButton.gameObject,
                    buttonParent,
                    false);
                clone.name = SettingsButtonName;
                clone.SetActive(true);
                settingsButton = clone.GetComponent<KButton>();

                int manageIndex = manageButton.transform.GetSiblingIndex();
                clone.transform.SetSiblingIndex(manageIndex);

                RectTransform sourceRect = manageButton.GetComponent<RectTransform>();
                RectTransform cloneRect = clone.GetComponent<RectTransform>();
                if (sourceRect != null && cloneRect != null)
                {
                    cloneRect.anchorMin = sourceRect.anchorMin;
                    cloneRect.anchorMax = sourceRect.anchorMax;
                    cloneRect.pivot = sourceRect.pivot;
                    cloneRect.sizeDelta = sourceRect.sizeDelta;
                    cloneRect.anchoredPosition = sourceRect.anchoredPosition +
                        new Vector2(-(sourceRect.rect.width + 8f), 0f);
                }
            }

            if (settingsButton == null)
            {
                return null;
            }

            LocText label = settingsButton.GetComponentInChildren<LocText>();
            if (label != null)
            {
                label.text = "设置";
            }

            settingsButton.ClearOnClick();
            settingsButton.onClick += delegate
            {
                StarterSettingsUI.Open(modsScreen);
            };
            settingsButton.isInteractable = true;

            ToolTip tooltip = settingsButton.GetComponent<ToolTip>();
            if (tooltip != null)
            {
                tooltip.toolTip = "新手套装设置";
            }

            return settingsButton;
        }
    }
}
