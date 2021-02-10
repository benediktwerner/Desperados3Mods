using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace Desperados3Mods.ProgressRestorer
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.progressrestorer";
        public const string Name = "ProgressRestorer";
        public const string Version = "1.1.0";

        static ManualLogSource StaticLogger;

        static bool showBadges = false;
        static bool showAchievements = false;
        static int? selectedMission = null;
        static string achievementIDString = "";
        static int? achievementID = null;
        static string message = "";
        static int progress = 0;

        void Awake()
        {
            Config.Bind("", "__ListDrawer", false,
                new ConfigDescription("Don't edit this! This has no effect. It's just used to draw the UI in the ConfigurationManager window.", null,
                    new ConfigurationManagerAttributes
                    {
                        CustomDrawer = Draw,
                        HideDefaultButton = true,
                        HideSettingName = true
                    }
                )
            );

            StaticLogger = Logger;
        }

        static void Draw(ConfigEntryBase entry)
        {
            var saveGameUser = SaveGameManager.instance.saveGameUser;

            GUILayout.BeginVertical();

            if (GUILayout.Button("Unlock all levels"))
            {
                MiSingletonNoMono<SaveGameManager>.instance.saveGameUser.unlockAllLevels();
                MiSingletonNoMono<SaveGameManager>.instance.saveGameUser.unlockAllHeadHunters();
                MiSingletonNoMono<SaveGameManager>.instance.saveSaveGameUser();
            }

            if (GUILayout.Button("Badges"))
            {
                showBadges = !showBadges;
                showAchievements = false;
            }

            if (showBadges)
            {
                GUILayout.BeginVertical("box");
                DrawBadges(saveGameUser);
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Achievements"))
            {
                showAchievements = !showAchievements;
                showBadges = false;
            }

            if (showAchievements)
            {
                GUILayout.BeginVertical("box");
                try
                {
                    DrawAchievements();
                }
                catch (Exception e)
                {
                    StaticLogger.LogError(e);
                    message = "Error:\n" + e.ToString();
                }
                GUILayout.EndVertical();
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                GUILayout.Label(message);
            }

            GUILayout.EndVertical();
        }

        static void DrawBadges(SaveGameUser saveGameUser)
        {
            foreach (var mission in saveGameUser.lSaveMissionData)
            {
                if (mission.m_liBadgeData.Count == 0) continue;

                if (mission.m_bMissionLocked)
                {
                    GUILayout.Label($"{mission.miScene.strNiceName} (Locked)");
                    continue;
                }

                if (!mission.m_bMissionFinished)
                {
                    GUILayout.Label($"{mission.miScene.strNiceName} (Not completed yet)");
                    continue;
                }

                var badgeData = mission.m_liBadgeData;
                var guid = mission.m_iGuidMissionData.iGuid;
                var unlockedCount = badgeData.Select(b => b.bCompleted ? 1 : 0).Sum();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{mission.miScene.strNiceName} ({unlockedCount}/{badgeData.Count})");

                var show = selectedMission == guid;
                if (GUILayout.Button(show ? "Hide" : "Show", GUILayout.Width(200)))
                {
                    show = !show;
                    selectedMission = show ? guid as int? : null;
                }
                GUILayout.EndHorizontal();
                if (!show) continue;

                GUILayout.BeginVertical("box");
                foreach (var badge in badgeData)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(badge.badgeRef.lstrText.strText);
                    if (badge.bCompleted) GUILayout.Label("Completed", GUILayout.Width(200));
                    else if (GUILayout.Button("Complete", GUILayout.Width(200)))
                    {
                        badge.setComplete(0);
                        MiSingletonNoMono<SaveGameManager>.instance.saveSaveGameUser();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        static void DrawAchievements()
        {
            var service = PrimaryGameUser.gameUserHolder.achievementsService as MiCoreServices.AchievementsServiceBase;
            if (service == null)
            {
                GUILayout.Label("Unable to retrieve achievement service status");
            }
            else
            {
                GUILayout.Label("Achievement service state: " + service.eStateAchievements);
                GUILayout.Label("Achievement service API running: " + MiCoreServices.GlobalManager.instance.API.bAPIRunning);
                GUILayout.Label("Achievement service connected: " + service.IsConnected);
                GUILayout.Label("Achievement service activated: " + service.IsActivated);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Achievement ID:");
            achievementIDString = GUILayout.TextField(achievementIDString);
            if (GUILayout.Button("Load"))
            {
                if (int.TryParse(achievementIDString, out var newAchievementID))
                {
                    achievementID = newAchievementID;
                    message = "";
                }
                else
                {
                    achievementID = null;
                    message = "Invalid ID";
                }
            }
            GUILayout.EndHorizontal();

            if (achievementID != null)
            {
                var a = AchievementCollection.instance.liAchievements.Where(x => x.iEnumID == (int)achievementID).SingleOrDefault() as Achievement;
                if (a == null)
                {
                    message = "No achievement with ID " + achievementID;
                    return;
                }

                GUILayout.Label("ID: " + achievementID);
                GUILayout.Label("Name: " + a.strName);
                GUILayout.Label("Type: " + a.eType);
                if (a.eType == Achievement.Type.Progress) GUILayout.Label("Progress: " + a.iProgress + "/" + a.iProgressCount);
                GUILayout.Label("State: " + a.eCompleteState);

                if (GUILayout.Button("Reset progress"))
                {
                    AccessTools.Field(typeof(Achievement), "m_iProgress").SetValue(a, 0);
                }

                if (GUILayout.Button("Reset state"))
                {
                    AccessTools.Field(typeof(Achievement), "m_eCompleteState").SetValue(a, Achievement.CompleteState.Running);
                }

                GUILayout.BeginHorizontal();
                var progressString = GUILayout.TextField(progress.ToString());
                if (string.IsNullOrWhiteSpace(progressString)) progress = 0;
                else int.TryParse(progressString, out progress);
                if (GUILayout.Button("Set Progress"))
                {
                    AccessTools.Field(typeof(Achievement), "m_iProgress").SetValue(a, progress);
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Trigger increase progress"))
                {
                    a.progressCount(1);
                }

                if (GUILayout.Button("Trigger completion"))
                {
                    AccessTools.Field(typeof(Achievement), "m_eCompleteState").SetValue(a, Achievement.CompleteState.CompletingAwaitingApiCall);
                    PrimaryGameUser.gameUserHolder.achievementsService.completeAchievement(a);
                }
            }
        }
    }
}
