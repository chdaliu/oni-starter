using HarmonyLib;

namespace ONIStarter
{
    /// <summary>
    /// 食物/腐烂物始终显示为 100% 新鲜。
    /// Rottable 的状态机每秒读取 RotValue 并据此决定 Fresh/Stale/Spoiled；
    /// 这里让读取值始终等于该食物的完整保质期，并在每次腐烂结算后把数值补满。
    /// </summary>
    [HarmonyPatch(typeof(Rottable.Instance), "get_RotValue")]
    public static class RottableFreshnessGetterPatch
    {
        public static void Postfix(Rottable.Instance __instance, ref float __result)
        {
            if (__instance == null || __instance.def == null)
            {
                return;
            }

            if (!StarterSettings.FoodFresh)
            {
                return;
            }

            if (__instance.def.spoilTime > 0f)
            {
                __result = __instance.def.spoilTime;
            }
        }
    }

    [HarmonyPatch(typeof(Rottable.Instance), "Rot")]
    public static class RottableFreshnessTickPatch
    {
        public static void Postfix(Rottable.Instance __instance)
        {
            if (__instance == null || __instance.def == null)
            {
                return;
            }

            if (!StarterSettings.FoodFresh)
            {
                return;
            }

            if (__instance.def.spoilTime > 0f)
            {
                // 同步把内部 Amount 也补满，保证提示框、拆分/合并等读取到真实满值。
                __instance.RotValue = __instance.def.spoilTime;
            }
        }
    }
}
