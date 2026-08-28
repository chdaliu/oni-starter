using HarmonyLib;
using KMod;
using UnityEngine;

namespace ONIStarter
{
    public sealed class ONIStarterMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            StarterSettings.Load();
            base.OnLoad(harmony);
            Debug.Log("[ONI-Starter] 新手套装已加载。");
        }
    }
}
