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
    /// 标准复制人的简化规则：
    /// 1. 移除负面特质、会禁用工作的特质、会直接授予技能的特质。
    /// 2. 压力反应统一为无害的大哭（UglyCrier）。
    /// 3. 只保留一项与职业对应的兴趣；属性点按原版规则计算。
    /// 4. 清空技能，但起始技能点保持原版。
    /// 仿生复制人不参与以上任何修改。
    /// </summary>
    public static class DuplicantSanitizer
    {
        private static readonly HashSet<string> KeepBeneficial = new HashSet<string>(StringComparer.Ordinal)
        {
            "SunnyDisposition",
            "Regeneration",
            "DeeperDiversLungs",
            "RockCrusher"
        };

        public static void SanitizeStartingStats(MinionStartingStats stats)
        {
            if (stats == null || stats.personality == null)
            {
                return;
            }

            if (!StarterSettings.SimplifyDuplicants)
            {
                return;
            }

            // 仿生复制人目前完全保持原版。
            if (stats.personality.model == BionicMinionConfig.MODEL)
            {
                return;
            }

            string baseTraitId = BaseMinionConfig.GetMinionBaseTraitIDForModel(stats.personality.model);
            List<Trait> kept = new List<Trait>();
            foreach (Trait trait in stats.Traits)
            {
                if (trait == null)
                {
                    continue;
                }

                if (trait.Id == baseTraitId || !IsBlockedTrait(trait))
                {
                    kept.Add(trait);
                }
            }

            stats.Traits = kept;

            if (stats.congenitaltrait != null && IsBlockedTrait(stats.congenitaltrait))
            {
                stats.congenitaltrait = null;
            }

            Trait uglyCrier = Db.Get().traits.TryGet("UglyCrier");
            if (uglyCrier != null)
            {
                stats.stressTrait = uglyCrier;
            }
        }

        public static void SanitizeSpawnedDuplicant(GameObject go)
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

            // 仿生复制人目前完全保持原版。
            if (identity.model == BionicMinionConfig.MODEL)
            {
                return;
            }

            string baseTraitId = BaseMinionConfig.GetMinionBaseTraitIDForModel(identity.model);

            Klei.AI.Traits traits = go.GetComponent<Klei.AI.Traits>();
            if (traits != null)
            {
                foreach (Trait trait in new List<Trait>(traits.TraitList))
                {
                    if (trait != null &&
                        trait.Id != baseTraitId &&
                        trait.Id != "UglyCrier" &&
                        IsBlockedTrait(trait))
                    {
                        traits.Remove(trait);
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

            // 注意：不要修改 BionicWaterDamageMonitor / BionicWaterStress。
            // 仿生复制人“遇水漏电”属于基础行为，不是特质，这里保持原版。
        }

        private static bool IsBlockedTrait(Trait trait)
        {
            if (trait == null || string.IsNullOrEmpty(trait.Id))
            {
                return true;
            }

            string id = trait.Id;

            // 基础特质由调用方保留；None 是无效果占位特质。
            if (id == "None")
            {
                return false;
            }

            // 授予技能的特质：保证新复制人没有任何技能。
            if (id.StartsWith("GrantSkill_", StringComparison.Ordinal))
            {
                return true;
            }

            if (id == "Minnow" ||
                id.StartsWith("BionicUpgrade_Skill", StringComparison.Ordinal) ||
                id.StartsWith("BionicUpgrade_SkilledWorker", StringComparison.Ordinal))
            {
                return true;
            }

            // 仿生复制人的故障特质全部视为负面。
            if (id.StartsWith("BionicBug", StringComparison.Ordinal))
            {
                return true;
            }

            // 会禁用某类工作的特质（例如不能攻击/不能建造/不能研究）一律移除。
            if (trait.disabledChoreGroups != null && trait.disabledChoreGroups.Length > 0)
            {
                return true;
            }

            if (trait.PositiveTrait)
            {
                return false;
            }

            if (KeepBeneficial.Contains(id))
            {
                return false;
            }

            // 其余非正面特质全部视为需要移除的负面/有害特质。
            return true;
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

            // 仿生复制人没有兴趣系统，保持原样。
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

    [HarmonyPatch(typeof(MinionStartingStats), "GenerateStats")]
    public static class StartingStatsSanitizePatch
    {
        public static void Postfix(MinionStartingStats __instance)
        {
            DuplicantSanitizer.SanitizeStartingStats(__instance);
        }
    }

    [HarmonyPatch(typeof(MinionStartingStats), nameof(MinionStartingStats.Apply))]
    public static class SpawnedDuplicantSanitizePatch
    {
        public static void Postfix(GameObject go)
        {
            DuplicantSanitizer.SanitizeSpawnedDuplicant(go);
        }
    }

    /// <summary>
    /// 技能树全部点满后，不再继续累积技能点/技能经验。
    /// 只拦截“技能点经验”，属性经验不受影响。
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
                // 仿生复制人目前完全保持原版。
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

            // 仿生复制人没有普通技能树，不强行改变其行为。
            return total > 0 && mastered >= total;
        }
    }
}
