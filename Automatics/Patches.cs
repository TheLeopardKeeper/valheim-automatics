﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;
using ModUtils;

namespace Automatics
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Awake")]
        private static void Player_Awake_Postfix(Player __instance)
        {
            if (Objects.GetZNetView(__instance, out var zNetView))
                Hooks.OnPlayerAwake?.Invoke(__instance, zNetView);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), "Update")]
        private static IEnumerable<CodeInstruction> Player_Update_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codeMatcher = new CodeMatcher(instructions, generator)
                .End()
                .MatchStartBackwards(new CodeMatch(OpCodes.Call,
                    AccessTools.Method(typeof(Player), "UpdatePlacement")))
                .MatchStartBackwards(new CodeMatch(OpCodes.Ldarg_0));

            var originalLabels = new List<Label>(codeMatcher.Labels);
            codeMatcher.Labels.Clear();

            return codeMatcher
                .CreateLabel(out var skip)
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.Method(typeof(Action<Player, bool>), "Invoke")))
                .CreateLabel(out var exec)
                .MatchStartBackwards(new CodeMatch(OpCodes.Ldarg_0))
                .Insert(
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Hooks), "get_OnPlayerUpdate")),
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Brtrue_S, exec),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Br_S, skip))
                .AddLabels(originalLabels)
                .InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        private static IEnumerable<CodeInstruction> Player_FixedUpdate_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            return new CodeMatcher(instructions, generator)
                .MatchStartForward(new CodeMatch(OpCodes.Call,
                    AccessTools.Method(typeof(Player), "UpdateStealth")))
                .Advance(1)
                .CreateLabel(out var skip)
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.Method(typeof(Action<Player, float>), "Invoke")))
                .CreateLabel(out var exec)
                .MatchStartBackwards(new CodeMatch(OpCodes.Ldarg_0))
                .Insert(
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Hooks), "get_OnPlayerFixedUpdate")),
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Brtrue_S, exec),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Br_S, skip))
                .InstructionEnumeration();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static void Terminal_InitTerminal_Prefix(out bool __state,
            bool ___m_terminalInitialized)
        {
            __state = ___m_terminalInitialized;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static void Terminal_InitTerminal_Postfix(bool __state)
        {
            if (!__state)
                Hooks.OnInitTerminal?.Invoke();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Container), "Awake")]
        private static void Container_Awake_Postfix(Container __instance, ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() != null)
                __instance.gameObject.AddComponent<ContainerCache>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Pickable), "Awake")]
        private static void Pickable_Awake_Postfix(Pickable __instance, ZNetView ___m_nview)
        {
            if (___m_nview.GetZDO() != null)
                __instance.gameObject.AddComponent<PickableCache>();
        }
    }
}