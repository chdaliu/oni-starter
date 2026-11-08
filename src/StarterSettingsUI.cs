using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KMod;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONIStarter
{
    /// <summary>
    /// Settings screen opened from the Mods list entry.
    /// The visual style follows the DGSM settings dialog: a compact purple
    /// title bar, a blue-grey panel, white rounded checkboxes with a dark blue
    /// check mark, and a bottom button row.
    /// </summary>
    public sealed class StarterSettingsScreen : KModalScreen
    {
        private const float PanelWidth = 640f;
        private const float PanelHeight = 660f;
        private const float TitleBarHeight = 34f;
        private const float RowHeight = 56f;

        private static readonly Color PanelColor = new Color(0.243f, 0.263f, 0.341f, 1f);
        private static readonly Color TitleBarColor = new Color(0.529f, 0.271f, 0.4f, 1f);
        private static readonly Color TitleBarHoverColor = new Color(0.616f, 0.329f, 0.475f, 1f);
        private static readonly Color DarkBlueColor = new Color(0.188f, 0.204f, 0.263f, 1f);
        private static readonly Color PinkTextColor = new Color(0.961f, 0.788f, 0.874f, 1f);
        private static readonly Color HintColor = new Color(0.486f, 0.51f, 0.604f, 1f);
        private static readonly Color WhiteColor = Color.white;

        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private GameObject labelTemplate;

        public void Initialize(GameObject template)
        {
            labelTemplate = template;
        }

        private sealed class SettingRow
        {
            public string Title;
            public string Description;
            public Func<bool> Getter;
            public Action<bool> Setter;
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            pause = false;
            canBackoutWithRightClick = true;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            try
            {
                DimModalBackground();
                BuildInterface();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] Failed to build the settings screen: " + exception);
            }
        }

        protected override void OnCleanUp()
        {
            StarterSettingsUI.NotifyClosed(this);
            base.OnCleanUp();
        }

        private void DimModalBackground()
        {
            Transform background = transform.Find("background");
            if (background == null)
            {
                return;
            }

            Image image = background.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0f, 0f, 0.3f);
            }
        }

        private void BuildInterface()
        {
            RectTransform panel = CreatePanel();
            CreateTitleBar(panel);
            RectTransform content = CreateContent(panel);
            CreateContentHeader(content);
            CreateSettingRows(content);
            CreateNotes(content);
            CreateBottomBar(panel);

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private RectTransform CreatePanel()
        {
            GameObject panelObject = new GameObject(
                "Panel",
                typeof(RectTransform),
                typeof(Image));

            panelObject.transform.SetParent(transform, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rect.anchoredPosition = Vector2.zero;

            Image image = panelObject.GetComponent<Image>();
            ApplySprite(image, "web_box", PanelColor);

            return rect;
        }

        private void CreateTitleBar(RectTransform panel)
        {
            GameObject barObject = new GameObject(
                "TitleBar",
                typeof(RectTransform),
                typeof(Image));

            barObject.transform.SetParent(panel, false);

            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, TitleBarHeight);

            Image barImage = barObject.GetComponent<Image>();
            ApplySprite(barImage, "web_box", TitleBarColor);
            barImage.raycastTarget = false;

            GameObject titleObject = CreateText(
                barObject,
                "Title",
                "新手套装设置",
                16f,
                TextAlignmentOptions.MidlineLeft,
                WhiteColor);

            if (titleObject != null)
            {
                RectTransform titleRect = titleObject.rectTransform();
                titleRect.anchorMin = new Vector2(0f, 0f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.offsetMin = new Vector2(14f, 0f);
                titleRect.offsetMax = new Vector2(-44f, 0f);

                LocText title = titleObject.GetComponent<LocText>();
                if (title != null)
                {
                    title.fontStyle = FontStyles.Bold;
                }
            }

            CreateCloseIconButton(barObject);
        }

        private void CreateCloseIconButton(GameObject titleBar)
        {
            GameObject holder = new GameObject(
                "Close",
                typeof(RectTransform),
                typeof(KButton));

            holder.transform.SetParent(titleBar.transform, false);

            RectTransform holderRect = holder.GetComponent<RectTransform>();
            holderRect.anchorMin = new Vector2(1f, 1f);
            holderRect.anchorMax = new Vector2(1f, 1f);
            holderRect.pivot = new Vector2(1f, 1f);
            holderRect.anchoredPosition = new Vector2(-8f, -5f);
            holderRect.sizeDelta = new Vector2(24f, 24f);

            KButton button = holder.GetComponent<KButton>();
            button.soundPlayer = new ButtonSoundPlayer { Enabled = false };
            button.additionalKImages = new KImage[0];
            button.onClick += Deactivate;

            GameObject iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(Image));

            iconObject.transform.SetParent(holder.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image icon = iconObject.GetComponent<Image>();
            ApplySprite(icon, "cancel", WhiteColor);
            icon.raycastTarget = true;

            ToolTip tooltip = holder.AddComponent<ToolTip>();
            tooltip.toolTip = "关闭";
        }

        private RectTransform CreateContent(RectTransform panel)
        {
            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));

            contentObject.transform.SetParent(panel, false);

            RectTransform rect = contentObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 60f);
            rect.offsetMax = new Vector2(-20f, -(TitleBarHeight + 16f));

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private void CreateContentHeader(RectTransform content)
        {
            GameObject headerObject = CreateText(
                content.gameObject,
                "SectionHeader",
                "功能开关",
                16f,
                TextAlignmentOptions.MidlineLeft,
                PinkTextColor);

            if (headerObject != null)
            {
                LayoutElement layoutElement = headerObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 28f;
                layoutElement.minHeight = 28f;
                layoutElement.flexibleWidth = 1f;
            }
        }

        private void CreateSettingRows(RectTransform content)
        {
            List<SettingRow> rows = new List<SettingRow>
            {
                new SettingRow
                {
                    Title = "食物保鲜",
                    Description = "所有食物和腐烂物始终保持在 100% 新鲜度。",
                    Getter = () => StarterSettings.FoodFresh,
                    Setter = value => StarterSettings.FoodFresh = value
                },
                new SettingRow
                {
                    Title = "简化复制人",
                    Description = "固定为健步如飞、健美者、潜水员之肺，并移除其他随机特质。",
                    Getter = () => StarterSettings.SimplifyDuplicants,
                    Setter = value => StarterSettings.SimplifyDuplicants = value
                },
                new SettingRow
                {
                    Title = "自动打印",
                    Description = "按打印舱侧屏“自动打印列表”的勾选自动打印补给包。",
                    Getter = () => StarterSettings.AutoPrint,
                    Setter = value => StarterSettings.AutoPrint = value
                },
                new SettingRow
                {
                    Title = "技能点满级不增加",
                    Description = "当前技能树全部点满后，停止继续增长技能点。",
                    Getter = () => StarterSettings.SkillPointCap,
                    Setter = value => StarterSettings.SkillPointCap = value
                }
            };

            for (int i = 0; i < rows.Count; i++)
            {
                CreateRow(content, rows[i]);
            }
        }

        private void CreateRow(RectTransform content, SettingRow row)
        {
            GameObject rowObject = new GameObject("Row_" + row.Title, typeof(RectTransform));
            rowObject.transform.SetParent(content, false);

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;

            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = RowHeight;
            layoutElement.minHeight = RowHeight;
            layoutElement.flexibleWidth = 1f;

            GameObject checkboxObject = CreateCheckbox(rowObject, row);
            if (checkboxObject != null)
            {
                RectTransform checkboxRect = checkboxObject.rectTransform();
                checkboxRect.anchorMin = new Vector2(0f, 0.5f);
                checkboxRect.anchorMax = new Vector2(0f, 0.5f);
                checkboxRect.pivot = new Vector2(0f, 0.5f);
                checkboxRect.anchoredPosition = new Vector2(2f, 0f);
                checkboxRect.sizeDelta = new Vector2(18f, 18f);
            }

            GameObject titleObject = CreateText(
                rowObject,
                "Title",
                row.Title,
                16f,
                TextAlignmentOptions.MidlineLeft,
                WhiteColor);

            if (titleObject != null)
            {
                RectTransform titleRect = titleObject.rectTransform();
                titleRect.anchorMin = new Vector2(0f, 0.5f);
                titleRect.anchorMax = new Vector2(0f, 0.5f);
                titleRect.pivot = new Vector2(0f, 0.5f);
                titleRect.anchoredPosition = new Vector2(32f, 14f);
                titleRect.sizeDelta = new Vector2(PanelWidth - 76f, 26f);

                ToolTip tooltip = titleObject.AddComponent<ToolTip>();
                tooltip.toolTip = row.Description;
            }

            GameObject descriptionObject = CreateText(
                rowObject,
                "Description",
                row.Description,
                12f,
                TextAlignmentOptions.MidlineLeft,
                HintColor);

            if (descriptionObject != null)
            {
                RectTransform descriptionRect = descriptionObject.rectTransform();
                descriptionRect.anchorMin = new Vector2(0f, 0.5f);
                descriptionRect.anchorMax = new Vector2(0f, 0.5f);
                descriptionRect.pivot = new Vector2(0f, 0.5f);
                descriptionRect.anchoredPosition = new Vector2(32f, -14f);
                descriptionRect.sizeDelta = new Vector2(PanelWidth - 76f, 22f);
            }
        }

        private void CreateNotes(RectTransform content)
        {
            GameObject headerObject = CreateText(
                content.gameObject,
                "NotesHeader",
                "说明",
                16f,
                TextAlignmentOptions.MidlineLeft,
                PinkTextColor);

            if (headerObject != null)
            {
                LayoutElement layoutElement = headerObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 28f;
                layoutElement.minHeight = 28f;
                layoutElement.flexibleWidth = 1f;
            }

            GameObject notesObject = CreateText(
                content.gameObject,
                "Notes",
                "简化复制人仅作用于标准复制人，仿生复制人保持原版。\n"
                + "技能点满级不增加同样只作用于标准复制人。\n"
                + "设置保存在本地配置文件中，所有存档共用。",
                12f,
                TextAlignmentOptions.TopLeft,
                HintColor);

            if (notesObject != null)
            {
                LocText notes = notesObject.GetComponent<LocText>();
                if (notes != null)
                {
                    notes.enableWordWrapping = true;
                }

                LayoutElement layoutElement = notesObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 76f;
                layoutElement.minHeight = 76f;
                layoutElement.flexibleWidth = 1f;
            }
        }

        private GameObject CreateCheckbox(GameObject parent, SettingRow row)
        {
            GameObject container = new GameObject(
                "Checkbox",
                typeof(RectTransform),
                typeof(KButton));

            container.transform.SetParent(parent.transform, false);

            GameObject backgroundObject = new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image));

            backgroundObject.transform.SetParent(container.transform, false);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = new Vector2(1f, 1f);
            backgroundRect.offsetMax = new Vector2(-1f, -1f);

            Image background = backgroundObject.GetComponent<Image>();
            ApplySprite(background, "web_rounded", WhiteColor);
            background.raycastTarget = true;

            GameObject checkObject = new GameObject(
                "CheckMark",
                typeof(RectTransform),
                typeof(Image));

            checkObject.transform.SetParent(container.transform, false);

            RectTransform checkRect = checkObject.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(4f, 5f);
            checkRect.offsetMax = new Vector2(-4f, -4f);

            Image checkImage = checkObject.GetComponent<Image>();
            ApplySprite(checkImage, "overview_jobs_icon_checkmark", DarkBlueColor);
            checkImage.raycastTarget = false;

            bool initial = row.Getter();
            checkObject.SetActive(initial);

            KButton button = container.GetComponent<KButton>();
            button.soundPlayer = new ButtonSoundPlayer { Enabled = false };
            button.additionalKImages = new KImage[0];
            button.onClick += delegate
            {
                bool value = !checkObject.activeSelf;
                checkObject.SetActive(value);
                row.Setter(value);
                StarterSettings.Save();
            };

            ToolTip tooltip = container.AddComponent<ToolTip>();
            tooltip.toolTip = row.Description;

            return container;
        }

        private void CreateBottomBar(RectTransform panel)
        {
            GameObject hintObject = CreateText(
                panel.gameObject,
                "Hint",
                "设置会自动保存",
                12f,
                TextAlignmentOptions.MidlineLeft,
                HintColor);

            if (hintObject != null)
            {
                RectTransform hintRect = hintObject.rectTransform();
                hintRect.anchorMin = new Vector2(0f, 0f);
                hintRect.anchorMax = new Vector2(0f, 0f);
                hintRect.pivot = new Vector2(0f, 0f);
                hintRect.anchoredPosition = new Vector2(20f, 12f);
                hintRect.sizeDelta = new Vector2(300f, 30f);
            }

            CreateCloseButton(panel);
        }

        private void CreateCloseButton(RectTransform panel)
        {
            GameObject buttonObject = CreateButtonObject(panel.gameObject, "关闭");
            if (buttonObject == null)
            {
                return;
            }

            RectTransform rect = buttonObject.rectTransform();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-20f, 12f);
            rect.sizeDelta = new Vector2(100f, 30f);
        }

        private GameObject CreateButtonObject(GameObject parent, string text)
        {
            GameObject buttonObject = new GameObject(
                "Button",
                typeof(RectTransform),
                typeof(KImage),
                typeof(KButton));

            buttonObject.transform.SetParent(parent.transform, false);

            KImage image = buttonObject.GetComponent<KImage>();
            ApplySprite(image, "web_button", TitleBarColor);

            ColorStyleSetting style = CreateButtonStyle(TitleBarColor, TitleBarHoverColor);
            image.colorStyleSetting = style;
            image.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.GetComponent<KButton>();
            button.bgImage = image;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer { Enabled = false };
            button.onClick += Deactivate;

            GameObject labelObject = CreateText(
                buttonObject,
                "Label",
                text,
                16f,
                TextAlignmentOptions.Center,
                WhiteColor);

            if (labelObject != null)
            {
                RectTransform labelRect = labelObject.rectTransform();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                LocText label = labelObject.GetComponent<LocText>();
                if (label != null)
                {
                    label.fontStyle = FontStyles.Bold;
                }
            }

            return buttonObject;
        }

        private static ColorStyleSetting CreateButtonStyle(Color normal, Color hover)
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.activeColor = normal;
            style.inactiveColor = normal;
            style.hoverColor = hover;
            style.disabledColor = normal;
            style.disabledActiveColor = normal;
            style.disabledhoverColor = normal;
            return style;
        }

        private GameObject CreateText(
            GameObject parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject labelObject = CreateLabel(parent, name);
            if (labelObject == null)
            {
                return null;
            }

            LocText label = labelObject.GetComponent<LocText>();
            if (label == null)
            {
                return null;
            }

            label.key = "";
            label.text = text;
            label.enableAutoSizing = false;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;

            return labelObject;
        }

        private GameObject CreateLabel(GameObject parent, string name)
        {
            GameObject prefab = labelTemplate;

            if (prefab == null)
            {
                prefab = Assets.UIPrefabs.TableScreenWidgets.Label;
            }

            if (prefab == null)
            {
                prefab = Assets.UIPrefabs.TableScreenWidgets.ButtonLabel;
            }

            if (prefab == null)
            {
                prefab = Assets.UIPrefabs.TableScreenWidgets.LabelHeader;
            }

            if (prefab == null)
            {
                return null;
            }

            LocText templateLabel = prefab.GetComponent<LocText>();
            GameObject labelObject = Util.KInstantiateUI(prefab, parent, true);
            labelObject.name = name;

            LocText label = labelObject.GetComponent<LocText>();
            if (label != null)
            {
                label.key = "";
                label.raycastTarget = false;

                if (templateLabel != null)
                {
                    if (templateLabel.font != null)
                    {
                        label.font = templateLabel.font;
                    }

                    if (templateLabel.fontSharedMaterial != null)
                    {
                        label.fontSharedMaterial = templateLabel.fontSharedMaterial;
                    }
                }
            }

            return labelObject;
        }

        private static void ApplySprite(Image image, string spriteName, Color color)
        {
            image.color = color;

            Sprite sprite = FindSprite(spriteName);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            if (sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
            }
        }

        private static Sprite FindSprite(string name)
        {
            if (SpriteCache.TryGetValue(name, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Assets.GetSprite(name);
            if (sprite == null)
            {
                foreach (Sprite candidate in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (candidate != null && candidate.name == name)
                    {
                        sprite = candidate;
                        break;
                    }
                }
            }

            SpriteCache[name] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// Adds the "Settings" button to the Mods list row and opens the custom
    /// settings screen. No additional entry points are added elsewhere.
    /// </summary>
    public static class StarterSettingsUI
    {
        private static StarterSettingsScreen currentScreen;

        public static void Open(ModsScreen modsScreen)
        {
            if (modsScreen == null)
            {
                return;
            }

            if (currentScreen != null)
            {
                return;
            }

            Transform parent = modsScreen.transform.parent;
            if (parent == null)
            {
                parent = modsScreen.transform;
            }

            GameObject screenObject = null;

            try
            {
                screenObject = new GameObject(
                    "ONIStarterSettingsScreen",
                    typeof(RectTransform));

                screenObject.transform.SetParent(parent, false);

                RectTransform rect = screenObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                currentScreen = screenObject.AddComponent<StarterSettingsScreen>();
                currentScreen.Initialize(FindLabelTemplate(modsScreen));
                currentScreen.Spawn();

                // Match the layer of the screen that opened us so the UI also
                // renders correctly when the canvas is not screen-space overlay.
                screenObject.SetLayerRecursively(parent.gameObject.layer);
            }
            catch (Exception exception)
            {
                currentScreen = null;

                if (screenObject != null)
                {
                    UnityEngine.Object.Destroy(screenObject);
                }

                Debug.LogWarning("[ONI-Starter] Failed to open the settings screen: " + exception);
            }
        }

        /// <summary>
        /// Uses a label from the already-spawned Mods screen as the template
        /// for all settings text. This keeps the game font, material and text
        /// style instead of relying on the bare table-screen label prefab.
        /// </summary>
        private static GameObject FindLabelTemplate(ModsScreen modsScreen)
        {
            LocText label = modsScreen != null
                ? modsScreen.GetComponentInChildren<LocText>(true)
                : null;

            if (label == null && ScreenPrefabs.Instance != null && ScreenPrefabs.Instance.ButtonGrid != null)
            {
                GameObject buttonPrefab = ScreenPrefabs.Instance.ButtonGrid.buttonPrefab;
                if (buttonPrefab != null)
                {
                    label = buttonPrefab.GetComponentInChildren<LocText>(true);
                }
            }

            return label != null ? label.gameObject : null;
        }

        public static void NotifyClosed(StarterSettingsScreen screen)
        {
            if (currentScreen == screen)
            {
                currentScreen = null;
            }
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

            try
            {
                AddSettingsEntry(__instance);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] Failed to add the settings entry: " + exception);
            }
        }

        private static void AddSettingsEntry(ModsScreen __instance)
        {
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
        /// Keeps the official Manage button and clones the same button style for
        /// the "Settings" entry.
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
                label.key = "";
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
