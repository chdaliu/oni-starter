using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ONIStarter
{
    [HarmonyPatch(typeof(Telepad), "OnSpawn")]
    public static class TelepadAutoPrintSetupPatch
    {
        public static void Postfix(Telepad __instance)
        {
            AutoPrintSetup.Setup(__instance);
        }
    }

    public static class AutoPrintSetup
    {
        public static readonly Tag PrintStorageId = TagManager.Create("ONI_STARTER_PRINT_STORAGE");
        private static readonly FieldInfo TargetField = typeof(TreeFilterableSideScreen).GetField(
            "target",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Setup(Telepad telepad)
        {
            if (telepad == null)
            {
                return;
            }

            try
            {
                SetupInternal(telepad);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] Failed to set up auto-print: " + exception);
            }
        }

        private static void SetupInternal(Telepad telepad)
        {
            // Reuse only our own storage. Never overwrite an unrelated Storage
            // that another component or mod may have added to the telepad.
            Storage storage = null;
            foreach (Storage existing in telepad.GetComponents<Storage>())
            {
                if (existing != null && existing.storageID == PrintStorageId)
                {
                    storage = existing;
                    break;
                }
            }

            if (storage == null)
            {
                storage = telepad.gameObject.AddComponent<Storage>();
            }

            storage.storageID = PrintStorageId;
            storage.storageFilters = new List<Tag>
            {
                AutoPrintController.CarePackageCategory
            };
            storage.capacityKg = 0f;
            storage.showInUI = false;
            storage.allowItemRemoval = false;
            storage.showSideScreenTitleBar = false;
            storage.showCapacityStatusItem = false;
            storage.showDescriptor = false;
            storage.allowClearable = false;
            storage.allowSettingOnlyFetchMarkedItems = false;
            storage.useGunForDelivery = false;
            storage.doDiseaseTransfer = false;
            storage.storeDropsFromButcherables = false;
            storage.dropOnLoad = false;

            TreeFilterable filter = telepad.GetComponent<TreeFilterable>();
            if (filter == null)
            {
                filter = telepad.gameObject.AddComponent<TreeFilterable>();
            }

            filter.storageToFilterTag = PrintStorageId;
            filter.preventAutoAddOnDiscovery = true;
            filter.copySettingsEnabled = false;
            filter.filterByStorageCategoriesOnSpawn = false;
            filter.autoSelectStoredOnLoad = false;
            filter.dropIncorrectOnFilterChange = false;
            filter.tintOnNoFiltersSet = false;
            filter.filterAllStoragesOnBuilding = false;
            filter.showUserMenu = true;
            filter.uiHeight = TreeFilterable.UISideScreenHeight.Tall;
            filter.allResourceFilterLabelString = "勾选后自动打印";

            // This Storage is only a data source for the list; it does not
            // participate in actual hauling or storage.
            storage.enabled = false;

            AutoPrintController controller = telepad.GetComponent<AutoPrintController>();
            if (controller == null)
            {
                controller = telepad.gameObject.AddComponent<AutoPrintController>();
            }

            controller.Initialize(telepad, storage, filter);

            // Prepare the care-package list before the side screen opens.
            // If this fails, the controller retries from Update.
            AutoPrintController.RegisterPrintableTags();
        }

        public static bool IsAutoPrintTarget(GameObject target)
        {
            return target != null && target.GetComponent<AutoPrintController>() != null;
        }

        public static GameObject GetSideScreenTarget(TreeFilterableSideScreen screen)
        {
            if (screen == null || TargetField == null)
            {
                return null;
            }

            return TargetField.GetValue(screen) as GameObject;
        }
    }

    /// <summary>
    /// Keeps the auto-print list out of the right-hand side screen and shows it
    /// in DetailsScreen's secondary side screen, which renders to the left of
    /// the main panel. The prefab is reused from the game's side-screen list,
    /// so the existing search box and checkboxes keep working unchanged.
    /// </summary>
    public static class AutoPrintSecondaryScreenPanel
    {
        private const string PanelTitle = "自动打印列表";

        private static readonly FieldInfo SideScreensField = typeof(DetailsScreen).GetField(
            "sideScreens",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ActiveSecondaryField = typeof(DetailsScreen).GetField(
            "activeSideScreen2",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static TreeFilterableSideScreen activeScreen;
        private static bool failureLogged;

        public static bool CanDetach()
        {
            return FindPrefab(DetailsScreen.Instance) != null;
        }

        public static void Refresh(DetailsScreen screen, GameObject target)
        {
            if (screen == null)
            {
                return;
            }

            if (!AutoPrintSetup.IsAutoPrintTarget(target))
            {
                ClearIfOurs(screen);
                return;
            }

            TreeFilterableSideScreen prefab = FindPrefab(screen);
            if (prefab == null)
            {
                // Keep the right-hand placement as a fallback.
                ClearIfOurs(screen);
                return;
            }

            KScreen secondary = screen.SetSecondarySideScreen(prefab, PanelTitle);
            TreeFilterableSideScreen list = secondary as TreeFilterableSideScreen;
            if (list == null)
            {
                return;
            }

            activeScreen = list;
            list.SetTarget(target);
        }

        public static void ClearIfOurs(DetailsScreen screen)
        {
            if (screen == null)
            {
                activeScreen = null;
                return;
            }

            bool isOurs;
            if (ActiveSecondaryField != null)
            {
                KScreen current = ActiveSecondaryField.GetValue(screen) as KScreen;
                isOurs = activeScreen != null && current == activeScreen;
            }
            else
            {
                // If the field cannot be found, fall back to checking whether
                // the screen we created is still active. Another feature that
                // took over the panel would have deactivated it first.
                isOurs = activeScreen != null && activeScreen.gameObject != null && activeScreen.gameObject.activeSelf;
            }

            if (isOurs)
            {
                screen.ClearSecondarySideScreen();
            }

            activeScreen = null;
        }

        public static void LogFailure(Exception exception)
        {
            if (failureLogged)
            {
                return;
            }

            failureLogged = true;
            Debug.LogWarning("[ONI-Starter] 自动打印列表面板切换失败：" + exception);
        }

        private static TreeFilterableSideScreen FindPrefab(DetailsScreen screen)
        {
            if (screen == null || SideScreensField == null)
            {
                return null;
            }

            List<DetailsScreen.SideScreenRef> sideScreens;
            try
            {
                sideScreens = SideScreensField.GetValue(screen) as List<DetailsScreen.SideScreenRef>;
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                return null;
            }

            if (sideScreens == null)
            {
                return null;
            }

            foreach (DetailsScreen.SideScreenRef sideScreen in sideScreens)
            {
                if (sideScreen != null && sideScreen.screenPrefab is TreeFilterableSideScreen treeFilter)
                {
                    return treeFilter;
                }
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), nameof(DetailsScreen.Refresh))]
    public static class AutoPrintSecondaryPanelRefreshPatch
    {
        public static void Postfix(DetailsScreen __instance, GameObject go)
        {
            try
            {
                AutoPrintSecondaryScreenPanel.Refresh(__instance, go);
            }
            catch (Exception exception)
            {
                AutoPrintSecondaryScreenPanel.LogFailure(exception);
            }
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), nameof(DetailsScreen.DeactivateSideContent))]
    public static class AutoPrintSecondaryPanelDeactivatePatch
    {
        public static void Postfix(DetailsScreen __instance)
        {
            try
            {
                AutoPrintSecondaryScreenPanel.ClearIfOurs(__instance);
            }
            catch (Exception exception)
            {
                AutoPrintSecondaryScreenPanel.LogFailure(exception);
            }
        }
    }

    [HarmonyPatch(typeof(TreeFilterableSideScreen), nameof(TreeFilterableSideScreen.IsValidForTarget))]
    public static class AutoPrintSideScreenValidPatch
    {
        public static bool Prefix(GameObject target, ref bool __result)
        {
            if (AutoPrintSetup.IsAutoPrintTarget(target))
            {
                // The list is shown in the secondary (left) panel. If that
                // panel is unavailable, fall back to the right-hand panel.
                __result = !AutoPrintSecondaryScreenPanel.CanDetach();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(TreeFilterableSideScreen), "get_IsStorage")]
    public static class AutoPrintSideScreenIsStoragePatch
    {
        public static void Postfix(TreeFilterableSideScreen __instance, ref bool __result)
        {
            GameObject target = AutoPrintSetup.GetSideScreenTarget(__instance);
            if (AutoPrintSetup.IsAutoPrintTarget(target))
            {
                // Hide the "0 kg" storage suffix after each item.
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(SideScreenContent), nameof(SideScreenContent.GetTitle))]
    public static class AutoPrintSideScreenTitlePatch
    {
        public static void Postfix(SideScreenContent __instance, ref string __result)
        {
            TreeFilterableSideScreen screen = __instance as TreeFilterableSideScreen;
            if (screen == null)
            {
                return;
            }

            GameObject target = AutoPrintSetup.GetSideScreenTarget(screen);
            if (AutoPrintSetup.IsAutoPrintTarget(target))
            {
                __result = "自动打印列表";
            }
        }
    }

    [HarmonyPatch(typeof(TreeFilterable), "RefreshTint")]
    public static class AutoPrintTreeFilterableTintPatch
    {
        public static bool Prefix(TreeFilterable __instance)
        {
            // The printing pod does not need the "no storage filter set"
            // state or its tint.
            if (__instance == null)
            {
                return true;
            }

            Storage storage = __instance.GetComponent<Storage>();
            return storage == null || storage.storageID != AutoPrintSetup.PrintStorageId;
        }
    }
}
