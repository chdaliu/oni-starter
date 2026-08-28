using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Database;
using Klei.CustomSettings;
using UnityEngine;

namespace ONIStarter
{
    /// <summary>
    /// 挂在打印舱上：
    /// - 给打印舱补一个只用于“自动打印列表”的 Storage + TreeFilterable；
    /// - 列表只包含补给包，不包含复制人；
    /// - 有勾选时，打印舱充满后自动打印匹配的补给包；
    /// - 没有勾选或本次随机结果没有勾选内容时保持原状，不会自动打印。
    /// </summary>
    public sealed class AutoPrintController : KMonoBehaviour
    {
        public static readonly Tag CarePackageCategory = TagManager.Create("ONI_STARTER_CAREPACKAGES", "补给包");

        private static readonly FieldInfo CarePackagesField = typeof(Immigration).GetField(
            "carePackages",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo DiscoveredCategoriesField = typeof(DiscoveredResources).GetField(
            "DiscoveredCategories",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private Telepad telepad;
        private Storage storage;
        private TreeFilterable filter;
        private PrintListConfig config = new PrintListConfig();

        private bool ready;
        private bool tagsRegistered;
        private bool configLoaded;
        private bool filtersRestored;
        private bool configDirty;
        private float nextTick;
        private float nextSave;
        private List<ITelepadDeliverable> offers;

        public void Initialize(Telepad pad, Storage store, TreeFilterable tree)
        {
            telepad = pad;
            storage = store;
            filter = tree;
        }

        private void Update()
        {
            EnsureReady();

            if (configDirty && Time.unscaledTime >= nextSave)
            {
                PrintListStore.Save(config);
                configDirty = false;
            }

            if (!StarterSettings.AutoPrint)
            {
                offers = null;
                return;
            }

            if (!ready)
            {
                return;
            }

            if (Time.unscaledTime < nextTick)
            {
                return;
            }

            nextTick = Time.unscaledTime + 0.75f;

            if (SpeedControlScreen.Instance != null && SpeedControlScreen.Instance.IsPaused)
            {
                return;
            }

            if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameOver())
            {
                return;
            }

            if (ImmigrantScreen.instance != null && ImmigrantScreen.instance.gameObject.activeInHierarchy)
            {
                // 玩家已经打开原版选人/选包界面时，让玩家自己选，避免后台抢走选项。
                return;
            }

            if (telepad == null || Immigration.Instance == null)
            {
                return;
            }

            Operational operational = telepad.GetComponent<Operational>();
            if (operational != null && !operational.IsOperational)
            {
                return;
            }

            if (!Immigration.Instance.ImmigrantsAvailable)
            {
                offers = null;
                return;
            }

            if (filter == null)
            {
                return;
            }

            HashSet<Tag> checkedTags = filter.GetTags();
            if (checkedTags == null || checkedTags.Count == 0)
            {
                // 没有任何勾选：停止自动打印。
                offers = null;
                return;
            }

            if (offers == null || offers.Count == 0)
            {
                try
                {
                    offers = RollOffers(checkedTags);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[ONI-Starter] 生成打印候选失败：" + exception);
                    offers = null;
                    return;
                }
            }

            if (offers == null || offers.Count == 0)
            {
                return;
            }

            ITelepadDeliverable match = null;
            foreach (ITelepadDeliverable offer in offers)
            {
                if (IsWanted(offer, checkedTags))
                {
                    match = offer;
                    break;
                }
            }

            if (match == null)
            {
                // 本次随机结果没有勾选内容：丢弃，保持满仓，下一次再重掷。
                offers = null;
                return;
            }

            try
            {
                telepad.OnAcceptDelivery(match);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 自动打印失败：" + exception);
            }

            offers = null;
        }

        protected override void OnCleanUp()
        {
            if (configDirty)
            {
                PrintListStore.Save(config);
                configDirty = false;
            }

            base.OnCleanUp();
        }

        private void EnsureReady()
        {
            if (ready)
            {
                return;
            }

            if (telepad == null)
            {
                telepad = GetComponent<Telepad>();
            }

            if (storage == null)
            {
                storage = GetComponent<Storage>();
            }

            if (filter == null)
            {
                filter = GetComponent<TreeFilterable>();
            }

            if (!tagsRegistered)
            {
                tagsRegistered = RegisterPrintableTags();
            }

            if (!configLoaded)
            {
                config = PrintListStore.Load();
                configLoaded = true;
            }

            if (!filtersRestored && filter != null && configLoaded && tagsRegistered)
            {
                HashSet<Tag> validTags = GetCarePackageTags();
                HashSet<Tag> savedTags = new HashSet<Tag>();
                foreach (string tagName in config.tags)
                {
                    if (string.IsNullOrEmpty(tagName))
                    {
                        continue;
                    }

                    Tag tag = TagManager.Create(tagName);
                    if (validTags.Contains(tag))
                    {
                        savedTags.Add(tag);
                    }
                }

                filter.UpdateFilters(savedTags);
                filter.OnFilterChanged += OnFilterChanged;
                filtersRestored = true;

                // 旧版本可能保存过复制人 Tag；自动迁移为只保留当前可用的补给包。
                config.tags = savedTags
                    .Select(tag => tag.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();
                configDirty = true;
                nextSave = Time.unscaledTime;
            }

            ready = tagsRegistered && configLoaded && filtersRestored && filter != null;
        }

        public static bool RegisterPrintableTags()
        {
            DiscoveredResources discoveredResources = DiscoveredResources.Instance;
            if (discoveredResources == null || DiscoveredCategoriesField == null)
            {
                return false;
            }

            Dictionary<Tag, HashSet<Tag>> categories =
                DiscoveredCategoriesField.GetValue(discoveredResources) as Dictionary<Tag, HashSet<Tag>>;
            if (categories == null)
            {
                categories = new Dictionary<Tag, HashSet<Tag>>();
                DiscoveredCategoriesField.SetValue(discoveredResources, categories);
            }

            if (Immigration.Instance == null)
            {
                return false;
            }

            if (CarePackagesField == null)
            {
                FillMissingNamesSafely();
                return true;
            }

            List<CarePackageInfo> packages = CarePackagesField.GetValue(Immigration.Instance) as List<CarePackageInfo>;

            if (packages == null)
            {
                FillMissingNamesSafely();
                return true;
            }

            List<Tag> packageTags = new List<Tag>();
            foreach (CarePackageInfo package in packages)
            {
                if (package == null || string.IsNullOrEmpty(package.id))
                {
                    continue;
                }

                Tag tag = TagManager.Create(package.id);
                if (tag != Tag.Invalid)
                {
                    packageTags.Add(tag);
                }
            }

            AddTagsToCategory(categories, CarePackageCategory, packageTags);
            FillMissingNamesSafely();
            return true;
        }

        private static void FillMissingNamesSafely()
        {
            try
            {
                TagManager.FillMissingProperNames();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 补全打印物品名称失败：" + exception);
            }
        }

        private static HashSet<Tag> GetCarePackageTags()
        {
            HashSet<Tag> result = new HashSet<Tag>();
            if (Immigration.Instance == null || CarePackagesField == null)
            {
                return result;
            }

            List<CarePackageInfo> packages = CarePackagesField.GetValue(Immigration.Instance) as List<CarePackageInfo>;
            if (packages == null)
            {
                return result;
            }

            foreach (CarePackageInfo package in packages)
            {
                if (package != null && !string.IsNullOrEmpty(package.id))
                {
                    result.Add(TagManager.Create(package.id));
                }
            }

            return result;
        }

        private static void AddTagsToCategory(
            Dictionary<Tag, HashSet<Tag>> categories,
            Tag category,
            IEnumerable<Tag> tags)
        {
            if (!categories.TryGetValue(category, out HashSet<Tag> set))
            {
                set = new HashSet<Tag>();
                categories[category] = set;
            }

            foreach (Tag tag in tags)
            {
                if (tag != Tag.Invalid)
                {
                    set.Add(tag);
                }
            }
        }

        private void OnFilterChanged(HashSet<Tag> tags)
        {
            if (config == null)
            {
                return;
            }

            config.tags = tags == null
                ? new List<string>()
                : tags.Select(tag => tag.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();

            configDirty = true;
            nextSave = Time.unscaledTime + 0.25f;
            offers = null;
        }

        private static bool IsWanted(ITelepadDeliverable offer, HashSet<Tag> checkedTags)
        {
            if (offer is CarePackageInfo directPackage)
            {
                return IsWantedCarePackage(directPackage, checkedTags);
            }

            if (offer is CarePackageContainer.CarePackageInstanceData packageInstance)
            {
                return IsWantedCarePackage(packageInstance.info, checkedTags);
            }

            return false;
        }

        private static bool IsWantedCarePackage(CarePackageInfo package, HashSet<Tag> checkedTags)
        {
            if (package == null || string.IsNullOrEmpty(package.id))
            {
                return false;
            }

            if (checkedTags.Contains(CarePackageCategory))
            {
                return true;
            }

            return checkedTags.Contains(TagManager.Create(package.id));
        }

        private static ITelepadDeliverable MakeCarePackageDeliverable(CarePackageInfo info)
        {
            if (info == null)
            {
                return null;
            }

            CarePackageContainer.CarePackageInstanceData data = new CarePackageContainer.CarePackageInstanceData();
            data.info = info;

            if (info.facadeID == "SELECTRANDOM")
            {
                ResourceSet<EquippableFacadeResource> facadeSet = Db.GetEquippableFacades();
                if (facadeSet != null && facadeSet.resources != null)
                {
                    List<EquippableFacadeResource> facades =
                        facadeSet.resources.FindAll(match => match.DefID == info.id);
                    if (facades.Count > 0)
                    {
                        data.facadeID = facades[UnityEngine.Random.Range(0, facades.Count)].Id;
                    }
                }
            }
            else
            {
                data.facadeID = info.facadeID;
            }

            return data;
        }

        /// <summary>
        /// 只生成补给包候选。开启补给包时按原版概率生成 1 或 2 个候选；
        /// 关闭补给包时没有可自动打印的内容。
        /// </summary>
        private static List<ITelepadDeliverable> RollOffers(HashSet<Tag> checkedTags)
        {
            List<ITelepadDeliverable> result = new List<ITelepadDeliverable>();

            bool carePackagesEnabled = true;
            try
            {
                CustomGameSettings settings = CustomGameSettings.Instance;
                if (settings != null)
                {
                    SettingLevel state = settings.GetCurrentQualitySetting(CustomGameSettingConfigs.CarePackages);
                    carePackagesEnabled = state != null && state.id == "Enabled";
                }
            }
            catch
            {
                carePackagesEnabled = true;
            }

            if (!carePackagesEnabled || Immigration.Instance == null)
            {
                return result;
            }

            int packageCount = UnityEngine.Random.Range(0, 101) <= 70 ? 1 : 2;

            for (int i = 0; i < packageCount; i++)
            {
                CarePackageInfo package = null;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    package = Immigration.Instance.RandomCarePackage();
                    if (package != null &&
                        !result.OfType<CarePackageContainer.CarePackageInstanceData>()
                            .Any(existing => existing.info != null && existing.info.id == package.id))
                    {
                        break;
                    }
                }

                if (package != null)
                {
                    ITelepadDeliverable deliverable = MakeCarePackageDeliverable(package);
                    if (deliverable != null)
                    {
                        result.Add(deliverable);
                    }
                }
            }

            return result;
        }
    }
}
