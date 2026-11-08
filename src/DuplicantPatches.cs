using System;
using System.Collections.Generic;
using Database;
using HarmonyLib;
using Klei.AI;
using TUNING;
using UnityEngine;

namespace ONIStarter
{
    /// <summary>
    /// Rules for standard duplicants:
    /// 1. Replace random traits with three fixed traits: Twinkletoes, StrongArm
    ///    and DiversLung.
    /// 2. Remove every other random trait, including negative traits, traits
    ///    that disable chores, and traits that grant skills.
    /// 3. Use only the harmless UglyCrier stress reaction.
    /// 4. Keep a single job-related aptitude; attributes follow vanilla rules.
    /// 5. Clear learned skills, but keep the vanilla starting skill points.
    /// Bionic duplicants are left untouched.
    /// </summary>
    public static class DuplicantSanitizer
    {
        private static readonly string[] FixedTraitIds =
        {
            "Twinkletoes",
            "StrongArm",
            "DiversLung"
        };

        public static int SanitizeGeneratedTraits(
            MinionStartingStats stats,
            List<ChoreGroup> disabledChoreGroups,
            int statDelta)
        {
            if (stats == null || stats.personality == null)
            {
                return statDelta;
            }

            if (!StarterSettings.SimplifyDuplicants)
            {
                return statDelta;
            }

            // Bionic duplicants are intentionally left untouched.
            if (stats.personality.model == BionicMinionConfig.MODEL)
            {
                return statDelta;
            }

            ApplyFixedTraits(stats);

            // The fixed traits have no random stat bonus, and none of them
            // disable chore groups. Remove the bonuses that belonged to the
            // discarded random traits so attributes follow vanilla rules.
            if (disabledChoreGroups != null)
            {
                disabledChoreGroups.Clear();
            }

            return 0;
        }

        private static void ApplyFixedTraits(MinionStartingStats stats)
        {
            string baseTraitId = BaseMinionConfig.GetMinionBaseTraitIDForModel(stats.personality.model);
            List<Trait> kept = new List<Trait>();

            // The base trait is always kept and must stay first.
            Trait baseTrait = Db.Get().traits.TryGet(baseTraitId);
            if (baseTrait != null)
            {
                kept.Add(baseTrait);
            }

            // Replace all random traits with the fixed set.
            foreach (string traitId in FixedTraitIds)
            {
                Trait trait = Db.Get().traits.TryGet(traitId);
                if (trait != null && !kept.Contains(trait))
                {
                    kept.Add(trait);
                }
            }

            stats.Traits = kept;
            stats.congenitaltrait = null;

            Trait uglyCrier = Db.Get().traits.TryGet("UglyCrier");
            if (uglyCrier != null)
            {
                stats.stressTrait = uglyCrier;
            }
        }

        public static void SanitizeSpawnedDuplicant(GameObject go, MinionStartingStats stats = null)
        {
            if (go == null)
            {
                return;
            }

            if (!StarterSettings.SimplifyDuplicants)
            {
                return;
            }

            MinionIdentity identity = go.GetComponent<MinionIdentity>();
            if (identity == null)
            {
                return;
            }

            // Bionic duplicants are intentionally left untouched.
            if (identity.model == BionicMinionConfig.MODEL)
            {
                return;
            }

            string baseTraitId = BaseMinionConfig.GetMinionBaseTraitIDForModel(identity.model);

            Klei.AI.Traits traits = go.GetComponent<Klei.AI.Traits>();
            if (traits != null)
            {
                // Keep only the base trait, the three fixed traits, and the
                // stress/joy reactions. This also cleans up older saved offers
                // that still carry random traits.
                HashSet<string> allowed = new HashSet<string>(StringComparer.Ordinal)
                {
                    baseTraitId,
                    "UglyCrier"
                };

                foreach (string traitId in FixedTraitIds)
                {
                    allowed.Add(traitId);
                }

                if (stats != null)
                {
                    if (stats.stressTrait != null)
                    {
                        allowed.Add(stats.stressTrait.Id);
                    }

                    if (stats.joyTrait != null)
                    {
                        allowed.Add(stats.joyTrait.Id);
                    }
                }

                foreach (Trait trait in new List<Trait>(traits.TraitList))
                {
                    if (trait != null && !allowed.Contains(trait.Id))
                    {
                        traits.Remove(trait);
                    }
                }

                // Ensure the fixed traits are present even if a duplicant was
                // created through an alternate spawn path.
                foreach (string traitId in FixedTraitIds)
                {
                    Trait fixedTrait = Db.Get().traits.TryGet(traitId);
                    if (fixedTrait != null && !traits.HasTrait(fixedTrait))
                    {
                        traits.Add(fixedTrait);
                    }
                }

                Trait uglyCrier = Db.Get().traits.TryGet("UglyCrier");
                if (uglyCrier != null && !traits.HasTrait(uglyCrier))
                {
                    traits.Add(uglyCrier);
                }
            }

            MinionResume resume = go.GetComponent<MinionResume>();
            if (resume == null)
            {
                return;
            }

            List<string> mastered = new List<string>(resume.MasteryBySkillID.Keys);
            foreach (string skillId in mastered)
            {
                if (resume.MasteryBySkillID.TryGetValue(skillId, out bool value) && value)
                {
                    resume.UnmasterSkill(skillId);
                }
            }

            resume.MasteryBySkillID.Clear();
            if (resume.GrantedSkillIDs != null)
            {
                resume.GrantedSkillIDs.Clear();
            }

            // Do not modify BionicWaterDamageMonitor / BionicWaterStress.
            // The bionic "water damage" behaviour is a base mechanic, not a
            // trait, so it must remain vanilla.
        }

    }

    [HarmonyPatch(typeof(MinionStartingStats), "GenerateAptitudes")]
    public static class SingleProfessionAptitudePatch
    {
        public static void Postfix(MinionStartingStats __instance, string guaranteedAptitudeID)
        {
            if (__instance == null || __instance.personality == null)
            {
                return;
            }

            if (!StarterSettings.SimplifyDuplicants)
            {
                return;
            }

            // Bionic duplicants have no aptitude system; leave them as-is.
            if (__instance.personality.model == BionicMinionConfig.MODEL)
            {
                return;
            }

            List<SkillGroup> candidates = new List<SkillGroup>();
            foreach (SkillGroup group in Db.Get().SkillGroups.resources)
            {
                if (group != null && group.allowAsAptitude && group.relevantAttributes != null && group.relevantAttributes.Count > 0)
                {
                    candidates.Add(group);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            SkillGroup chosen = null;
            if (!string.IsNullOrEmpty(guaranteedAptitudeID))
            {
                chosen = candidates.Find(group => group.Id == guaranteedAptitudeID);
            }

            if (chosen == null)
            {
                chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }

            __instance.skillAptitudes.Clear();
            __instance.skillAptitudes[chosen] = DUPLICANTSTATS.APTITUDE_BONUS;
        }
    }

    [HarmonyPatch(typeof(MinionStartingStats), "GenerateTraits")]
    public static class GeneratedTraitsSanitizePatch
    {
        public static void Postfix(
            MinionStartingStats __instance,
            List<ChoreGroup> disabled_chore_groups,
            ref int __result)
        {
            __result = DuplicantSanitizer.SanitizeGeneratedTraits(
                __instance,
                disabled_chore_groups,
                __result);
        }
    }

    [HarmonyPatch(typeof(MinionStartingStats), nameof(MinionStartingStats.Apply))]
    public static class SpawnedDuplicantSanitizePatch
    {
        public static void Postfix(MinionStartingStats __instance, GameObject go)
        {
            DuplicantSanitizer.SanitizeSpawnedDuplicant(go, __instance);
        }
    }

    /// <summary>
    /// Stops skill points and skill experience from accumulating once the whole
    /// skill tree is mastered. Attribute experience is not affected.
    /// </summary>
    [HarmonyPatch(typeof(MinionResume), nameof(MinionResume.AddExperience))]
    public static class SkillPointCapPatch
    {
        public static bool Prefix(MinionResume __instance, float amount)
        {
            if (__instance == null || amount <= 0f)
            {
                return true;
            }

            if (!StarterSettings.SkillPointCap)
            {
                return true;
            }

            MinionIdentity identity = __instance.GetComponent<MinionIdentity>();
            if (identity == null || identity.model == BionicMinionConfig.MODEL)
            {
                // Bionic duplicants are intentionally left untouched.
                return true;
            }

            return !IsSkillTreeFull(__instance);
        }

        private static bool IsSkillTreeFull(MinionResume resume)
        {
            MinionIdentity identity = resume.GetComponent<MinionIdentity>();
            string model = identity != null && identity.model != Tag.Invalid
                ? identity.model.Name
                : GameTags.Minions.Models.Standard.Name;

            int total = 0;
            int mastered = 0;

            foreach (Skill skill in Db.Get().Skills.resources)
            {
                if (skill == null || skill.deprecated)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(skill.requiredDuplicantModel) &&
                    skill.requiredDuplicantModel != model)
                {
                    continue;
                }

                if (!Game.IsCorrectDlcActiveForCurrentSave(skill))
                {
                    continue;
                }

                total++;
                if (resume.HasMasteredSkill(skill.Id))
                {
                    mastered++;
                }
            }

            // Bionic duplicants do not use the normal skill tree; do not force
            // any behaviour change for them.
            return total > 0 && mastered >= total;
        }
    }
}
