using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MiCoreServices;
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

        static float? originalVolume = null;

        void Awake()
        {
            configStartHighlights = Config.Bind("General", "Start Highlights On", false);
            configStartZoom = Config.Bind("General", "Start Zoomed Out", false);
            configMuteMusicInBackground = Config.Bind("General", "Mute Music when in Background", false);

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        void Start() => GlobalManager.executeOnInit(OnGlobalManagerInit, 0);

        private void OnGlobalManagerInit()
        {
            GlobalManager.instance.processLifetimeService.Suspending += OnApplicationSuspended;
            GlobalManager.instance.processLifetimeService.Resuming += OnApplicationResumed;
            GlobalManager.instance.processLifetimeService.Constrained += OnApplicationConstrained;
            GlobalManager.instance.processLifetimeService.Unconstrained += OnApplicationUnconstrained;
        }

        private void OnApplicationSuspended() => OnApplicationFocus(false);
        private void OnApplicationResumed() => OnApplicationFocus(false);
        private void OnApplicationConstrained() => OnApplicationFocus(false);
        private void OnApplicationUnconstrained() => OnApplicationFocus(false);

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                if (originalVolume != null)
                {
                    Logger.LogDebug("Focus gained. Setting volume to: " + originalVolume);
                    AudioListener.volume = (float)originalVolume;
                }
                else Logger.LogDebug("Focus gained but no previous volume known.");
                originalVolume = null;
            }
            else if (configMuteMusicInBackground.Value && originalVolume == null && MiAudioMixer.s_fVolumeMaster > 0)
            {
                originalVolume = AudioListener.volume;
                AudioListener.volume = 0;
                Logger.LogDebug("Focus lost. Muting audio. Previous volume: " + originalVolume);
            }
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
        }
    }
}
