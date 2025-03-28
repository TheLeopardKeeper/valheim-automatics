﻿using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ModUtils;

namespace Automatics.AutomaticProcessing
{
    using AcceptableProcess = AcceptableValueEnum<Process>;

    internal static class Config
    {
        private const string Section = "automatic_processing";

        private static ConfigEntry<bool> _moduleDisable;
        private static ConfigEntry<bool> _enableAutomaticProcessing;
        private static Dictionary<string, ConfigEntry<Process>> _allowProcessing;
        private static Dictionary<string, ConfigEntry<int>> _containerSearchRange;
        private static Dictionary<string, ConfigEntry<int>> _materialCountOfSuppressProcessing;
        private static Dictionary<string, ConfigEntry<bool>> _supplyOnlyWhenMaterialsRunOut;
        private static Dictionary<string, ConfigEntry<int>> _fuelCountOfSuppressProcessing;
        private static Dictionary<string, ConfigEntry<int>> _productStacksOfSuppressProcessing;
        private static Dictionary<string, ConfigEntry<bool>> _refuelOnlyWhenMaterialsSupplied;
        private static Dictionary<string, ConfigEntry<bool>> _refuelOnlyWhenOutOfFuel;

        public static bool IsModuleDisabled => _moduleDisable.Value;
        public static bool EnableAutomaticProcessing => _enableAutomaticProcessing.Value;

        public static Process AllowProcessing(string processor)
        {
            return _allowProcessing.TryGetValue(processor, out var entry)
                ? entry.Value
                : Process.None;
        }

        public static int ContainerSearchRange(string processor)
        {
            return _containerSearchRange.TryGetValue(processor, out var entry) ? entry.Value : 0;
        }

        public static int MaterialCountOfSuppressProcessing(string processor)
        {
            return _materialCountOfSuppressProcessing.TryGetValue(processor, out var entry)
                ? entry.Value
                : 0;
        }

        public static bool SupplyOnlyWhenMaterialsRunOut(string processor)
        {
            return _supplyOnlyWhenMaterialsRunOut.TryGetValue(processor, out var entry) &&
                   entry.Value;
        }

        public static int FuelCountOfSuppressProcessing(string processor)
        {
            return _fuelCountOfSuppressProcessing.TryGetValue(processor, out var entry)
                ? entry.Value
                : 0;
        }

        public static int ProductStacksOfSuppressProcessing(string processor)
        {
            return _productStacksOfSuppressProcessing.TryGetValue(processor, out var entry)
                ? entry.Value
                : 0;
        }

        public static bool RefuelOnlyWhenMaterialsSupplied(string processor)
        {
            return _refuelOnlyWhenMaterialsSupplied.TryGetValue(processor, out var entry) &&
                   entry.Value;
        }

        public static bool RefuelOnlyWhenOutOfFuel(string processor)
        {
            return _refuelOnlyWhenOutOfFuel.TryGetValue(processor, out var entry) && entry.Value;
        }

        public static void Initialize()
        {
            var config = global::Automatics.Config.Instance;
            config.ChangeSection(Section);
            _moduleDisable = config.Bind("module_disable", false, initializer: x =>
            {
                x.DispName = Automatics.L10N.Translate("@config_common_disable_module_name");
                x.Description =
                    Automatics.L10N.Translate("@config_common_disable_module_description");
            });
            if (_moduleDisable.Value) return;

            _enableAutomaticProcessing = config.Bind("enable_automatic_processing", true);

            _allowProcessing = new Dictionary<string, ConfigEntry<Process>>();
            _containerSearchRange = new Dictionary<string, ConfigEntry<int>>();
            _materialCountOfSuppressProcessing = new Dictionary<string, ConfigEntry<int>>();
            _supplyOnlyWhenMaterialsRunOut = new Dictionary<string, ConfigEntry<bool>>();
            _fuelCountOfSuppressProcessing = new Dictionary<string, ConfigEntry<int>>();
            _productStacksOfSuppressProcessing = new Dictionary<string, ConfigEntry<int>>();
            _refuelOnlyWhenMaterialsSupplied = new Dictionary<string, ConfigEntry<bool>>();
            _refuelOnlyWhenOutOfFuel = new Dictionary<string, ConfigEntry<bool>>();

            foreach (var processor in Processor.GetAllInstance())
            {
                var processorName = processor.name;
                var displayName = Automatics.L10N.Translate(processorName);
                var rawName = processorName.Substring(1);
                var defaultAllowedProcesses = processor.defaultAllowedProcesses;
                var processes = processor.processes.ToArray();

                var key = $"allow_processing_by_{rawName}";
                _allowProcessing[processorName] =
                    config.Bind(key, defaultAllowedProcesses, new AcceptableProcess(processes),
                        Initializer("allow_processing_by", displayName));

                key = $"container_search_range_by_{rawName}";
                _containerSearchRange[processorName] =
                    config.Bind(key, 8, (1, 64),
                        Initializer("container_search_range_by", displayName));

                if (processes.Contains(Process.Craft))
                {
                    key = $"{rawName}_material_count_of_suppress_processing";
                    _materialCountOfSuppressProcessing[processorName] =
                        config.Bind(key, 1, (0, 999),
                            Initializer("material_count_of_suppress_processing", displayName));

                    key = $"{rawName}_product_stacks_of_suppress_processing";
                    _productStacksOfSuppressProcessing[processorName] =
                        config.Bind(key, 0, (0, 99),
                            Initializer("product_stacks_of_suppress_processing", displayName));

                    key = $"{rawName}_supply_only_when_materials_run_out";
                    _supplyOnlyWhenMaterialsRunOut[processorName] =
                        config.Bind(key, false,
                            initializer: Initializer("supply_only_when_materials_run_out", displayName));
                }

                if (processes.Contains(Process.Refuel))
                {
                    key = $"{rawName}_fuel_count_of_suppress_processing";
                    _fuelCountOfSuppressProcessing[processorName] =
                        config.Bind(key, 1, (0, 999),
                            Initializer("fuel_count_of_suppress_processing", displayName));

                    key = $"{rawName}_refuel_only_when_out_of_fuel";
                    _refuelOnlyWhenOutOfFuel[processorName] =
                        config.Bind(key, false,
                            initializer: Initializer("refuel_only_when_out_of_fuel", displayName));
                }

                if (processes.Contains(Process.Refuel) && processes.Contains(Process.Craft))
                {
                    key = $"{rawName}_refuel_only_when_materials_supplied";
                    _refuelOnlyWhenMaterialsSupplied[processorName] =
                        config.Bind(key, false,
                            initializer: Initializer("refuel_only_when_materials_supplied",
                                displayName));
                }
            }

            Action<ConfigurationManagerAttributes> Initializer(string key, string displayName)
            {
                return x =>
                {
                    x.DispName =
                        Automatics.L10N.Localize($"@config_automatic_processing_{key}_name",
                            displayName);
                    x.Description =
                        Automatics.L10N.Localize($"@config_automatic_processing_{key}_description",
                            displayName);
                };
            }
        }
    }
}