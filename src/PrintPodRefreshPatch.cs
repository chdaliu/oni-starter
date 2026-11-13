using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONIStarter
{
    /// <summary>
    /// Adds a per-card refresh button to the printing pod's "Choose Blueprint"
    /// screen. The vanilla containers already have a reshuffle button, but the
    /// printing pod hides it. While the setting is enabled we move that button
    /// into the card title bar and re-enable it, so a single duplicant or care
    /// package can be rerolled without rerolling the whole selection.
    /// </summary>
    public static class PrintPodRefreshButtons
    {
        private const string RefreshLabel = "刷新";
        private const string RefreshTooltip = "刷新这个打印选项";

        private static readonly FieldInfo ContainersField = typeof(CharacterSelectionController).GetField(
            "containers",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CharacterButtonField = typeof(CharacterContainer).GetField(
            "reshuffleButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CharacterTitleBarField = typeof(CharacterContainer).GetField(
            "titleBar",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CarePackageButtonField = typeof(CarePackageContainer).GetField(
            "reshuffleButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CarePackageTitleBarField = typeof(CarePackageContainer).GetField(
            "titleBar",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo CarePackageReshuffleMethod = typeof(CarePackageContainer).GetMethod(
            "Reshuffle",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CharacterControllerField = typeof(CharacterContainer).GetField(
            "controller",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CarePackageControllerField = typeof(CarePackageContainer).GetField(
            "controller",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CharacterSelectButtonField = typeof(CharacterContainer).GetField(
            "selectButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CarePackageSelectButtonField = typeof(CarePackageContainer).GetField(
            "selectButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo SelectedDeliverablesField = typeof(CharacterSelectionController).GetField(
            "selectedDeliverables",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo SelectableCountField = typeof(CharacterSelectionController).GetField(
            "selectableCount",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool failureLogged;

        public static void Apply(ImmigrantScreen screen)
        {
            if (screen == null || !StarterSettings.PrintPodRefresh || ContainersField == null)
            {
                return;
            }

            try
            {
                List<ITelepadDeliverableContainer> containers =
                    ContainersField.GetValue(screen) as List<ITelepadDeliverableContainer>;
                if (containers == null)
                {
                    return;
                }

                foreach (ITelepadDeliverableContainer container in containers)
                {
                    CharacterContainer character = container as CharacterContainer;
                    if (character != null)
                    {
                        ApplyCharacter(character);
                        continue;
                    }

                    CarePackageContainer carePackage = container as CarePackageContainer;
                    if (carePackage != null)
                    {
                        ApplyCarePackage(carePackage);
                    }
                }
            }
            catch (Exception exception)
            {
                if (!failureLogged)
                {
                    failureLogged = true;
                    Debug.LogWarning("[ONI-Starter] 添加打印舱刷新按钮失败：" + exception);
                }
            }
        }

        private static void ApplyCharacter(CharacterContainer container)
        {
            KButton button = CharacterButtonField?.GetValue(container) as KButton;
            Image titleBar = CharacterTitleBarField?.GetValue(container) as Image;
            ApplyButton(button, titleBar, delegate { RefreshCharacter(container); });
        }

        private static void ApplyCarePackage(CarePackageContainer container)
        {
            KButton button = CarePackageButtonField?.GetValue(container) as KButton;
            Image titleBar = CarePackageTitleBarField?.GetValue(container) as Image;
            ApplyButton(button, titleBar, delegate { RefreshCarePackage(container); });
        }

        private static void RefreshCharacter(CharacterContainer container)
        {
            try
            {
                CharacterSelectionController controller =
                    CharacterControllerField?.GetValue(container) as CharacterSelectionController;

                container.Reshuffle(false);

                KToggle selectButton = CharacterSelectButtonField?.GetValue(container) as KToggle;
                RestoreSelectionLimit(controller, selectButton, delegate { container.SelectDeliverable(); });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 刷新复制人失败：" + exception);
            }
        }

        private static void RefreshCarePackage(CarePackageContainer container)
        {
            try
            {
                CharacterSelectionController controller =
                    CarePackageControllerField?.GetValue(container) as CharacterSelectionController;

                // CarePackageContainer.Reshuffle only checks IsSelected(info), but the
                // selection list stores CarePackageInstanceData objects, so a selected
                // package would stay in the list after being rerolled. Deselect with the
                // actual deliverable first.
                if (controller != null &&
                    container.carePackageInstanceData != null &&
                    controller.IsSelected(container.carePackageInstanceData))
                {
                    container.DeselectDeliverable();
                }

                ReshuffleCarePackage(container);

                KToggle selectButton = CarePackageSelectButtonField?.GetValue(container) as KToggle;
                RestoreSelectionLimit(controller, selectButton, delegate { container.SelectDeliverable(); });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 刷新补给包失败：" + exception);
            }
        }

        private static void ReshuffleCarePackage(CarePackageContainer container)
        {
            if (container == null || CarePackageReshuffleMethod == null)
            {
                return;
            }

            try
            {
                CarePackageReshuffleMethod.Invoke(container, new object[] { false });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 刷新补给包失败：" + exception);
            }
        }

        /// <summary>
        /// GenerateCharacter resets the select button to plain SelectDeliverable,
        /// ignoring the current selection limit. Re-apply the
        /// vanilla click behaviour so clicking a refreshed card while another one
        /// is already selected cannot call AddDeliverable past the limit (which
        /// logs the red "Tried to add minions beyond the allowed limit" error).
        /// </summary>
        private static void RestoreSelectionLimit(
            CharacterSelectionController controller,
            KToggle selectButton,
            System.Action select)
        {
            if (controller == null || selectButton == null ||
                SelectedDeliverablesField == null || SelectableCountField == null)
            {
                return;
            }

            IList selected = SelectedDeliverablesField.GetValue(controller) as IList;
            if (selected == null)
            {
                return;
            }

            int limit = (int)SelectableCountField.GetValue(controller);
            selectButton.ClearOnClick();

            if (selected.Count < limit)
            {
                selectButton.onClick += select;
                return;
            }

            if (controller.AllowsReplacing)
            {
                selectButton.onClick += delegate
                {
                    controller.RemoveLast();
                    select();
                };
            }
            else
            {
                selectButton.onClick += delegate
                {
                    KMonoBehaviour.PlaySound(GlobalAssets.GetSound("Negative"));
                };
            }
        }

        internal static IList GetSelectedDeliverables(CharacterSelectionController controller)
        {
            if (controller == null || SelectedDeliverablesField == null)
            {
                return null;
            }

            return SelectedDeliverablesField.GetValue(controller) as IList;
        }

        internal static int GetSelectableCount(CharacterSelectionController controller)
        {
            if (controller == null || SelectableCountField == null)
            {
                return 0;
            }

            object value = SelectableCountField.GetValue(controller);
            return value is int count ? count : 0;
        }

        /// <summary>
        /// Vanilla GenerateCharacter resets the select button to a plain
        /// SelectDeliverable handler. Re-apply the limit-aware handler after every
        /// generation (including the delayed first generation and any vanilla
        /// reshuffle) so a stale selection can never be exceeded.
        /// </summary>
        internal static void RestoreAfterCharacterGeneration(CharacterContainer container)
        {
            if (container == null || !IsPodScreenActive())
            {
                return;
            }

            try
            {
                CharacterSelectionController controller =
                    CharacterControllerField?.GetValue(container) as CharacterSelectionController;
                KToggle selectButton = CharacterSelectButtonField?.GetValue(container) as KToggle;
                RestoreSelectionLimit(controller, selectButton, delegate { container.SelectDeliverable(); });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 恢复复制人选择状态失败：" + exception);
            }
        }

        internal static void RestoreAfterCarePackageGeneration(CarePackageContainer container)
        {
            if (container == null || !IsPodScreenActive())
            {
                return;
            }

            try
            {
                CharacterSelectionController controller =
                    CarePackageControllerField?.GetValue(container) as CharacterSelectionController;
                KToggle selectButton = CarePackageSelectButtonField?.GetValue(container) as KToggle;
                RestoreSelectionLimit(controller, selectButton, delegate { container.SelectDeliverable(); });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 恢复补给包选择状态失败：" + exception);
            }
        }

        internal static bool IsPodScreenActive()
        {
            return StarterSettings.PrintPodRefresh &&
                ImmigrantScreen.instance != null &&
                ImmigrantScreen.instance.gameObject.activeInHierarchy;
        }

        private static void ApplyButton(KButton button, Image titleBar, System.Action onClick)
        {
            if (button == null || titleBar == null)
            {
                return;
            }

            // The titleBar field points at the background image. Put the button
            // next to it under the actual title bar root so it is not clipped
            // by the background and can share the title's layout area.
            Transform titleBarRoot = titleBar.transform.parent != null
                ? titleBar.transform.parent
                : titleBar.transform;

            GameObject buttonObject = button.gameObject;
            bool firstApply = buttonObject.transform.parent != titleBarRoot;

            buttonObject.SetActive(true);
            button.isInteractable = true;

            // Make sure the click lands on this button instead of falling through
            // to the card's selection toggle underneath it.
            foreach (Graphic graphic in buttonObject.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = true;
            }

            if (firstApply)
            {
                buttonObject.transform.SetParent(titleBarRoot, false);
                buttonObject.transform.SetAsLastSibling();

                ConfigureButtonRect(buttonObject.transform as RectTransform);
                ConfigureButtonContents(buttonObject);
                ShrinkTitleLabel(titleBarRoot);

                ToolTip tooltip = buttonObject.GetComponent<ToolTip>();
                if (tooltip == null)
                {
                    tooltip = buttonObject.AddComponent<ToolTip>();
                }

                tooltip.toolTip = RefreshTooltip;
            }

            // The duplicant button rerolls with is_starter: true by default,
            // which is only correct for the new-colony screen. Replace the
            // handler so the printing pod rerolls with its normal rules.
            button.ClearOnClick();
            button.onClick += onClick;
        }

        private static void ConfigureButtonRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-6f, 0f);
            rect.sizeDelta = new Vector2(78f, 28f);
        }

        private static void ConfigureButtonContents(GameObject buttonObject)
        {
            Transform iconTransform = buttonObject.transform.Find("FG");
            bool hasIcon = iconTransform != null &&
                iconTransform.GetComponent<Image>() != null &&
                iconTransform.GetComponent<Image>().sprite != null;

            if (hasIcon)
            {
                iconTransform.gameObject.SetActive(true);
                RectTransform iconRect = iconTransform as RectTransform;
                if (iconRect != null)
                {
                    iconRect.anchorMin = new Vector2(0f, 0.5f);
                    iconRect.anchorMax = new Vector2(0f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(15f, 0f);
                    iconRect.sizeDelta = new Vector2(18f, 18f);
                }
            }
            else if (iconTransform != null)
            {
                iconTransform.gameObject.SetActive(false);
            }

            Transform textTransform = buttonObject.transform.Find("Text");
            if (textTransform == null)
            {
                return;
            }

            RectTransform textRect = textTransform as RectTransform;
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(hasIcon ? 28f : 4f, 0f);
                textRect.offsetMax = new Vector2(-4f, 0f);
            }

            LocText label = textTransform.GetComponent<LocText>();
            if (label != null)
            {
                label.SetText(RefreshLabel);
                label.fontSize = 14f;
                label.enableAutoSizing = false;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void ShrinkTitleLabel(Transform titleBar)
        {
            Transform labelGroup = titleBar.Find("LabelGroup");
            RectTransform labelRect = labelGroup as RectTransform;
            if (labelRect == null || labelRect.sizeDelta.x <= 220f)
            {
                return;
            }

            float width = labelRect.sizeDelta.x - 86f;
            labelRect.sizeDelta = new Vector2(width, labelRect.sizeDelta.y);

            Transform inputTransform = labelGroup.Find("LocTextInputField");
            RectTransform inputRect = inputTransform as RectTransform;
            if (inputRect != null && inputRect.sizeDelta.x > width)
            {
                inputRect.sizeDelta = new Vector2(width, inputRect.sizeDelta.y);
            }
        }
    }

    [HarmonyPatch(typeof(ImmigrantScreen), nameof(ImmigrantScreen.InitializeImmigrantScreen))]
    public static class PrintPodRefreshInitializePatch
    {
        public static void Postfix()
        {
            PrintPodRefreshButtons.Apply(ImmigrantScreen.instance);
        }
    }

    [HarmonyPatch(typeof(CharacterContainer), nameof(CharacterContainer.GenerateCharacter))]
    public static class CharacterContainerGeneratePatch
    {
        public static void Postfix(CharacterContainer __instance)
        {
            PrintPodRefreshButtons.RestoreAfterCharacterGeneration(__instance);
        }
    }

    [HarmonyPatch(typeof(CarePackageContainer), "GenerateCharacter")]
    public static class CarePackageContainerGeneratePatch
    {
        public static void Postfix(CarePackageContainer __instance)
        {
            PrintPodRefreshButtons.RestoreAfterCarePackageGeneration(__instance);
        }
    }

    /// <summary>
    /// Safety net for the printing pod screen: if something still tries to select
    /// a deliverable after the limit is reached, replace the previous choice
    /// instead of letting the vanilla method log the red
    /// "Tried to add minions beyond the allowed limit" error.
    /// </summary>
    [HarmonyPatch(typeof(CharacterSelectionController), nameof(CharacterSelectionController.AddDeliverable))]
    public static class PrintPodAddDeliverableGuardPatch
    {
        public static bool Prefix(CharacterSelectionController __instance)
        {
            if (__instance == null ||
                ImmigrantScreen.instance == null ||
                !ImmigrantScreen.instance.gameObject.activeInHierarchy)
            {
                return true;
            }

            IList selected = PrintPodRefreshButtons.GetSelectedDeliverables(__instance);
            int limit = PrintPodRefreshButtons.GetSelectableCount(__instance);
            if (selected == null || limit <= 0 || selected.Count < limit)
            {
                return true;
            }

            Debug.LogWarning(
                "[ONI-Starter] 打印舱选择已满，改为替换上一个选项。\n" +
                Environment.StackTrace);
            __instance.RemoveLast();
            return true;
        }
    }
}
