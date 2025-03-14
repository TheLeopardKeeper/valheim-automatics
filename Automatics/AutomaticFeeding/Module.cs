﻿using HarmonyLib;

namespace Automatics.AutomaticFeeding
{
    internal static class Module
    {
        [AutomaticsInitializer(4)]
        private static void Initialize()
        {
            Config.Initialize();
            if (Config.IsModuleDisabled) return;

            Harmony.CreateAndPatchAll(typeof(Patches),
                Automatics.GetHarmonyId("automatic-feeding"));
        }
    }
}