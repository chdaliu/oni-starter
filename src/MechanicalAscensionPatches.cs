using System;
using HarmonyLib;
using UnityEngine;

namespace ONIStarter
{
    [HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
    public static class MechanicalAscensionMinionPatch
    {
        public static void Postfix(MinionIdentity __instance)
        {
            if (__instance == null)
            {
                return;
            }

            try
            {
                MechanicalAscensionMenu menu =
                    __instance.gameObject.AddOrGet<MechanicalAscensionMenu>();
                menu.Initialize();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 初始化机械飞升按钮失败：" + exception);
            }
        }
    }

    [HarmonyPatch(typeof(Telepad), "OnSpawn")]
    public static class MechanicalAscensionTelepadPatch
    {
        public static void Postfix(Telepad __instance)
        {
            if (__instance == null)
            {
                return;
            }

            try
            {
                MechanicalAscensionStation station =
                    __instance.gameObject.AddOrGet<MechanicalAscensionStation>();
                station.Initialize(__instance);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 初始化打印舱机械飞升失败：" + exception);
            }
        }
    }
}
