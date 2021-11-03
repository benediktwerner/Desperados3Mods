using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace Desperados3Mods.D1CooperGun
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.d1coopergun";
        public const string Name = "D1CooperGun";
        public const string Version = "1.0";

        public static ConfigEntry<bool> configEnabled;
        public static ConfigEntry<int> configAmmo ;
        public static ConfigEntry<float> configCooldownShot;
        public static ConfigEntry<float> configCooldownReload;

        void Awake()
        {
            configEnabled = Config.Bind("General", "Enabled", true, new ConfigDescription("Enabled", null, new ConfigurationManagerAttributes { Order = 100 }));
            configAmmo = Config.Bind("General", "Ammo", 6);
            configCooldownShot = Config.Bind("General", "Shot Cooldown", 0.25f);
            configCooldownReload = Config.Bind("General", "Reload Cooldown", 10f);

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }

    struct State
    {
        public float originalCooldown;
        public bool isDoubleShot;
    }

    class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillGunAttackTwoShot), "execute")]
        internal static void SkillGunAttackTwoShot_execute_Prefix(ref SkillGunAttackTwoShot __instance, ref State __state, bool ___m_bTryExecuteTwoShot, PlanningModeCommand ___m_commandSecond, SkillGunAttackTwoShot ___m_skillGunOtherHand)
        {
            if (Main.configEnabled.Value)
            {
                __state = new State
                {
                    originalCooldown = __instance.m_skillRangedData.m_fCooldown,
                    isDoubleShot = ___m_bTryExecuteTwoShot && !___m_commandSecond.bExecute && ___m_commandSecond.skill.bIsInRange(___m_commandSecond.skillCommand) && SkillThrow.bValidRaycast(__instance.character, __instance, ___m_commandSecond.skillCommand.m_MiCharacterTarget, true, null),
                };

                var needReload = __state.isDoubleShot ? __instance.iCount == 2 : __instance.iCount == 1;
                var cooldown = needReload ? Main.configCooldownReload.Value : Main.configCooldownShot.Value;

                __instance.m_skillRangedData.m_fCooldown = cooldown;
                ___m_skillGunOtherHand.m_skillRangedData.m_fCooldown = cooldown;

                if (!__state.isDoubleShot) ___m_skillGunOtherHand.startCooldown(cooldown);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SkillGunAttackTwoShot), "execute")]
        internal static void SkillGunAttackTwoShot_execute_Postfix(ref SkillGunAttackTwoShot __instance, State __state, SkillGunAttackTwoShot ___m_skillGunOtherHand)
        {
            if (Main.configEnabled.Value)
            {
                if (__state.isDoubleShot && __instance.iCount == 2 || __instance.iCount == 0)
                {
                    __instance.character.m_charInventory.Insert(MiCharacterInventory.ItemType.GunAmmo, (uint)Main.configAmmo.Value, true, true);
                }
                __instance.m_skillRangedData.m_fCooldown = __state.originalCooldown;
                ___m_skillGunOtherHand.m_skillRangedData.m_fCooldown = __state.originalCooldown;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillGunAttack), "iMaxCount", MethodType.Getter)]
        internal static bool SkillGunAttack_get_iMaxCount(ref SkillGunAttack __instance, ref int __result)
        {
            if (Main.configEnabled.Value && __instance is SkillGunAttackTwoShot)
            {
                __result = Main.configAmmo.Value;
                return false;
            }
            return true;
        }
    }
}
