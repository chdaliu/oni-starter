using System;
using System.Collections.Generic;
using Database;
using Klei.AI;
using UnityEngine;

namespace ONIStarter
{
    /// <summary>
    /// Shared logic for the mechanical ascension feature. A conversion always
    /// creates a fresh duplicant of the target model and removes the old one,
    /// so the game never has to swap models on a live object. Skill points and
    /// skill experience are deliberately not carried over.
    /// </summary>
    public static class MechanicalAscensionService
    {
        public const string BionicDlcId = "DLC3_ID";

        public static readonly Tag AscensionCorpseTag =
            TagManager.Create("ONI_STARTER_ASCENSION_CORPSE");

        private static readonly Dictionary<string, string> StandardToBionic =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "AMARI", "GIZMO" },
                { "MEEP", "CHIP" },
                { "ARI", "EDWIREDO" },
                { "NAILS", "SONYAR" },
                { "PEI", "STEELA" }
            };

        private static readonly Dictionary<string, string> BionicToStandard =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "GIZMO", "AMARI" },
                { "CHIP", "MEEP" },
                { "EDWIREDO", "ARI" },
                { "SONYAR", "NAILS" },
                { "STEELA", "PEI" }
            };

        public static bool IsAvailable
        {
            get
            {
                return StarterSettings.MechanicalAscension && IsBionicDlcActive();
            }
        }

        public static bool IsBionicDlcActive()
        {
            return Game.Instance != null &&
                SaveLoader.Instance != null &&
                Game.IsDlcActiveForCurrentSave(BionicDlcId);
        }

        public static bool IsBionic(GameObject duplicant)
        {
            MinionIdentity identity = duplicant != null
                ? duplicant.GetComponent<MinionIdentity>()
                : null;
            return identity != null && identity.model == BionicMinionConfig.MODEL;
        }

        public static Telepad FindNearestTelepad(GameObject duplicant)
        {
            if (duplicant == null)
            {
                return null;
            }

            int worldId = duplicant.GetMyWorldId();
            if (worldId == 255)
            {
                return null;
            }

            Telepad nearest = null;
            int nearestDistance = int.MaxValue;
            int duplicantCell = Grid.PosToCell(duplicant);

            foreach (Telepad telepad in Components.Telepads.GetWorldItems(worldId))
            {
                if (telepad == null)
                {
                    continue;
                }

                int distance = Grid.GetCellDistance(duplicantCell, Grid.PosToCell(telepad));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = telepad;
                }
            }

            return nearest;
        }

        public static bool IsNearTelepad(GameObject duplicant, Telepad telepad)
        {
            if (duplicant == null || telepad == null)
            {
                return false;
            }

            Building building = telepad.GetComponent<Building>();
            if (building == null)
            {
                return Grid.GetCellDistance(
                    Grid.PosToCell(duplicant),
                    Grid.PosToCell(telepad)) <= 4;
            }

            Extents extents = building.GetExtents();
            Grid.CellToXY(Grid.PosToCell(duplicant), out int x, out int y);
            const int margin = 2;
            return x >= extents.x - margin &&
                x < extents.x + extents.width + margin &&
                y >= extents.y - margin &&
                y < extents.y + extents.height + margin;
        }

        /// <summary>
        /// Finds a non-solid cell just outside the printing pod that the
        /// duplicant can actually reach. Returns Grid.InvalidCell on failure.
        /// </summary>
        public static int FindApproachCell(GameObject duplicant, Telepad telepad)
        {
            if (duplicant == null || telepad == null)
            {
                return Grid.InvalidCell;
            }

            Building building = telepad.GetComponent<Building>();
            Navigator navigator = duplicant.GetComponent<Navigator>();
            if (building == null || navigator == null)
            {
                return Grid.InvalidCell;
            }

            Extents extents = building.GetExtents();
            int duplicantCell = Grid.PosToCell(duplicant);
            int bestCell = Grid.InvalidCell;
            int bestDistance = int.MaxValue;

            for (int x = extents.x - 1; x <= extents.x + extents.width; x++)
            {
                TryCell(x, extents.y - 1);
                TryCell(x, extents.y + extents.height);
            }

            for (int y = extents.y; y < extents.y + extents.height; y++)
            {
                TryCell(extents.x - 1, y);
                TryCell(extents.x + extents.width, y);
            }

            return bestCell;

            void TryCell(int x, int y)
            {
                int cell = Grid.XYToCell(x, y);
                if (!Grid.IsValidCell(cell) ||
                    !Grid.IsWorldValidCell(cell) ||
                    Grid.IsSolidCell(cell) ||
                    !navigator.CanReach(cell))
                {
                    return;
                }

                int distance = Grid.GetCellDistance(duplicantCell, cell);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCell = cell;
                }
            }
        }

        /// <summary>
        /// Converts a living duplicant or a corpse into the other model.
        /// The old object is destroyed on success.
        /// </summary>
        public static bool Convert(GameObject source, bool toBionic, Vector3 spawnPosition)
        {
            if (source == null)
            {
                return false;
            }

            MinionIdentity oldIdentity = source.GetComponent<MinionIdentity>();
            if (oldIdentity == null)
            {
                return false;
            }

            if (toBionic && !IsBionicDlcActive())
            {
                return false;
            }

            Tag targetModel = toBionic ? BionicMinionConfig.MODEL : MinionConfig.MODEL;
            string prefabId = BaseMinionConfig.GetMinionIDForModel(targetModel);
            GameObject prefab = Assets.TryGetPrefab(TagManager.Create(prefabId));
            if (prefab == null)
            {
                return false;
            }

            Personality personality = ChoosePersonality(oldIdentity, targetModel);
            if (personality == null)
            {
                return false;
            }

            GameObject newDuplicant = null;
            try
            {
                newDuplicant = Util.KInstantiate(prefab);
                if (newDuplicant == null)
                {
                    return false;
                }

                newDuplicant.name = prefab.name;
                newDuplicant.transform.SetPosition(spawnPosition);

                if (Immigration.Instance != null)
                {
                    Immigration.Instance.ApplyDefaultPersonalPriorities(newDuplicant);
                }

                newDuplicant.SetActive(true);
                MinionStartingStats stats = new MinionStartingStats(personality);
                stats.Apply(newDuplicant);

                MinionIdentity newIdentity = newDuplicant.GetComponent<MinionIdentity>();
                if (newIdentity != null)
                {
                    newIdentity.SetName(oldIdentity.GetProperName());
                    newIdentity.gender = oldIdentity.gender;
                    newIdentity.genderStringKey = oldIdentity.genderStringKey;
                    newIdentity.voiceIdx = oldIdentity.voiceIdx;
                    newIdentity.stickerType = oldIdentity.stickerType;
                }

                CopyFaceAccessories(source, newDuplicant);
                CopyPersonalSettings(source, newDuplicant);

                // The existing sanitizer applies the simplified-duplicant rules
                // to standard duplicants and intentionally leaves bionics
                // untouched.
                DuplicantSanitizer.SanitizeSpawnedDuplicant(newDuplicant, stats);

                MinionResume resume = newDuplicant.GetComponent<MinionResume>();
                if (resume != null)
                {
                    // Both conversions permanently discard every skill point
                    // and all accumulated skill experience. Learned skills are
                    // cleared separately by the simplified-duplicant sanitizer
                    // when that setting is enabled.
                    resume.ForceSetSkillPoints(0);
                }

                WorldContainer world = newDuplicant.GetMyWorld();
                if (world != null)
                {
                    world.SetDupeVisited();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 机械飞升转换失败：" + exception);
                if (newDuplicant != null)
                {
                    Util.KDestroyGameObject(newDuplicant);
                }

                return false;
            }

            Util.KDestroyGameObject(source);
            return true;
        }

        /// <summary>
        /// Moves a dead duplicant toward the pod, or converts it immediately
        /// when it is already in front of the pod.
        /// </summary>
        public static bool BeginCorpseAscension(GameObject corpse, bool toBionic, Telepad telepad)
        {
            if (corpse == null || telepad == null || !corpse.HasTag(GameTags.Dead))
            {
                return false;
            }

            Vector3 spawnPosition = Grid.CellToPosCBC(
                Grid.PosToCell(telepad),
                Grid.SceneLayer.Move);

            if (IsNearTelepad(corpse, telepad))
            {
                return Convert(corpse, toBionic, spawnPosition);
            }

            MechanicalAscensionStation station = telepad.GetComponent<MechanicalAscensionStation>();
            return station != null && station.RequestCorpse(corpse);
        }

        private static Personality ChoosePersonality(MinionIdentity identity, Tag targetModel)
        {
            string nameKey = identity != null ? identity.nameStringKey : null;
            if (!string.IsNullOrEmpty(nameKey))
            {
                Personality sameName = Db.Get().Personalities.GetPersonalityFromNameStringKey(nameKey);
                if (sameName != null && sameName.model == targetModel && !sameName.Disabled)
                {
                    return sameName;
                }

                string mappedKey;
                Dictionary<string, string> mapping =
                    targetModel == BionicMinionConfig.MODEL ? StandardToBionic : BionicToStandard;

                if (mapping.TryGetValue(nameKey, out mappedKey))
                {
                    Personality mapped = Db.Get().Personalities.GetPersonalityFromNameStringKey(mappedKey);
                    if (mapped != null && mapped.model == targetModel && !mapped.Disabled)
                    {
                        return mapped;
                    }
                }
            }

            List<Personality> candidates = Db.Get().Personalities
                .GetAll(true, false)
                .FindAll(personality => personality != null &&
                    personality.model == targetModel &&
                    !personality.Disabled);

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static void CopyFaceAccessories(GameObject source, GameObject target)
        {
            Accessorizer sourceAccessorizer = source.GetComponent<Accessorizer>();
            Accessorizer targetAccessorizer = target.GetComponent<Accessorizer>();
            if (sourceAccessorizer == null || targetAccessorizer == null)
            {
                return;
            }

            AccessorySlot[] faceSlots =
            {
                Db.Get().AccessorySlots.HeadShape,
                Db.Get().AccessorySlots.Mouth,
                Db.Get().AccessorySlots.Eyes,
                Db.Get().AccessorySlots.Hair
            };

            foreach (AccessorySlot slot in faceSlots)
            {
                Accessory accessory = sourceAccessorizer.GetAccessory(slot);
                if (accessory == null)
                {
                    continue;
                }

                Accessory existing = targetAccessorizer.GetAccessory(slot);
                if (existing != null && existing != accessory)
                {
                    targetAccessorizer.RemoveAccessory(existing);
                }

                targetAccessorizer.AddAccessory(accessory);
            }

            targetAccessorizer.UpdateHairBasedOnHat();
        }

        private static void CopyPersonalSettings(GameObject source, GameObject target)
        {
            ChoreConsumer sourceConsumer = source.GetComponent<ChoreConsumer>();
            ChoreConsumer targetConsumer = target.GetComponent<ChoreConsumer>();
            if (sourceConsumer != null && targetConsumer != null)
            {
                Dictionary<HashedString, ChoreConsumer.PriorityInfo> priorities =
                    sourceConsumer.GetChoreGroupPriorities();
                if (priorities != null)
                {
                    targetConsumer.SetChoreGroupPriorities(
                        new Dictionary<HashedString, ChoreConsumer.PriorityInfo>(priorities));
                }
            }

            // Food permissions are intentionally left at the target model's
            // defaults. Bionic duplicants do not eat, and copying their food
            // restrictions to a standard duplicant could make it refuse food.
            Schedulable sourceSchedulable = source.GetComponent<Schedulable>();
            Schedulable targetSchedulable = target.GetComponent<Schedulable>();
            if (sourceSchedulable != null &&
                targetSchedulable != null &&
                ScheduleManager.Instance != null)
            {
                Schedule schedule = ScheduleManager.Instance.GetSchedule(sourceSchedulable);
                if (schedule != null)
                {
                    Schedule currentSchedule = ScheduleManager.Instance.GetSchedule(targetSchedulable);
                    if (currentSchedule != null && currentSchedule != schedule)
                    {
                        currentSchedule.Unassign(targetSchedulable);
                    }

                    schedule.Unassign(sourceSchedulable);
                    schedule.Assign(targetSchedulable);
                }
            }
        }
    }

    /// <summary>
    /// Adds the mechanical ascension buttons to the selected duplicant's user
    /// menu and makes living duplicants walk to the printing pod on click.
    /// </summary>
    public sealed class MechanicalAscensionMenu : KMonoBehaviour
    {
        private bool pending;
        private bool initialized;
        private Telepad targetTelepad;
        private float nextCheck;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenu);
            Subscribe((int)GameHashes.StatusChange, OnRefreshUserMenu);
        }

        private void OnRefreshUserMenu(object data)
        {
            if (!MechanicalAscensionService.IsAvailable ||
                Game.Instance == null ||
                Game.Instance.userMenu == null)
            {
                return;
            }

            MinionIdentity identity = GetComponent<MinionIdentity>();
            if (identity == null)
            {
                return;
            }

            bool isDead = gameObject.HasTag(GameTags.Dead);
            bool isStandard = identity.model == MinionConfig.MODEL;
            bool isBionic = identity.model == BionicMinionConfig.MODEL;
            if (!isStandard && !isBionic)
            {
                return;
            }

            Telepad telepad = MechanicalAscensionService.FindNearestTelepad(gameObject);
            bool hasTelepad = telepad != null;
            bool hasStation = telepad != null &&
                telepad.GetComponent<MechanicalAscensionStation>() != null;

            string text = isBionic ? "秽土转生" : "机械飞升";
            string icon = isBionic ? "action_control" : "action_power";
            string tooltip;
            if (!hasTelepad)
            {
                tooltip = "没有可用的打印舱";
            }
            else if (isDead && !hasStation)
            {
                tooltip = "打印舱尚未准备好";
            }
            else if (!isDead && pending)
            {
                tooltip = "正在前往打印舱...";
            }
            else if (isDead)
            {
                tooltip = isBionic
                    ? "将遗体运送到打印舱并秽土转生。所有技能点将永久丢失。"
                    : "将遗体运送到打印舱并机械飞升。所有技能点将永久丢失。";
            }
            else
            {
                tooltip = isBionic
                    ? "前往打印舱并恢复为标准复制人。所有技能点将永久丢失。"
                    : "前往打印舱并改造成仿生人。所有技能点将永久丢失。";
            }

            bool interactable = hasTelepad;
            KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo(
                icon,
                text,
                delegate { OnClick(); },
                global::Action.NumActions,
                null,
                null,
                null,
                tooltip,
                interactable);

            Game.Instance.userMenu.AddButton(gameObject, button, 0.9f);
        }

        private void OnClick()
        {
            if (!MechanicalAscensionService.IsAvailable)
            {
                return;
            }

            MinionIdentity identity = GetComponent<MinionIdentity>();
            if (identity == null)
            {
                return;
            }

            Telepad telepad = MechanicalAscensionService.FindNearestTelepad(gameObject);
            if (telepad == null)
            {
                PlayNegative();
                return;
            }

            bool isDead = gameObject.HasTag(GameTags.Dead);
            bool isStandard = identity.model == MinionConfig.MODEL;
            bool isBionic = identity.model == BionicMinionConfig.MODEL;
            if (!isStandard && !isBionic)
            {
                return;
            }

            bool toBionic = !isBionic;
            if (isDead)
            {
                bool started = MechanicalAscensionService.BeginCorpseAscension(
                    gameObject,
                    toBionic,
                    telepad);
                if (!started)
                {
                    PlayNegative();
                }

                return;
            }

            if (MechanicalAscensionService.IsNearTelepad(gameObject, telepad))
            {
                Vector3 spawnPosition = Grid.CellToPosCBC(
                    Grid.PosToCell(gameObject),
                    Grid.SceneLayer.Move);
                if (!MechanicalAscensionService.Convert(gameObject, toBionic, spawnPosition))
                {
                    PlayNegative();
                }

                return;
            }

            int approachCell = MechanicalAscensionService.FindApproachCell(gameObject, telepad);
            if (approachCell == Grid.InvalidCell)
            {
                PlayNegative();
                return;
            }

            pending = true;
            targetTelepad = telepad;
            nextCheck = Time.unscaledTime + 0.25f;

            MoveToLocationMonitor.Instance moveMonitor =
                gameObject.GetSMI<MoveToLocationMonitor.Instance>();
            if (moveMonitor != null)
            {
                moveMonitor.MoveToLocation(approachCell);
            }
            else
            {
                Navigator navigator = GetComponent<Navigator>();
                if (navigator == null || !navigator.GoTo(approachCell))
                {
                    pending = false;
                    PlayNegative();
                    return;
                }
            }

            if (Game.Instance != null && Game.Instance.userMenu != null)
            {
                Game.Instance.userMenu.Refresh(gameObject);
            }
        }

        private void Update()
        {
            if (!pending)
            {
                return;
            }

            if (gameObject == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (gameObject.HasTag(GameTags.Dead))
            {
                pending = false;
                return;
            }

            if (targetTelepad == null)
            {
                pending = false;
                return;
            }

            if (Time.unscaledTime < nextCheck)
            {
                return;
            }

            nextCheck = Time.unscaledTime + 0.5f;
            if (!MechanicalAscensionService.IsNearTelepad(gameObject, targetTelepad))
            {
                return;
            }

            pending = false;
            bool toBionic = !MechanicalAscensionService.IsBionic(gameObject);
            Vector3 spawnPosition = Grid.CellToPosCBC(
                Grid.PosToCell(gameObject),
                Grid.SceneLayer.Move);
            if (!MechanicalAscensionService.Convert(gameObject, toBionic, spawnPosition))
            {
                PlayNegative();
            }
        }

        private void PlayNegative()
        {
            KMonoBehaviour.PlaySound(GlobalAssets.GetSound("Negative"));
        }
    }

    /// <summary>
    /// Lives on the printing pod. It accepts marked corpses through a fetch
    /// chore and converts them when they are delivered.
    /// </summary>
    public sealed class MechanicalAscensionStation : KMonoBehaviour
    {
        public static readonly Tag StorageId =
            TagManager.Create("ONI_STARTER_ASCENSION_STORAGE");

        private Telepad telepad;
        private Storage storage;
        private readonly Dictionary<GameObject, FetchChore> chores =
            new Dictionary<GameObject, FetchChore>();
        private bool converting;

        public void Initialize(Telepad pad)
        {
            telepad = pad;

            if (storage == null)
            {
                foreach (Storage existing in GetComponents<Storage>())
                {
                    if (existing != null && existing.storageID == StorageId)
                    {
                        storage = existing;
                        break;
                    }
                }
            }

            if (storage == null)
            {
                storage = gameObject.AddComponent<Storage>();
                storage.storageID = StorageId;
                storage.storageFilters = new List<Tag> { GameTags.Corpse };
                storage.capacityKg = 100f;
                storage.showInUI = false;
                storage.allowItemRemoval = false;
                storage.doDiseaseTransfer = false;
                storage.storeDropsFromButcherables = false;
                storage.dropOnLoad = false;
                storage.allowClearable = false;
                storage.showCapacityStatusItem = false;
                storage.showDescriptor = false;
                storage.showSideScreenTitleBar = false;
                storage.useGunForDelivery = false;
            }

            storage.OnStorageChange -= OnStorageChanged;
            storage.OnStorageChange += OnStorageChanged;

            // A save made while a corpse was already inside the pod storage
            // needs to finish the conversion after loading.
            if (GameScheduler.Instance != null &&
                storage.items != null &&
                storage.items.Count > 0)
            {
                foreach (GameObject item in new List<GameObject>(storage.items))
                {
                    if (item != null && item.HasTag(MechanicalAscensionService.AscensionCorpseTag))
                    {
                        GameScheduler.Instance.Schedule(
                            "ONIStarterMechanicalAscensionResume",
                            0f,
                            delegate(object data) { OnStorageChanged(data as GameObject); },
                            item);
                    }
                }
            }
        }

        public bool RequestCorpse(GameObject corpse)
        {
            if (corpse == null || storage == null)
            {
                return false;
            }

            if (corpse.HasTag(GameTags.Stored))
            {
                return false;
            }

            if (chores.TryGetValue(corpse, out FetchChore activeChore) &&
                activeChore != null &&
                !activeChore.isComplete)
            {
                return true;
            }

            corpse.AddTag(MechanicalAscensionService.AscensionCorpseTag);

            Pickupable pickupable = corpse.GetComponent<Pickupable>();
            float amount = pickupable != null
                ? Mathf.Max(1f, pickupable.FetchTotalAmount)
                : 30f;

            try
            {
                FetchChore chore = new FetchChore(
                    Db.Get().ChoreTypes.FetchCritical,
                    storage,
                    amount,
                    new HashSet<Tag> { GameTags.BaseMinion },
                    FetchChore.MatchCriteria.MatchTags,
                    MechanicalAscensionService.AscensionCorpseTag,
                    operational_requirement: Operational.State.None);

                chore.allowMultifetch = false;
                chores[corpse] = chore;
                return true;
            }
            catch (Exception exception)
            {
                corpse.RemoveTag(MechanicalAscensionService.AscensionCorpseTag);
                Debug.LogWarning("[ONI-Starter] 创建机械飞升搬运任务失败：" + exception);
                return false;
            }
        }

        protected override void OnCleanUp()
        {
            if (storage != null)
            {
                storage.OnStorageChange -= OnStorageChanged;
            }

            base.OnCleanUp();
        }

        private void OnStorageChanged(GameObject item)
        {
            if (converting || item == null || storage == null)
            {
                return;
            }

            if (!item.HasTag(MechanicalAscensionService.AscensionCorpseTag))
            {
                return;
            }

            converting = true;
            try
            {
                chores.Remove(item);

                MinionIdentity identity = item.GetComponent<MinionIdentity>();
                if (identity == null)
                {
                    FailCorpse(item);
                    return;
                }

                bool toBionic = identity.model != BionicMinionConfig.MODEL;
                if (toBionic && !MechanicalAscensionService.IsBionicDlcActive())
                {
                    FailCorpse(item);
                    return;
                }

                if (telepad == null)
                {
                    FailCorpse(item);
                    return;
                }

                Vector3 spawnPosition = Grid.CellToPosCBC(
                    Grid.PosToCell(telepad),
                    Grid.SceneLayer.Move);

                if (!MechanicalAscensionService.Convert(item, toBionic, spawnPosition))
                {
                    FailCorpse(item);
                }
            }
            finally
            {
                converting = false;
            }
        }

        private void FailCorpse(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            item.RemoveTag(MechanicalAscensionService.AscensionCorpseTag);
            if (storage != null && item.HasTag(GameTags.Stored))
            {
                storage.Drop(item);
            }
        }
    }
}
