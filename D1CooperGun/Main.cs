using static UnityModManagerNet.UnityModManager;
using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;

namespace D1CooperGun
{
    public class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static ModEntry.ModLogger Logger;

        static void Load(ModEntry modEntry)
        {
            settings = ModSettings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        static bool OnToggle(ModEntry modEntry, bool enabled)
        {
            Main.enabled = enabled;
            return true;
        }

        static void OnGUI(ModEntry modEntry) => settings.Draw(modEntry);
        static void OnSaveGUI(ModEntry modEntry) => settings.Save(modEntry);
    }

    public struct State
    {
        public float originalCooldown;
        public bool isDoubleShot;
    }

    [HarmonyPatch]
    class Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillGunAttackTwoShot), "execute")]
        public static void SkillGunAttackTwoShot_execute_Prefix(ref SkillGunAttackTwoShot __instance, ref State __state, bool ___m_bTryExecuteTwoShot, PlanningModeCommand ___m_commandSecond, SkillGunAttackTwoShot ___m_skillGunOtherHand)
        {
            if (Main.enabled)
            {
                __state = new State
                {
                    originalCooldown = __instance.m_skillRangedData.m_fCooldown,
                    isDoubleShot = ___m_bTryExecuteTwoShot && !___m_commandSecond.bExecute && ___m_commandSecond.skill.bIsInRange(___m_commandSecond.skillCommand) && SkillThrow.bValidRaycast(__instance.character, __instance, ___m_commandSecond.skillCommand.m_MiCharacterTarget, true, null),
                };

                Main.Logger.Log("pre-execute: " + __state.isDoubleShot + " - " + __instance.iCount);

                var needReload = __state.isDoubleShot ? __instance.iCount == 2 : __instance.iCount == 1;
                var cooldown = needReload ? Main.settings.cooldownReload : Main.settings.cooldownShot;

                __instance.m_skillRangedData.m_fCooldown = cooldown;
                ___m_skillGunOtherHand.m_skillRangedData.m_fCooldown = cooldown;

                if (!__state.isDoubleShot) ___m_skillGunOtherHand.startCooldown(cooldown);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SkillGunAttackTwoShot), "execute")]
        public static void SkillGunAttackTwoShot_execute_Postfix(ref SkillGunAttackTwoShot __instance, State __state, SkillGunAttackTwoShot ___m_skillGunOtherHand)
        {
            if (Main.enabled)
            {
                Main.Logger.Log("post-execute: " + __state.isDoubleShot + " - " + __instance.iCount);
                if (__state.isDoubleShot && __instance.iCount == 2 || __instance.iCount == 0)
                {
                    __instance.character.m_charInventory.Insert(MiCharacterInventory.ItemType.GunAmmo, (uint)Main.settings.ammo, true, true);
                }
                __instance.m_skillRangedData.m_fCooldown = __state.originalCooldown;
                ___m_skillGunOtherHand.m_skillRangedData.m_fCooldown = __state.originalCooldown;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillGunAttack), "iMaxCount", MethodType.Getter)]
        public static bool SkillGunAttack_get_iMaxCount(ref SkillGunAttack __instance, ref int __result)
        {
            if (Main.enabled && __instance is SkillGunAttackTwoShot)
            {
                __result = Main.settings.ammo;
                return false;
            }
            return true;
        }
    }

    public class Settings : ModSettings, IDrawable
    {
        [Draw("Ammo", Min = 2)] public int ammo = 6;
        [Draw("Shot Cooldown", Min = 0)] public float cooldownShot = 0.25f;
        [Draw("Reload Cooldown", Min = 0)] public float cooldownReload = 10f;

        public override void Save(ModEntry modEntry) => Save(this, modEntry);

        public void OnChange() { }
    }
}
