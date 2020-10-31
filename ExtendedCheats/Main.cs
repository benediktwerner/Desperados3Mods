using static UnityModManagerNet.UnityModManager;
using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace ExtendedCheats
{
    public class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static Harmony harmony;
        public static bool isMultiKnockoutPatched = false;

        public static void Load(ModEntry modEntry)
        {
            settings = ModSettings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            PatchMultiKnockout();

            MiSingletonScriptableObject<GlobalSettings>.instance.bEnableCheats = settings.enableCheats;
            MiSingletonScriptableObject<GlobalSettings>.instance.bDevOptions = settings.enableDev;
            MiSingletonScriptableObject<GlobalSettings>.instance.bDevOptionsExtra = settings.enableDevExtra;
        }

        static bool OnToggle(ModEntry modEntry, bool enabled)
        {
            Main.enabled = enabled;
            return true;
        }

        static void OnGUI(ModEntry modEntry) => settings.Draw(modEntry);
        static void OnSaveGUI(ModEntry modEntry)
        {
            settings.Save(modEntry);

            PatchMultiKnockout();

            MiSingletonScriptableObject<GlobalSettings>.instance.bEnableCheats = settings.enableCheats;
            MiSingletonScriptableObject<GlobalSettings>.instance.bDevOptions = settings.enableDev;
            MiSingletonScriptableObject<GlobalSettings>.instance.bDevOptionsExtra = settings.enableDevExtra;
        }

        static void PatchMultiKnockout()
        {
            var original = AccessTools.Method(typeof(MiCharacter), "checkKnockoutCharInRange");
            var transpiler = typeof(Patch).GetMethod("CheckKnockoutCharInRangeTranspiler");

            if (enabled && settings.multiKnockOut)
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

    [HarmonyPatch]
    class Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MiCharacterInventory), "HasItem")]
        internal static bool HasItem(MiCharacterInventory.ItemType _itemType, ref bool __result)
        {
            if (Main.enabled && Main.settings.infiniteAmmo)
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
            if (Main.enabled && Main.settings.infiniteAmmo)
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
            if (Main.enabled)
            {
                Main.settings.corpseKnockoutRange.SetIfEnabled(ref _fMaxRange);

                if (Main.settings.multiKnockOut)
                    _charTarget = null;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillData), "fRange", MethodType.Getter)]
        internal static bool AbilityRange(ref SkillData __instance, ref float __result)
        {
            if (Main.enabled)
            {
                switch (__instance.name)
                {
                    case "SkillCpyKeyThrowKnife": return !Main.settings.abilityModifiers.cpyThrowKnifeRange.SetIfEnabled(ref __result);

                    case "SkillCopKeyThrowKnife": return !Main.settings.abilityModifiers.copThrowKnifeRange.SetIfEnabled(ref __result);
                    case "SkillCopKeyWhistleStone": return !Main.settings.abilityModifiers.copWhistleStoneRange.SetIfEnabled(ref __result);
                    case "SkillCopKeyGunLeft": return !Main.settings.abilityModifiers.copGunLeftRange.SetIfEnabled(ref __result);
                    case "SkillCopKeyGunRight": return !Main.settings.abilityModifiers.copGunRightRange.SetIfEnabled(ref __result);

                    case "SkillMccKeyGun": return !Main.settings.abilityModifiers.mccGunRange.SetIfEnabled(ref __result);
                    case "SkillMccKeyStunbox": return !Main.settings.abilityModifiers.mccStunboxRange.SetIfEnabled(ref __result);
                    case "SkillMccKeyStunGrenade": return !Main.settings.abilityModifiers.mccStunGrenadeRange.SetIfEnabled(ref __result);

                    case "SkillTraKeyGun": return !Main.settings.abilityModifiers.traGunRange.SetIfEnabled(ref __result);

                    case "SkillKatKeyGun": return !Main.settings.abilityModifiers.katGunRange.SetIfEnabled(ref __result);
                    case "SkillKatKeyBlind": return !Main.settings.abilityModifiers.katBlindRange.SetIfEnabled(ref __result);

                    case "SkillVooKeyControl": return !Main.settings.abilityModifiers.vooControlRange.SetIfEnabled(ref __result);
                    case "SkillVooKeyConnect": return !Main.settings.abilityModifiers.vooConnectRange.SetIfEnabled(ref __result);
                    case "SkillVooKeyPet": return !Main.settings.abilityModifiers.vooPetRange.SetIfEnabled(ref __result);

                    case "SkillCarryThrow": return !Main.settings.corpseThrowRange.SetIfEnabled(ref __result);
                }
            }
            return true;
        }
    }

    [Horizontal]
    public struct ToggleableFloat
    {
        [Draw("")] public bool enabled;
        [Draw("", VisibleOn = "enabled|true")] public float value;

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
    }

    public class AbilityModifiers
    {
        [Header("Young Cooper")]
        [Draw("Throw Knife Range")] public ToggleableFloat cpyThrowKnifeRange = new ToggleableFloat(false, 8); // SkillCpyKeyThrowKnife
        //[Draw("Throw Knife Noise Radius")] public ToggleableFloat cpyThrowKnifeNoise = new ToggleableFloat(false, 8); // SkillCpyKeyThrowKnife

        [Header("Cooper")]
        [Draw("Knife Throw Range")] public ToggleableFloat copThrowKnifeRange = new ToggleableFloat(false, 12);
        [Draw("Coin Range")] public ToggleableFloat copWhistleStoneRange = new ToggleableFloat(false, 12);
        //[Draw("Coin Noise Radius")] public ToggleableFloat copWhistleStoneNoise = new ToggleableFloat(false, 12);
        [Draw("Left Gun Range")] public ToggleableFloat copGunLeftRange = new ToggleableFloat(false, 17);
        //[Draw("Left Gun Noise Radius")] public ToggleableFloat copGunLeftNoise = new ToggleableFloat(false, 17);
        [Draw("Right Gun Range")] public ToggleableFloat copGunRightRange = new ToggleableFloat(false, 17);
        //[Draw("Right Gun Noise Radius")] public ToggleableFloat copGunRightNoise = new ToggleableFloat(false, 17);

        [Header("Doc McCoy")]
        [Draw("Gun Range")] public ToggleableFloat mccGunRange = new ToggleableFloat(false, 55);
        //[Draw("Gun Noise Radius")] public ToggleableFloat mccGunNoise = new ToggleableFloat(false, 55);
        [Draw("Bag Range")] public ToggleableFloat mccStunboxRange = new ToggleableFloat(false, 5);
        [Draw("Gas Range")] public ToggleableFloat mccStunGrenadeRange = new ToggleableFloat(false, 8);

        [Header("Hector")]
        [Draw("Gun Range")] public ToggleableFloat traGunRange = new ToggleableFloat(false, 12);
        //[Draw("Gun Noise Radius")] public ToggleableFloat traGunNoise = new ToggleableFloat(false, 12);

        [Header("Kate")]
        [Draw("Gun Range")] public ToggleableFloat katGunRange = new ToggleableFloat(false, 9);
        //[Draw("Gun Noise Radius")] public ToggleableFloat katGunNoise = new ToggleableFloat(false, 9);
        [Draw("Perfume Range")] public ToggleableFloat katBlindRange = new ToggleableFloat(false, 12);

        [Header("Isabelle")]
        [Draw("Mind Control Range")] public ToggleableFloat vooControlRange = new ToggleableFloat(false, 10);
        [Draw("Connect Range")] public ToggleableFloat vooConnectRange = new ToggleableFloat(false, 10);
        [Draw("Cat Range")] public ToggleableFloat vooPetRange = new ToggleableFloat(false, 14);
    }

    public class Settings : ModSettings, IDrawable
    {
        [Draw("Infinite Ammo")] public bool infiniteAmmo = true;

        [Header("Abilities")]
        [Draw("", Collapsible = true)] public AbilityModifiers abilityModifiers = new AbilityModifiers();

        [Header("Corpse Throwing"), Space(5)]
        [Draw("Throw Range")] public ToggleableFloat corpseThrowRange = new ToggleableFloat(false, 5);
        [Draw("Knockout Range")] public ToggleableFloat corpseKnockoutRange = new ToggleableFloat(false, 2);
        [Draw("Allow a throw to knockout more than one enemy")] public bool multiKnockOut = true;

        [Header("Cheats and Dev options")]
        [Draw("Enable cheats")] public bool enableCheats = false;
        [Draw("Enable dev options")] public bool enableDev = true;
        [Draw("Enable extra dev options")] public bool enableDevExtra = true;

        public override void Save(ModEntry modEntry) => Save(this, modEntry);
        public void OnChange() { }
    }
}
