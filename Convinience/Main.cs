using static UnityModManagerNet.UnityModManager;
using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;

namespace Desperados3Mods.Convinience
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

            MiSingletonMonoResource<GlobalUnityListener>.instance.m_evOnApplicationFocus -= MuteMusic;

            if (enabled && settings.muteMusicInBackground)
                MiSingletonMonoResource<GlobalUnityListener>.instance.m_evOnApplicationFocus += MuteMusic;

            return true;
        }

        static void OnGUI(ModEntry modEntry) => settings.Draw(modEntry);
        static void OnSaveGUI(ModEntry modEntry)
        {
            settings.Save(modEntry);

            MiSingletonMonoResource<GlobalUnityListener>.instance.m_evOnApplicationFocus -= MuteMusic;

            if (settings.muteMusicInBackground)
                MiSingletonMonoResource<GlobalUnityListener>.instance.m_evOnApplicationFocus += MuteMusic;
        }

        static float previousMasterVolume;

        static void MuteMusic(bool focus)
        {
            if (focus)
            {
                MiAudioMixer.s_fVolumeMaster = previousMasterVolume;
            }
            else
            {
                previousMasterVolume = MiAudioMixer.s_fVolumeMaster;
                MiAudioMixer.s_fVolumeMaster = 0;
            }
            MiSingletonMonoResource<MiAudioMixer>.instance.applySettings();
        }
    }

    [HarmonyPatch]
    class Patch
    {
        static bool needToEnableHighlights = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MiGameInput), "MiStart")]
        internal static void MiGameInput_MiStart()
        {
            needToEnableHighlights = Main.enabled && Main.settings.startHighlights;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MiGameInput), "MiUpdate")]
        internal static void MiGameInput_MiUpdate(MiGameInput __instance)
        {
            if (needToEnableHighlights)
            {
                needToEnableHighlights = false;
                __instance.toggleHighlightAllActiveUI();
            }
        }

        static bool zoomOutNext = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MiCamBirdviewGameTactics), "MiStart")]
        internal static void MiCamBirdviewGameTactics_MiStart()
        {
            zoomOutNext = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MiCamBirdviewGameTactics), "Update")]
        internal static void MiCamBirdviewGameTactics_Update(MiCamBirdviewGameTactics __instance)
        {
            if (Main.enabled && Main.settings.startZoom)
            {
                if (MiCamHandler.bCutsceneMode) zoomOutNext = true;
                else if (zoomOutNext)
                {
                    __instance.fZoomHeight = __instance.FMaxHeight;
                    zoomOutNext = false;
                }
            }
        }
    }

    public class Settings : ModSettings, IDrawable
    {
        [Draw("Start Highlights On")] public bool startHighlights = false;
        [Draw("Start Zoomed Out")] public bool startZoom = false;
        [Draw("Mute Music when in Background")] public bool muteMusicInBackground = false;

        public override void Save(ModEntry modEntry) => Save(this, modEntry);
        public void OnChange() { }
    }
}
