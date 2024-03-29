using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.extendedcheats";
        public const string Name = "ExtendedCheats";
        public const string Version = "1.3";

        public static ConfigEntry<bool> configEnabled;
        public static ConfigEntry<bool> configInfiniteAmmo;

        public static ConfigEntry<bool> configEnableCheats;
        public static ConfigEntry<bool> configEnableDev;
        public static ConfigEntry<bool> configEnableDevExtra;

        public static ConfigEntry<ToggleableFloat> configCorpseKnockoutRange;
        public static ConfigEntry<bool> configMultiKnockOut;

        internal static Overrides overrides;

        public static Harmony harmony;
        public static bool isMultiKnockoutPatched = false;

        static GUIStyle _skinBold;
        public static GUIStyle SkinBold {
            get {
                if (_skinBold == null)
                {
                    _skinBold = new GUIStyle(GUI.skin.label)
                    {
                        name = "D3.ExtendedCheats Bold",
                        fontStyle = FontStyle.Bold
                    };
                }
                return _skinBold;
            }
        }

        static GUIStyle _skinFloatField;
        public static GUIStyle SkinFloatField {
            get {
                if (_skinFloatField == null)
                {
                    _skinFloatField = new GUIStyle(GUI.skin.textField)
                    {
                        name = "D3.ExtendedCheats Float TextField",
                        alignment = TextAnchor.MiddleRight
                    };
                }
                return _skinFloatField;
            }
        }

        internal static ManualLogSource StaticLogger;

        void Awake()
        {
            StaticLogger = Logger;

            TomlTypeConverter.AddConverter(typeof(ToggleableFloat), new TypeConverter
            {
                ConvertToObject = (str, type) =>
                {
                    var match = Regex.Match(str, @"\s*\[(false|true),\s*([\d.eE-])+", RegexOptions.IgnoreCase);

                    if (!match.Success) throw new Exception("Invalid format. Expected '[bool, float]'");

                    var enabled = match.Groups[1].Value.ToLower() == "true";

                    return new ToggleableFloat(enabled, float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
                },
                ConvertToString = (obj, type) =>
                {
                    var val = (ToggleableFloat)obj;
                    return "[" + val.enabled + ", " + val.value.ToString(CultureInfo.InvariantCulture) + "]";
                }
            });

            configEnabled = Config.Bind("General", "Enabled", true, new ConfigDescription("Enable/Disable all Cheats at once", null, new ConfigurationManagerAttributes { Category = "", Order = 100 }));

            configInfiniteAmmo = Config.Bind("General", "Infinite Ammo", false, new ConfigDescription("Infinite Ammo", null, new ConfigurationManagerAttributes { Category = "", Order = 99 }));

            configEnableCheats = Config.Bind("1. Cheats and Dev options", "Enable Cheats", false);
            configEnableDev = Config.Bind("1. Cheats and Dev options", "Enable Dev Options", false);
            configEnableDevExtra = Config.Bind("1. Cheats and Dev options", "Enable Extra Dev Options", false);

            configCorpseKnockoutRange = Config.BindToggleableFloat("3. Corpse Throwing", "Knockout Range", new ToggleableFloat(false, 2));
            configMultiKnockOut = Config.Bind("3. Corpse Throwing", "Allow throws to knockout multiple enemies", false);

            Commands.Bind(Config);

            overrides = new Overrides(Config);

            harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

            OnSettingsChanged();

            Config.SettingChanged += (_, __) => OnSettingsChanged();
        }

        static void OnSettingsChanged()
        {
            PatchMultiKnockout();
            GlobalSettings.instance.bEnableCheats = configEnabled.Value && configEnableCheats.Value;
            GlobalSettings.instance.bDevOptions = configEnabled.Value && configEnableDev.Value;
            GlobalSettings.instance.bDevOptionsExtra = configEnabled.Value && configEnableDevExtra.Value;
        }

        static void PatchMultiKnockout()
        {
            var original = AccessTools.Method(typeof(MiCharacter), "checkKnockoutCharInRange");
            var transpiler = typeof(Hooks).GetMethod("CheckKnockoutCharInRangeTranspiler");

            if (configEnabled.Value && configMultiKnockOut.Value)
            {
                if (!isMultiKnockoutPatched)
                {
                    harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
                    isMultiKnockoutPatched = true;
                }
            }
            else if (isMultiKnockoutPatched)
            {
                harmony.Unpatch(original, transpiler);
                isMultiKnockoutPatched = false;
            }
        }
    }

    class Hooks
    {
        public static IEnumerable<CodeInstruction> CheckKnockoutCharInRangeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var foundReturn = false;
            foreach (var instr in instructions)
            {
                if (!foundReturn && instr.opcode == OpCodes.Ret) foundReturn = true;
                else yield return instr;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MiCharacterInventory), "HasItem")]
        internal static bool HasItem(MiCharacterInventory.ItemType _itemType, ref bool __result)
        {
            if (Main.configEnabled.Value && Main.configInfiniteAmmo.Value)
            {
                switch (_itemType)
                {
                    case MiCharacterInventory.ItemType.DamageGrenade:
                    case MiCharacterInventory.ItemType.KnockoutGrenade:
                    case MiCharacterInventory.ItemType.SnipeAmmo:
                    case MiCharacterInventory.ItemType.GunAmmo:
                    case MiCharacterInventory.ItemType.ControlDart:
                        __result = true;
                        return false;
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MiCharacterInventory), "Count")]
        internal static bool ItemCount(MiCharacterInventory.ItemType _itemType, ref uint __result)
        {
            if (Main.configEnabled.Value && Main.configInfiniteAmmo.Value)
            {
                switch (_itemType)
                {
                    case MiCharacterInventory.ItemType.DamageGrenade:
                    case MiCharacterInventory.ItemType.KnockoutGrenade:
                    case MiCharacterInventory.ItemType.SnipeAmmo:
                    case MiCharacterInventory.ItemType.GunAmmo:
                    case MiCharacterInventory.ItemType.ControlDart:
                        __result = 2;
                        return false;
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MiCharacter), "checkKnockoutTarget")]
        internal static void CheckKnockoutTargetPrefix(ref MiCharacter _charTarget, ref float _fMaxRange)
        {
            if (Main.configEnabled.Value)
            {
                Main.configCorpseKnockoutRange.SetIfEnabled(ref _fMaxRange);

                if (Main.configMultiKnockOut.Value)
                    _charTarget = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIManager), "onGameplayHUDUIInitEndOfFrame")]
        internal static void UIManager_onGameplayHUDUIInitEndOfFrame()
        {
            if (Main.configEnabled.Value) Main.overrides.OnLevelLoad();
        }
    }

    public struct ToggleableFloat
    {
        public bool enabled;
        public float value;

        public ToggleableFloat(bool initialEnabled, float initialValue)
        {
            enabled = initialEnabled;
            value = initialValue;
        }

        public bool SetIfEnabled(ref float value)
        {
            if (enabled) value = this.value;
            return enabled;
        }

        internal static void Draw(ConfigEntryBase entry)
        {
            var self = (ToggleableFloat)entry.BoxedValue;
            if (self.enabled = GUILayout.Toggle(self.enabled, self.enabled ? "Overwrite" : "Disabled"))
            {
                var str = GUILayout.TextField(self.value.ToString("f2", CultureInfo.InvariantCulture));
                if (string.IsNullOrEmpty(str))
                    self.value = 0;
                else
                {
                    if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                    {
                        self.value = num;
                    }
                    else
                    {
                        self.value = 0;
                    }
                }
            }
            entry.BoxedValue = self;
        }
    }
}
