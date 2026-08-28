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

            Storage storage = telepad.GetComponent<Storage>();
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

            // 只把这个 Storage 当作列表数据源，不参与真正的搬运/存储。
            storage.enabled = false;

            AutoPrintController controller = telepad.GetComponent<AutoPrintController>();
            if (controller == null)
            {
                controller = telepad.gameObject.AddComponent<AutoPrintController>();
            }

            controller.Initialize(telepad, storage, filter);

            // 尽量在玩家打开侧屏前把补给包列表准备好；失败时控制器会在 Update 中重试。
            AutoPrintController.RegisterPrintableTags();
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

    [HarmonyPatch(typeof(TreeFilterableSideScreen), nameof(TreeFilterableSideScreen.IsValidForTarget))]
    public static class AutoPrintSideScreenValidPatch
    {
        public static bool Prefix(GameObject target, ref bool __result)
        {
            if (target != null && target.GetComponent<AutoPrintController>() != null)
            {
                __result = true;
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
            if (target != null && target.GetComponent<AutoPrintController>() != null)
            {
                // 去掉每个物品后面的“0 kg”存储量后缀。
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
            if (target != null && target.GetComponent<AutoPrintController>() != null)
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
            // 打印舱不需要“未设置存储过滤”状态，也不要被染色。
            if (__instance == null)
            {
                return true;
            }

            Storage storage = __instance.GetComponent<Storage>();
            return storage == null || storage.storageID != AutoPrintSetup.PrintStorageId;
        }
    }
}
