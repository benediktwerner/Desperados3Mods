using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Desperados3Mods.DevKillsList
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("com.bepis.bepinex.configurationmanager")]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.devkilllist";
        public const string Name = "DevKillList";
        public const string Version = "1.0.1";

        void Awake()
        {
            Config.Bind("", "__ListDrawer", false,
                new ConfigDescription("Don't edit this! This has no effect. It's just used to draw the list in the ConfigurationManager window.", null,
                    new ConfigurationManagerAttributes
                    {
                        CustomDrawer = DrawList,
                        HideDefaultButton = true,
                        HideSettingName = true
                    }
                )
            );
        }

        static void DrawList(ConfigEntryBase entry)
        {
            try
            {
                var achievement = GetBountyHunterAchievement();
                if (achievement.m_eCompletedState == MiCoreServices.AchievementBase.CompleteState.Completed)
                    GUILayout.Label("Achievement completed. You killed all the devs!", GUILayout.ExpandWidth(true));
                else if (achievement.m_eCompletedState == MiCoreServices.AchievementBase.CompleteState.CompletingAwaitingApiCall)
                    GUILayout.Label("Achievement completed. You killed all the devs! (The achievement is still in the process of being registered)", GUILayout.ExpandWidth(true));
                else
                {
                    var done = achievement.m_lGUIDsTriggered.Count;
                    var total = Dev.DEVS.Length;
                    GUILayout.BeginVertical();
                    GUILayout.Label("You found " + done + "/" + total + " devs. " + (total - done) + " are still missing:", GUILayout.ExpandWidth(true));
                    foreach (var dev in Dev.DEVS)
                    {
                        if (!achievement.m_lGUIDsTriggered.Contains(dev.guid))
                        {
                            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                            GUILayout.Label(Dev.LEVEL_NAMES[dev.level], GUILayout.Width(200));
                            GUILayout.Label(dev.nick);
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.Label("Check https://www.trueachievements.com/a298714/veteran-bounty-hunter-achievement for the exact locations.", GUILayout.ExpandWidth(true));
                    GUILayout.EndVertical();
                }
            }
            catch (Exception e)
            {
                GUILayout.Label("Error: " + e.Message, GUILayout.ExpandWidth(true));
            }
        }

        static AchievementSave GetBountyHunterAchievement()
        {
            foreach (var achievement in AchievementCollection.instance.getSaveAchievements())
            {
                if (achievement.m_iEnumID == (int)AchievementID.VeteranBountyHunter)
                    return achievement;
            }
            throw new Exception("Veteran Bounty Hunter Achievement not found");
        }
    }

    struct Dev
    {
        public string level;
        public string nick;
        public string name;
        public int guid;

        Dev(string level, string nick, string name, int guid)
        {
            this.level = level;
            this.nick = nick;
            this.name = name;
            this.guid = guid;
        }

        public static Dev[] DEVS = {
            new Dev("train", "Ben", "", -1726282228),
            new Dev("train", "Jojo", "", 1489316608),

            new Dev("town", "sfxphil", "", 1078832935),
            new Dev("town", "Pawel", "", 749088431),

            new Dev("wedding", "Stella", "", 299643032),
            new Dev("wedding", "Dennis", "", -841455808),

            new Dev("ranch", "Filippo", "", 345789839),
            new Dev("ranch", "Gina", "", 1620598830),

            new Dev("bridge", "Tom", "", 39388096),
            new Dev("bridge", "Ramon", "", -1054488406),

            new Dev("flashback2", "Anneke", "", 1427747142),

            new Dev("harbor", "Daniel", "", -2128368385),
            new Dev("harbor", "Anna", "", -2073015168),
            new Dev("harbor", "Dorian", "", -137749288),

            new Dev("river", "Toby", "", 350329208),
            new Dev("river", "Leoni", "", 1327516076),

            new Dev("city1", "Anisha", "", 423277358),
            new Dev("city1", "Flo", "", 1937789139),
            new Dev("city1", "Lucas", "", 220909290),

            new Dev("swamp", "Frieder", "", 1404751820),
            new Dev("swamp", "Reinier", "", -714421517),

            new Dev("city2", "Mo", "", 1910509156),
            new Dev("city2", "Cem", "", 169418365),

            new Dev("mine", "Dom", "", -977970577),
            new Dev("mine", "Phil", "", 1236412014),

            new Dev("pueblo", "Jones", "", -1268319850),
            new Dev("pueblo", "Bianca", "", 1787167685),

            new Dev("hacienda", "Simon", "", 1847077581),
            new Dev("hacienda", "Matt", "", -6903977),

            new Dev("showdown", "Hambo", "", 993037679),
            new Dev("showdown", "Felix", "", -241333596),
        };

        public static Dictionary<string, string> LEVEL_NAMES = new Dictionary<string, string> {
            {"train",       "Level 2: Byers Pass"},
            {"town",        "Level 3: Flagstone"},
            {"wedding",     "Level 4: Higgins' Estate"},
            {"ranch",       "Level 5: O'Hara Ranch"},
            {"bridge",      "Level 6: Eagle Falls"},

            {"flashback2",  "Level 7: Devil's Canyon"},
            {"harbor",      "Level 8: Baton Rouge"},
            {"river",       "Level 9: Mississippi River"},
            {"city1",       "Level 10: New Orleans"},
            {"swamp",       "Level 11: Queen's Nest"},
            {"city2",       "Level 12: New Orleans Docks"},

            {"mine",        "Level 13: DeVitt Goldmine"},
            {"pueblo",      "Level 14: Las Pierdas"},
            {"hacienda",    "Level 15: Cassa DeVitt"},
            {"showdown",    "Level 16: Devil's Canyon"},
        };
    }
}
