using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Desperados3Mods.Convenience
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.convenience";
        public const string Name = "Convenience";
        public const string Version = "1.0.2";

        static ConfigEntry<bool> configStartHighlights;
        static ConfigEntry<bool> configStartZoom;
        static ConfigEntry<bool> configMuteMusicInBackground;

        void Awake()
        {
            configStartHighlights = Config.Bind("General", "Start Highlights On", false);
            configStartZoom = Config.Bind("General", "Start Zoomed Out", false);
            configMuteMusicInBackground = Config.Bind("General", "Mute Music when in Background", false);

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        static class Hooks
        {
            static bool needToEnableHighlights = false;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MiGameInput), "MiStart")]
            internal static void MiGameInput_MiStart()
            {
                needToEnableHighlights = configStartHighlights.Value;
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
                if (configStartZoom.Value)
                {
                    if (MiCamHandler.bCutsceneMode) zoomOutNext = true;
                    else if (zoomOutNext)
                    {
                        __instance.fZoomHeight = __instance.FMaxHeight;
                        zoomOutNext = false;
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GlobalUnityListener), "updateFocus")]
            internal static void GlobalUnityListener_updateFocus(GlobalUnityListener __instance)
            {
                if (__instance.bApplicationFocus)
                    AudioListener.volume = 1;
                else if (configMuteMusicInBackground.Value)
                    AudioListener.volume = 0;
            }
        }
    }
}
