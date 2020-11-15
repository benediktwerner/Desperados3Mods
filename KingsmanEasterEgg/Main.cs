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
        public const string Version = "1.0";

        public static ConfigEntry<float> configProbability;

        public void Awake()
        {
            configProbability = Config.Bind("General", "TriggerChancePercent", 20f,
                new ConfigDescription("Chance for the Easter Egg to trigger in %", new AcceptableValueRange<float>(0f, 100f))
            );

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }

    [HarmonyPatch(typeof(TriggerPercentage), "triggerOverride")]
    class Hooks
    {
        internal static void Prefix(TriggerPercentage __instance)
        {
            __instance.m_fPercentage = Mathf.Clamp(Main.configProbability.Value, 0, 100);
        }
    }
}
