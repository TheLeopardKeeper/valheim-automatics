﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Automatics.AutomaticMapping
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fish), "Start")]
        private static void Fish_Start_Postfix(Fish __instance, ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() != null)
                __instance.gameObject.AddComponent<FishCache>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RandomFlyingBird), "Start")]
        private static void RandomFlyingBird_Start_Postfix(RandomFlyingBird __instance,
            ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() != null)
                __instance.gameObject.AddComponent<BirdCache>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "Awake")]
        private static void Ship_Awake_Postfix(Ship __instance, ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() != null)
                __instance.gameObject.AddComponent<ShipCache>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), "Start")]
        private static void Minimap_Start_Postfix()
        {
            Map.Initialize();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), "UpdateMap")]
        private static void Minimap_UpdateMap_Postfix(Player player, float dt, bool takeInput)
        {
            AutomaticMapping.Mapping(player, dt, takeInput);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
        private static bool Minimap_OnMapLeftClick_Postfix()
        {
            return !AutomaticMapping.SetSaveFlag();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Minimap), "UpdateMap")]
        private static IEnumerable<CodeInstruction> Minimap_UpdateMap_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            return new CodeMatcher(instructions, generator)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldfld,
                        AccessTools.Field(typeof(Minimap.PinData), "m_checked")),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Ceq),
                    new CodeMatch(OpCodes.Stfld,
                        AccessTools.Field(typeof(Minimap.PinData), "m_checked")))
                .CreateLabel(out var originalCodes)
                .Advance(6)
                .CreateLabel(out var skipOriginalCodes)
                .MatchStartBackwards(
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldloc_S))
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_S, 11),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(AutomaticMapping), "SetSaveFlag",
                            new[] { typeof(Minimap.PinData) })),
                    new CodeInstruction(OpCodes.Brtrue_S, skipOriginalCodes),
                    new CodeInstruction(OpCodes.Br, originalCodes))
                .InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static IEnumerable<CodeInstruction> Minimap_UpdatePins_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            return new CodeMatcher(instructions, generator)
                .MatchEndForward(
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(typeof(Transform), "SetParent",
                            new[] { typeof(Transform) })),
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldfld,
                        AccessTools.Field(typeof(Minimap.PinData), "m_doubleSize")))
                .MatchStartForward(
                    new CodeMatch(OpCodes.Stloc_S))
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_S, 5),
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Map), "ResizeIcon")),
                    new CodeInstruction(OpCodes.Stloc_S, 10))
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldfld,
                        AccessTools.Field(typeof(Minimap.PinData), "m_nameElement")),
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(typeof(Component), "get_gameObject")),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(typeof(GameObject), "SetActive")))
                .CreateLabel(out var originalCodes)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Br))
                .CreateLabel(out var skipOriginalCodes)
                .MatchStartBackwards(
                    new CodeMatch(OpCodes.Bge_Un))
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_S, 5),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Map), "IsNameTagHidden")),
                    new CodeInstruction(OpCodes.Brfalse, originalCodes),
                    new CodeInstruction(OpCodes.Ldloc_S, 5),
                    new CodeInstruction(OpCodes.Ldfld,
                        AccessTools.Field(typeof(Minimap.PinData), "m_nameElement")),
                    new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.Method(typeof(Component), "get_gameObject")),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.Method(typeof(GameObject), "SetActive")),
                    new CodeInstruction(OpCodes.Br, skipOriginalCodes))
                .InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Pickable), "SetPicked")]
        private static IEnumerable<CodeInstruction> Pickable_SetPicked_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .End()
                .MatchStartBackwards(
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(typeof(ZNetView), "Destroy")))
                .MatchStartBackwards(
                    new CodeMatch(OpCodes.Ldarg_0))
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        AccessTools.Field(typeof(Pickable), "m_nview")),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(StaticObjectMapping), "OnObjectDestroy")))
                .InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MineRock5), "DamageArea")]
        private static IEnumerable<CodeInstruction> MineRock5_DamageArea_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .End()
                .MatchStartBackwards(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld,
                        AccessTools.Field(typeof(MineRock5), "m_nview")),
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(typeof(ZNetView), "Destroy")))
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        AccessTools.Field(typeof(MineRock5), "m_nview")),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(StaticObjectMapping), "OnObjectDestroy")))
                .InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Destructible), "Awake")]
        private static void Destructible_Awake_Postfix(Destructible __instance, ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() != null)
                __instance.m_onDestroyed += () =>
                    StaticObjectMapping.OnObjectDestroy(__instance, ___m_nview);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TeleportWorld), "Awake")]
        private static void TeleportWorld_Awake_Postfix(TeleportWorld __instance,
            ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() == null) return;

            var portal = __instance.GetComponent<WearNTear>();
            if (portal)
                portal.m_onDestroyed += () =>
                {
                    if (!Config.EnableAutomaticMapping) return;
                    if (!Config.AllowPinningPortal) return;
                    Map.RemovePin(__instance.transform.position);
                };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Vagon), "Awake")]
        private static void Vagon_Awake_Postfix(Vagon __instance, ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() == null) return;

            var wearNTear = __instance.GetComponent<WearNTear>();
            if (wearNTear)
                wearNTear.m_onDestroyed += () =>
                {
                    if (!Config.EnableAutomaticMapping) return;
                    if (!Config.AllowPinningPortal) return;
                    Map.RemovePin(__instance.transform.position);
                };
        }
    }
}