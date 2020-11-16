using BepInEx;
using BepInEx.Configuration;
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
        public const string Version = "1.0";

        public static ConfigEntry<bool> configEnabled;
        public static ConfigEntry<bool> configInfiniteAmmo;

        public static ConfigEntry<bool> configEnableCheats;
        public static ConfigEntry<bool> configEnableDev;
        public static ConfigEntry<bool> configEnableDevExtra;

        public static ConfigEntry<ToggleableFloat> configCorpseThrowRange;
        public static ConfigEntry<ToggleableFloat> configCorpseKnockoutRange;
        public static ConfigEntry<bool> configMultiKnockOut;

        public static AbilityModifiers configAbilityModifiers;

        public static Harmony harmony;
        public static bool isMultiKnockoutPatched = false;

        public static GUIStyle headingStyle;

        public void Awake()
        {
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

            configCorpseThrowRange = Config.BindToggleableFloat("3. Corpse Throwing", "Throw Range", new ToggleableFloat(false, 5));
            configCorpseKnockoutRange = Config.BindToggleableFloat("3. Corpse Throwing", "Knockout Range", new ToggleableFloat(false, 2));
            configMultiKnockOut = Config.Bind("3. Corpse Throwing", "Allow throws to knockout multiple enemies", false);

            configAbilityModifiers = new AbilityModifiers(Config);

            Commands.Bind(Config);

            harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

            OnSettingsChanged();

            Config.SettingChanged += (_, __) => OnSettingsChanged();
        }

        void OnGUI()
        {
            if (headingStyle != null) return;

            headingStyle = new GUIStyle(GUI.skin.label);
            headingStyle.name = "D3.ExtendedCheats Heading";
            headingStyle.fontStyle = FontStyle.Bold;
        }

        static void OnSettingsChanged()
        {
            PatchMultiKnockout();
            MiSingletonScriptableObject<GlobalSettings>.instance.bEnableCheats = configEnabled.Value && configEnableCheats.Value;
            MiSingletonScriptableObject<GlobalSettings>.instance.bDevOptions = configEnabled.Value && configEnableDev.Value;
            MiSingletonScriptableObject<GlobalSettings>.instance.bDevOptionsExtra = configEnabled.Value && configEnableDevExtra.Value;
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillData), "fRange", MethodType.Getter)]
        internal static bool AbilityRange(SkillData __instance, ref float __result)
        {
            if (Main.configEnabled.Value)
            {
                switch (__instance.name)
                {
                    case "SkillCpyKeyThrowKnife": return !Main.configAbilityModifiers.cpyThrowKnifeRange.SetIfEnabled(ref __result);

                    case "SkillCopKeyThrowKnife": return !Main.configAbilityModifiers.copThrowKnifeRange.SetIfEnabled(ref __result);
                    case "SkillCopKeyWhistleStone": return !Main.configAbilityModifiers.copWhistleStoneRange.SetIfEnabled(ref __result);
                    case "SkillCopKeyGunLeft": return !Main.configAbilityModifiers.copGunLeftRange.SetIfEnabled(ref __result);
                    case "SkillCopKeyGunRight": return !Main.configAbilityModifiers.copGunRightRange.SetIfEnabled(ref __result);

                    case "SkillMccKeyGun": return !Main.configAbilityModifiers.mccGunRange.SetIfEnabled(ref __result);
                    case "SkillMccKeyStunbox": return !Main.configAbilityModifiers.mccStunboxRange.SetIfEnabled(ref __result);
                    case "SkillMccKeyStunGrenade": return !Main.configAbilityModifiers.mccStunGrenadeRange.SetIfEnabled(ref __result);

                    case "SkillTraKeyGun": return !Main.configAbilityModifiers.traGunRange.SetIfEnabled(ref __result);

                    case "SkillKatKeyGun": return !Main.configAbilityModifiers.katGunRange.SetIfEnabled(ref __result);
                    case "SkillKatKeyBlind": return !Main.configAbilityModifiers.katBlindRange.SetIfEnabled(ref __result);

                    case "SkillVooKeyControl": return !Main.configAbilityModifiers.vooControlRange.SetIfEnabled(ref __result);
                    case "SkillVooKeyConnect": return !Main.configAbilityModifiers.vooConnectRange.SetIfEnabled(ref __result);
                    case "SkillVooKeyPet": return !Main.configAbilityModifiers.vooPetRange.SetIfEnabled(ref __result);

                    case "SkillCarryThrow": return !Main.configCorpseThrowRange.SetIfEnabled(ref __result);
                }
            }
            return true;
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

    static class Extensions
    {
        internal static ConfigEntry<ToggleableFloat> BindToggleableFloat(this ConfigFile config, string category, string description, ToggleableFloat initial)
        {
            return config.Bind(category, description, initial,
                new ConfigDescription(description, null,
                    new ConfigurationManagerAttributes
                    {
                        CustomDrawer = ToggleableFloat.Draw,
                        DefaultValue = initial,
                    }
                )
            );
        }

        internal static bool SetIfEnabled(this ConfigEntry<ToggleableFloat> entry, ref float value)
        {
            return entry.Value.SetIfEnabled(ref value);
        }
    }

    public class AbilityModifiers
    {
        public ConfigEntry<ToggleableFloat> cpyThrowKnifeRange;
        // public ConfigEntry<ToggleableFloat> cpyThrowKnifeNoise;

        public ConfigEntry<ToggleableFloat> copThrowKnifeRange;
        public ConfigEntry<ToggleableFloat> copWhistleStoneRange;
        // public ConfigEntry<ToggleableFloat> copWhistleStoneNoise;
        public ConfigEntry<ToggleableFloat> copGunLeftRange;
        // public ConfigEntry<ToggleableFloat> copGunLeftNoise;
        public ConfigEntry<ToggleableFloat> copGunRightRange;
        // public ConfigEntry<ToggleableFloat> copGunRightNoise;

        public ConfigEntry<ToggleableFloat> mccGunRange;
        // public ConfigEntry<ToggleableFloat> mccGunNoise;
        public ConfigEntry<ToggleableFloat> mccStunboxRange;
        public ConfigEntry<ToggleableFloat> mccStunGrenadeRange;

        public ConfigEntry<ToggleableFloat> traGunRange;
        // public ConfigEntry<ToggleableFloat> traGunNoise;

        public ConfigEntry<ToggleableFloat> katGunRange;
        // public ConfigEntry<ToggleableFloat> katGunNoise;
        public ConfigEntry<ToggleableFloat> katBlindRange;

        public ConfigEntry<ToggleableFloat> vooControlRange;
        public ConfigEntry<ToggleableFloat> vooConnectRange;
        public ConfigEntry<ToggleableFloat> vooPetRange;

        public AbilityModifiers(ConfigFile config)
        {
            cpyThrowKnifeRange = config.BindToggleableFloat("4. Skills", "Cooper Young Throw Knife Range", new ToggleableFloat(false, 8));

            copThrowKnifeRange = config.BindToggleableFloat("4. Skills", "Cooper Knife Throw Range", new ToggleableFloat(false, 12));
            copWhistleStoneRange = config.BindToggleableFloat("4. Skills", "Cooper Coin Range", new ToggleableFloat(false, 12));
            copGunLeftRange = config.BindToggleableFloat("4. Skills", "Cooper Left Gun Range", new ToggleableFloat(false, 17));
            copGunRightRange = config.BindToggleableFloat("4. Skills", "Cooper Right Gun Range", new ToggleableFloat(false, 17));

            mccGunRange = config.BindToggleableFloat("4. Skills", "McCoy Gun Range", new ToggleableFloat(false, 55));
            mccStunboxRange = config.BindToggleableFloat("4. Skills", "McCoy Bag Range", new ToggleableFloat(false, 5));
            mccStunGrenadeRange = config.BindToggleableFloat("4. Skills", "McCoy Gas Range", new ToggleableFloat(false, 8));

            traGunRange = config.BindToggleableFloat("4. Skills", "Hector Gun Range", new ToggleableFloat(false, 12));

            katGunRange = config.BindToggleableFloat("4. Skills", "Kate Gun Range", new ToggleableFloat(false, 9));
            katBlindRange = config.BindToggleableFloat("4. Skills", "Kate Perfume Range", new ToggleableFloat(false, 12));

            vooControlRange = config.BindToggleableFloat("4. Skills", "Isabelle Mind Control Range", new ToggleableFloat(false, 10));
            vooConnectRange = config.BindToggleableFloat("4. Skills", "Isabelle Connect Range", new ToggleableFloat(false, 10));
            vooPetRange = config.BindToggleableFloat("4. Skills", "Isabelle Cat Range", new ToggleableFloat(false, 14));
        }
    }
}
