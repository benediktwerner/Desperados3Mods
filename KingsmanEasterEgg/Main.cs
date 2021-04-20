using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Desperados3Mods.KingsmanEasterEgg
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.kingsmaneasteregg";
        public const string Name = "KingsmanEasterEgg";
        public const string Version = "1.1";

        public static ConfigEntry<float> configExplodingHeadsProbability;
        public static ConfigEntry<bool> configForceAlternativeKateFan;
        public static ConfigEntry<bool> configRemoveTimeoutBetweenIdleAnimations;

        public void Awake()
        {
            configExplodingHeadsProbability = Config.Bind("General", "ExplodingHeadsTriggerChancePercent", 20f,
                new ConfigDescription("Chance for the exploding heads Easter Egg to trigger in %", new AcceptableValueRange<float>(0f, 100f))
            );
            configForceAlternativeKateFan = Config.Bind("General", "ForceAlternativeKateFan", false,
                new ConfigDescription("Force Kate's alternative fan")
            );
            configRemoveTimeoutBetweenIdleAnimations = Config.Bind("General", "RemoveTimeoutBetweenIdleAnimations", false,
                new ConfigDescription("Remove timeout between idle animations, causing idle characters to continously perform random idle animations one after another")
            );

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }

    class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TriggerPercentage), "triggerOverride")]
        internal static void TriggerPercentage_triggerOverride_Prefix(TriggerPercentage __instance)
        {
            __instance.m_fPercentage = Mathf.Clamp(Main.configExplodingHeadsProbability.Value, 0, 100);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RandomizeKateFan), "MiOnEnable")]
        internal static void RandomizeKateFan_MiOnEnable_Prefix(RandomizeKateFan __instance)
        {
            if (!Main.configForceAlternativeKateFan.Value) return;

            AccessTools.Field(typeof(RandomizeKateFan), "s_iBaseCount").SetValue(null, __instance.m_iMinBaseOccurance);
            __instance.m_fProbabilityAlternative = 2f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MiIdle), "fRandomIntervalDuration", MethodType.Getter)]
        internal static void MiIdle_fRandomIntervalDuration_Postfix(ref float __result)
        {
            if (!Main.configRemoveTimeoutBetweenIdleAnimations.Value) return;

            __result = 0.1f;
        }
    }
}
