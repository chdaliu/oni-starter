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
    /// Attached to the printing pod:
    /// - adds a Storage + TreeFilterable used only by the auto-print list;
    /// - the list contains care packages only, never duplicants;
    /// - when something is checked, a matching care package is printed
    ///   automatically once the pod is ready;
    /// - when nothing is checked, or the current random result contains no
    ///   checked item, nothing is printed and vanilla behaviour is kept.
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
        private bool updateFailed;
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
            try
            {
                UpdateAutoPrint();
                updateFailed = false;
            }
            catch (Exception exception)
            {
                // Log one failure instead of spamming the log every frame.
                if (!updateFailed)
                {
                    updateFailed = true;
                    Debug.LogWarning("[ONI-Starter] Auto-print update failed: " + exception);
                }
            }
        }

        private void UpdateAutoPrint()
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
                // If the vanilla duplicant/package selection screen is open,
                // let the player choose instead of consuming the offer in the
                // background.
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
                // Nothing is checked: stop auto-printing.
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
                // The current random result has no checked item: discard it,
                // keep the pod ready, and roll again next time.
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
                foreach (Storage existing in GetComponents<Storage>())
                {
                    if (existing != null && existing.storageID == AutoPrintSetup.PrintStorageId)
                    {
                        storage = existing;
                        break;
                    }
                }
            }

            if (filter == null)
            {
                foreach (TreeFilterable existing in GetComponents<TreeFilterable>())
                {
                    if (existing != null && existing.storageToFilterTag == AutoPrintSetup.PrintStorageId)
                    {
                        filter = existing;
                        break;
                    }
                }
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

                // Older versions may have saved duplicant tags. Migrate the
                // list automatically to keep only currently valid care
                // packages.
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
        /// Generates care-package candidates only. When care packages are
        /// enabled, vanilla probabilities produce one or two candidates. When
        /// care packages are disabled there is nothing to auto-print.
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
