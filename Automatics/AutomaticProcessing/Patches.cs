﻿using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace Automatics.AutomaticProcessing
{
    [SuppressMessage( "ReSharper", "InconsistentNaming" )]
    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Beehive), "IncreseLevel")]
        private static bool BeehiveIncreseLevelPrefix(Beehive __instance, ZNetView ___m_nview, int i)
        {
            if (!Config.AutomaticProcessingEnabled) return true;
            return AutomaticProcessing.AutomaticStore(__instance, ___m_nview, i);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        private static void CookingStationUpdateCookingPostfix(CookingStation __instance, ZNetView ___m_nview)
        {
            if (!Config.AutomaticProcessingEnabled) return;

            AutomaticProcessing.AutomaticRefuel(__instance, ___m_nview);
            AutomaticProcessing.AutomaticStore(__instance, ___m_nview);
            AutomaticProcessing.AutomaticCraft(__instance, ___m_nview);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CraftingStation), "FixedUpdate")]
        private static void CraftingStationFixedUpdatePostfix(CraftingStation __instance, ZNetView ___m_nview)
        {
            if (!Config.AutomaticProcessingEnabled) return;

            AutomaticProcessing.AutomaticCraft(__instance, ___m_nview);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Fermenter), "SlowUpdate")]
        private static void FermenterSlowUpdatePostfix(Fermenter __instance, ZNetView ___m_nview)
        {
            if (!Config.AutomaticProcessingEnabled) return;

            AutomaticProcessing.AutomaticStore(__instance, ___m_nview);
            AutomaticProcessing.AutomaticCraft(__instance, ___m_nview);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
        private static void FireplaceUpdateFireplacePostfix(Fireplace __instance, Piece ___m_piece, ZNetView ___m_nview)
        {
            if (!Config.AutomaticProcessingEnabled) return;

            AutomaticProcessing.AutomaticRefuel(__instance, ___m_piece, ___m_nview);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
        private static void SmelterUpdateSmelterPostfix(Smelter __instance, ZNetView ___m_nview)
        {
            if (!Config.AutomaticProcessingEnabled) return;

            AutomaticProcessing.AutomaticRefuel(__instance, ___m_nview);
            AutomaticProcessing.AutomaticCraft(__instance, ___m_nview);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Smelter), "Spawn")]
        private static bool SmelterSpawnPrefix(Smelter __instance, string ore, int stack)
        {
            if (!Config.AutomaticProcessingEnabled) return true;
            return AutomaticProcessing.AutomaticStore(__instance, ore, stack);
        }
    }
}