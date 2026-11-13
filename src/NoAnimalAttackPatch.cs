using System.Collections.Generic;
using HarmonyLib;

namespace ONIStarter
{
    /// <summary>
    /// Stops animals from proactively selecting standard or bionic duplicants
    /// as attack targets. Retaliation after a duplicant attacks an animal is
    /// intentionally left vanilla, as is the Sap Tree (Experiment 52B), which
    /// is a plant rather than an animal.
    /// </summary>
    [HarmonyPatch(typeof(ThreatMonitor.Instance), nameof(ThreatMonitor.Instance.PickBestTarget))]
    public static class NoAnimalAttackPatch
    {
        public static void Prefix(ThreatMonitor.Instance __instance, List<FactionAlignment> threats)
        {
            if (!StarterSettings.NoAnimalAttacks ||
                __instance == null ||
                threats == null ||
                __instance.alignment == null ||
                __instance.IAmADuplicant)
            {
                return;
            }

            for (int i = threats.Count - 1; i >= 0; i--)
            {
                FactionAlignment alignment = threats[i];
                if (alignment != null &&
                    alignment.Alignment == FactionManager.FactionID.Duplicant)
                {
                    threats.RemoveAt(i);
                }
            }
        }
    }
}
