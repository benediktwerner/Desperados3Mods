using BepInEx;
using BepInEx.Configuration;
using System.Linq;
using UnityEngine;

namespace Desperados3Mods.ProgressRestorer
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.progressrestorer";
        public const string Name = "ProgressRestorer";
        public const string Version = "1.0.0";

        static int? selectedMission = null;

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
                    else if (GUILayout.Button("Complete", GUILayout.Width(200))) {
                        badge.setComplete(0);
                        MiSingletonNoMono<SaveGameManager>.instance.saveSaveGameUser();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }
    }
}
