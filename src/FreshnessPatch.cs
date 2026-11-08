using HarmonyLib;

namespace ONIStarter
{
    /// <summary>
    /// Food and rot always show 100% freshness.
    /// The Rottable state machine reads RotValue every second and uses it to
    /// decide between Fresh, Stale and Spoiled. This patch always returns the
    /// full spoil time and refills the value after each rot tick.
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
                // Refill the internal amount as well so tooltips and stack
                // splitting/merging read the real full value.
                __instance.RotValue = __instance.def.spoilTime;
            }
        }
    }
}
