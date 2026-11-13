using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ONIStarter
{
    /// <summary>
    /// Shatters regular meteors as soon as they enter the world, mirroring the
    /// debris behaviour of a meteor-blaster interception. Only <see cref="Comet"/>
    /// is patched, so the DLC4 calamity asteroid (<c>LargeComet</c>), the
    /// geothermal-vent mini comets (<c>MiniComet</c>), deprecated event props,
    /// and Moo meteors keep their vanilla behaviour.
    /// </summary>
    [HarmonyPatch(typeof(Comet), nameof(Comet.Sim33ms))]
    public static class CometAutoShatterPatch
    {
        private const float DebrisMassModifier = 0.25f;
        private const float DebrisSpeed = 6f;
        private const float DebrisMaxAngle = 40f;

        private static readonly FieldInfo HasExplodedField = typeof(Comet).GetField(
            "hasExploded",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Prefix(Comet __instance)
        {
            if (!StarterSettings.AutoShatterMeteors || __instance == null)
            {
                return true;
            }

            try
            {
                // Match the vanilla meteor blaster defaults: deprecated event
                // props and Moo meteors (which deliver a live Gassy Moo) are
                // left alone.
                if (__instance.HasTag(GameTags.DeprecatedContent) ||
                    __instance.GetComponent<GassyMooComet>() != null ||
                    !HasEnteredWorld(__instance) ||
                    HasExploded(__instance))
                {
                    return true;
                }

                Shatter(__instance);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 自动破碎流星失败：" + exception);
            }

            // The comet has either been destroyed or will be destroyed by
            // Shatter's cleanup; never let the vanilla impact logic run.
            return false;
        }

        private static bool HasEnteredWorld(Comet comet)
        {
            int worldId = comet.GetMyWorldId();
            if (worldId == 255 || worldId < 0)
            {
                return false;
            }

            int cell = Grid.PosToCell(comet.TargetPosition);
            return Grid.IsValidCellInWorld(cell, worldId);
        }

        private static bool HasExploded(Comet comet)
        {
            if (HasExplodedField == null)
            {
                return false;
            }

            object value = HasExplodedField.GetValue(comet);
            return value is bool exploded && exploded;
        }

        private static void Shatter(Comet comet)
        {
            try
            {
                Vector3 origin = comet.TargetPosition;
                SpawnDebrisFields(comet, origin);
                SpawnExplosionEffect(origin);
            }
            finally
            {
                Util.KDestroyGameObject(comet.gameObject);
            }
        }

        private static void SpawnDebrisFields(Comet comet, Vector3 origin)
        {
            PrimaryElement primaryElement = comet.GetComponent<PrimaryElement>();
            if (primaryElement == null)
            {
                return;
            }

            int worldId = comet.GetMyWorldId();
            int originCell = Grid.PosToCell(origin);
            if (worldId == 255 || worldId < 0 || !Grid.IsValidCellInWorld(originCell, worldId))
            {
                return;
            }

            Substance substance = primaryElement.Element.substance;
            int oreCount = comet.GetRandomNumOres();
            int tileCount = comet.addTiles;
            string[] loot = comet.lootOnDestroyedByMissile;
            int lootCount = loot != null ? loot.Length : 0;
            int debrisCount = oreCount + tileCount + lootCount;

            float oreMassEach = oreCount > 0
                ? comet.ExplosionMass * DebrisMassModifier / oreCount
                : 0f;
            float tileMassEach = tileCount > 0
                ? comet.AddTileMass * DebrisMassModifier / tileCount
                : 0f;
            float temperature = comet.GetRandomTemperatureForOres();
            int diseaseCountEach = oreCount + tileCount > 0
                ? primaryElement.DiseaseCount / (oreCount + tileCount)
                : 0;

            int debrisIndex = 0;
            for (int i = 0; i < oreCount; i++, debrisIndex++)
            {
                SpawnResource(
                    substance,
                    origin,
                    GetDebrisVelocity(debrisIndex, debrisCount),
                    oreMassEach,
                    temperature,
                    primaryElement.DiseaseIdx,
                    diseaseCountEach);
            }

            for (int i = 0; i < tileCount; i++, debrisIndex++)
            {
                SpawnResource(
                    substance,
                    origin,
                    GetDebrisVelocity(debrisIndex, debrisCount),
                    tileMassEach,
                    temperature,
                    primaryElement.DiseaseIdx,
                    diseaseCountEach);
            }

            for (int i = 0; i < lootCount; i++, debrisIndex++)
            {
                SpawnLoot(
                    origin,
                    loot[i],
                    GetDebrisVelocity(debrisIndex, debrisCount));
            }
        }

        /// <summary>
        /// Same fan pattern used by <c>MissileProjectile.SpawnMeteorResources</c>:
        /// debris is thrown downward with alternating angles up to 40 degrees.
        /// </summary>
        private static Vector3 GetDebrisVelocity(int index, int count)
        {
            int evenCount = count % 2 == 0 ? count : count - 1;
            if (evenCount <= 0)
            {
                return Vector3.down * DebrisSpeed;
            }

            float step = DebrisMaxAngle * 2f / evenCount;
            bool positive = index % 2 == 0;
            float offset = step
                * Mathf.CeilToInt(index / 2f)
                * (Mathf.PI / 180f)
                * (positive ? 1f : -1f);
            float angle = 3f * Mathf.PI / 2f + offset;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized
                * DebrisSpeed;
        }

        private static void SpawnResource(
            Substance substance,
            Vector3 origin,
            Vector3 velocity,
            float mass,
            float temperature,
            byte diseaseIndex,
            int diseaseCount)
        {
            if (substance == null || mass <= 0f)
            {
                return;
            }

            GameObject debris = substance.SpawnResource(
                GetSpawnPosition(origin, velocity),
                mass,
                temperature,
                diseaseIndex,
                diseaseCount);
            AddFaller(debris, velocity);
        }

        private static void SpawnLoot(Vector3 origin, string prefabName, Vector3 velocity)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }

            Vector3 position = GetSpawnPosition(origin, velocity);
            int cell = Grid.PosToCell(position);
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            GameObject loot = Scenario.SpawnPrefab(cell, 0, 0, prefabName);
            if (loot == null)
            {
                return;
            }

            loot.SetActive(true);
            position.z = loot.transform.position.z;
            loot.transform.position = position;
            AddFaller(loot, velocity);
        }

        private static Vector3 GetSpawnPosition(Vector3 origin, Vector3 velocity)
        {
            return origin
                + velocity.normalized * 0.75f
                + new Vector3(0f, 0.55f, 0f);
        }

        private static void AddFaller(GameObject item, Vector3 velocity)
        {
            if (item == null)
            {
                return;
            }

            if (GameComps.Fallers.Has(item))
            {
                GameComps.Fallers.Remove(item);
            }

            GameComps.Fallers.Add(item, new Vector2(velocity.x, velocity.y));
        }

        private static void SpawnExplosionEffect(Vector3 origin)
        {
            try
            {
                Game.Instance.SpawnFX(SpawnFXHashes.MissileExplosion, origin, 0f);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ONI-Starter] 流星破碎特效播放失败：" + exception);
            }
        }
    }
}
