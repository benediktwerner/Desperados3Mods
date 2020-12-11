using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    static class Commands
    {
        static bool show = false;
        static readonly FieldInfo fieldMissionSetupSettings_s_difficultySettings = AccessTools.Field(typeof(MissionSetupSettings), "s_difficultySettings");

        public static void Bind(ConfigFile config)
        {
            config.Bind("2. Commands", "__CommandsDrawer", false,
                new ConfigDescription("Don't edit this! This has no effect. It's just used to draw the commands section in the ConfigurationManager window.", null,
                    new ConfigurationManagerAttributes
                    {
                        Category = "2. Commands",
                        CustomDrawer = DrawCommands,
                        HideDefaultButton = true,
                        HideSettingName = true
                    }
                )
            );
        }

        static void DrawCommands(ConfigEntryBase entry)
        {
            var gameInput = MiGameInput.instance;
            if (gameInput == null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("Enter a level to show available commands");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical();

            if (GUILayout.Button(show ? "Hide" : "Show")) show = !show;
            if (!show)
            {
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical("box");

            for (int i = 0; i < gameInput.iPlayerCharacterCount; i++)
            {
                MiCharacter character = gameInput.lPlayerCharacter[i];
                var uiData = character.uiData;

                GUILayout.Label(uiData.lstrName.strText, Main.SkinBold);
                DrawAdjustableInt("Health", character.m_charHealth.iHealth.ToString(),
                    () => character.m_charHealth.iHealth--,
                    () => character.m_charHealth.iHealth++
                );

                foreach (var skill in character.controller.lSkills)
                {
                    var playerSkill = skill as PlayerSkill;
                    if (playerSkill == null) continue;

                    var ammoType = GetAmmoType(playerSkill);
                    if (ammoType == 0) continue;

                    var charSkill = new CharacterSkillName(skill.m_skillData.name);

                    DrawAdjustableInt(charSkill.skill + " Ammo", $"{playerSkill.iCount}/{character.m_charInventory.MaxCount(ammoType)}",
                        () => character.m_charInventory.Remove(ammoType),
                        () => character.m_charInventory.Insert(ammoType)
                    );
                }
            }

#if DEBUG
            if (GUILayout.Button("Dump Skill Data"))
            {
                var content = "";
                foreach (var data in Resources.FindObjectsOfTypeAll<PlayerSkillData>())
                {
                    content += data.name + "\n";
                }
                File.WriteAllText(Path.Combine(Paths.ConfigPath, "skills.toml"), content);
            }
#endif
            var difficultySettings = (MissionSetupSettings.DifficultySettings)fieldMissionSetupSettings_s_difficultySettings.GetValue(null);
            var showdownPauseOld = difficultySettings.m_bFocusModePause;
            difficultySettings.m_bFocusModePause = GUILayout.Toggle(difficultySettings.m_bFocusModePause, "Pausing Showdownmode");
            if (showdownPauseOld != difficultySettings.m_bFocusModePause)
            {
                fieldMissionSetupSettings_s_difficultySettings.SetValue(null, difficultySettings);
            }

            if (SceneStatistics.instance != null && GUILayout.Button("Reset Level Statistics"))
            {
                var stats = SceneStatistics.instance;
                var statsType = typeof(SceneStatistics);
                ((IDictionary)AccessTools.Field(statsType, "m_dictNPCsKilledBy").GetValue(stats)).Clear();
                ((IDictionary)AccessTools.Field(statsType, "m_dictSkillsUsed").GetValue(stats)).Clear();
                AccessTools.Field(statsType, "m_iAlarmCount").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iBodiesHidden").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iCritterKilled").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iDeadBodiesFound").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iEnemiesKilled").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iEnemiesKilledWithTorches").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iEnemiesShotPlayer").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iDeadBodiesFound").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iEnemiesSpared").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iEnemiesTiedUp").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iDeadBodiesFound").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iNPCsKilled").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iPlayerDetected").SetValue(stats, 0);
                AccessTools.Field(statsType, "m_iSamuraiKilled").SetValue(stats, 0);

                var statsData = stats.persistentSaveData;
                statsData.fPlaytime = 0;
                statsData.iFrameCount = 0;
                statsData.iLoadCount = 0;
                statsData.iSaveCount = 0;
                statsData.iTotalHealthLost = 0;
                statsData.lPlayerDeathCount.Clear();
                statsData.lPlayerStateDurations.Clear();

                var statsNoSaveData = SceneStatisticsNoSave.instance.persistentSaveData;
                statsNoSaveData.fPlaytime = 0;
                statsNoSaveData.iFrameCount = 0;
                statsNoSaveData.iLoadCount = 0;
                statsNoSaveData.iSaveCount = 0;
                statsNoSaveData.iTotalHealthLost = 0;
                statsNoSaveData.lPlayerDeathCount.Clear();
                statsNoSaveData.lPlayerStateDurations.Clear();
            }

            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        public static MiCharacterInventory.ItemType GetAmmoType(PlayerSkill skill)
        {
            switch (skill)
            {
                case SkillSnipe _:
                    return MiCharacterInventory.ItemType.SnipeAmmo;
                case SkillGunAttackPlayer _:
                    return MiCharacterInventory.ItemType.GunAmmo;
                case SkillThrowGrenadeStun _:
                    return MiCharacterInventory.ItemType.KnockoutGrenade;
                case SkillThrowStraightMindControlDart _:
                    return MiCharacterInventory.ItemType.ControlDart;
                default:
                    return 0;
            }
        }

        static void DrawAdjustableInt(string label, string value, Action increase, Action decrease)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(label, GUILayout.Width(200));
            GUILayout.Label(value, GUILayout.Width(50));

            if (GUILayout.Button("-", GUILayout.Width(50))) increase();
            if (GUILayout.Button("+", GUILayout.Width(50))) decrease();

            GUILayout.EndHorizontal();
        }
    }
}
