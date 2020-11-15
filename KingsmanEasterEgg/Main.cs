using static UnityModManagerNet.UnityModManager;
using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;

namespace Desperados3Mods.KingsmanEasterEgg
{
    public class Main
    {
        public static bool enabled;
        public static Settings settings;

        public static void Load(ModEntry modEntry)
        {
            settings = ModSettings.Load<Settings>(modEntry);

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

    [HarmonyPatch(typeof(TriggerPercentage), "triggerOverride")]
    class Patch
    {
        internal static void Prefix(TriggerPercentage __instance, out float __state)
        {
            __state = __instance.m_fPercentage;

            if (Main.enabled)
            {
                __instance.m_fPercentage = Main.settings.chance;
            }
        }

        internal static void Postfix(TriggerPercentage __instance, float __state)
        {
            __instance.m_fPercentage = __state;
        }
    }

    public class Settings : ModSettings, IDrawable
    {
        [Draw("Chance for the Easter Egg to trigger in %", Min = 0, Max = 100)] public float chance = 100;

        public override void Save(ModEntry modEntry) => Save(this, modEntry);

        public void OnChange() { }
    }
}
