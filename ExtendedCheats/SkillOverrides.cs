using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    class SkillOverrides
    {
        const string CONFIG_FILENAME = "skills.json";
        static string ConfigPath => Path.Combine(Paths.ConfigPath, CONFIG_FILENAME);
        static readonly string[] PlayerSkillNames = {
            "SkillCopKeyKill",
            "SkillCopKeyStun",
            "SkillCopKeyThrowKnife",
            "SkillCopKeyWhistleStone",
            "SkillCopKeyGunLeft",
            "SkillCopKeyGunRight",
            "SkillCpyKeyThrowKnife",
            "SkillKatKeyStun",
            "SkillKatKeyBlind",
            "SkillKatKeyDisguise",
            "SkillKatKeyDistract",
            "SkillKatKeyFollow",
            "SkillKatKeyGun",
            "SkillMccKeyKill",
            "SkillMccKeyStun",
            "SkillMccKeyGun",
            "SkillMccKeyStunGrenade",
            "SkillMccKeyStunbox",
            "SkillHealMcc",
            "SkillTraKeyKill",
            "SkillTraKeyStun",
            "SkillTraKeyTrap",
            "SkillTraKeyWhistle",
            "SkillTraKeyGun",
            "SkillTraKeyHeal",
            "SkillCarryThrow",
            "SkillVooKeyKill",
            "SkillVooKeyStun",
            "SkillVooKeyConnect",
            "SkillVooKeyControl",
            "SkillVooKeyPet",
            "SkillHealVoo",
        };

        class SkillOverrideOld
        {
            public float? cooldown = null;
            public float? range = null;

            public SkillOverrideOld() { }

            public SkillOverrideOld(JSONClass obj)
            {
                range = obj["range"]?.AsFloat;
                cooldown = obj["cooldown"]?.AsFloat;
            }
        }

        Dictionary<string, Dictionary<string, SkillOverrideOld>> skillOverrides;

        bool show = false;
        string charToShow = null;
        bool generateSkillsFile = false;

        readonly ConfigEntry<bool> configEnabled;

        internal SkillOverrides(ConfigFile config)
        {
            configEnabled = config.Bind("General", "Enable Skill Overrides", false,
                new ConfigDescription(
                    "Enable overriding skill. Use the in-game settings menu provided by BepInEx.ConfigurationManager or edit skills.json to modify values (enter a level to generate).",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        Category = "4. Skill Overrides",
                        CustomDrawer = DrawSkills,
                        HideDefaultButton = true,
                        HideSettingName = true
                    }
                )
            );
            Load();
        }

        void DrawSkills(ConfigEntryBase _)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button(show ? "Hide" : "Show")) show = !show;
            if (!show)
            {
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            configEnabled.Value = GUILayout.Toggle(configEnabled.Value, configEnabled.Value ? "Overrides Enabled" : "Overrides Disabled");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset all overrides", GUILayout.Width(200)))
            {
                skillOverrides = null;
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reload skills.json"))
            {
                Load();
                Merge();
            }

            if (skillOverrides == null)
            {
                GUILayout.Label("skills.json not found or failed to load. Please enter a level to generate.");
            }
            else
            {
                if (GUILayout.Button("Apply changes and Save to skills.json"))
                {
                    Merge();
                    Store();
                }

                foreach (var character in skillOverrides)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(character.Key, Main.HeadingStyle, GUILayout.Width(300));
                    var showChar = charToShow == character.Key;

                    if (GUILayout.Button(showChar ? "Hide" : "Show", GUILayout.Width(200)))
                    {
                        showChar = !showChar;
                        charToShow = showChar ? character.Key : null;
                    }
                    GUILayout.EndHorizontal();
                    if (!showChar) continue;

                    GUILayout.BeginVertical("box");
                    foreach (var skill in character.Value)
                    {
                        skill.Value.range = DrawFloatField(skill.Key + " Range", skill.Value.range);
                        skill.Value.cooldown = DrawFloatField(skill.Key + " Cooldown", skill.Value.cooldown);
                    }
                    GUILayout.EndVertical();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        float? DrawFloatField(string label, float? value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));

            var strVal = GUILayout.TextField(value?.ToString("f2", CultureInfo.InvariantCulture) ?? "Reload level", GUILayout.Width(100));
            float? newVal = null;
            if (!string.IsNullOrWhiteSpace(strVal) && float.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValFloat))
            {
                newVal = newValFloat;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset", GUILayout.Width(100)))
            {
                newVal = null;
            }

            GUILayout.EndHorizontal();

            return newVal;
        }

        void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                skillOverrides = null;
                generateSkillsFile = true;
                return;
            }

            skillOverrides = new Dictionary<string, Dictionary<string, SkillOverrideOld>>();

            try
            {
                var config = JSON.Parse(File.ReadAllText(ConfigPath)).AsObject;

                foreach (var name in PlayerSkillNames)
                {
                    var charSkill = new CharacterSkill(name);
                    var character = config[charSkill.character]?.AsObject;
                    if (character == null) continue;
                    var skill = character[charSkill.name].AsObject;
                    if (skill == null) continue;

                    if (!skillOverrides.TryGetValue(charSkill.character, out var characterSkills))
                    {
                        characterSkills = new Dictionary<string, SkillOverrideOld>();
                        skillOverrides[charSkill.character] = characterSkills;
                    }

                    characterSkills[charSkill.name] = new SkillOverrideOld(skill);
                }
            }
            catch (Exception e)
            {
                Main.StaticLogger.LogError("Error when loading skills.json:");
                Main.StaticLogger.LogError(e);
                skillOverrides = null;
            }
        }

        void Store()
        {
            if (skillOverrides == null)
            {
                generateSkillsFile = true;
                return;
            }

            var content = "// Delete values or set them to null to reset them\n";
            content += "{\n";
            var firstChar = true;

            foreach (var character in skillOverrides)
            {
                if (firstChar) firstChar = false;
                else content += ",\n";

                content += "  \"" + character.Key + "\": {\n";

                bool firstSkill = true;
                foreach (var skill in character.Value)
                {
                    if (firstSkill) firstSkill = false;
                    else content += ",\n";
                    content += "    \"" + skill.Key + "\": {\n";
                    content += "      \"range\": " + (skill.Value.range?.ToString(CultureInfo.InvariantCulture) ?? "null") + ",\n";
                    content += "      \"cooldown\": " + (skill.Value.cooldown?.ToString(CultureInfo.InvariantCulture) ?? "null") + "\n";
                    content += "    }";
                }
                content += "\n";
                content += "  }";
            }
            content += "\n}";

            File.WriteAllText(ConfigPath, content);
        }

        internal void OnLevelLoad()
        {
            if (skillOverrides == null || configEnabled.Value)
            {
                Merge();

                if (generateSkillsFile)
                {
                    Store();
                    generateSkillsFile = false;
                }
            }
        }

        void Merge()
        {
            var objs = Resources.FindObjectsOfTypeAll<PlayerSkillData>();

            if (objs.Length == 0) return;

            if (skillOverrides == null) skillOverrides = new Dictionary<string, Dictionary<string, SkillOverrideOld>>();

            foreach (var obj in objs)
            {
                if (PlayerSkillNames.Contains(obj.name))
                {
                    var charSkill = new CharacterSkill(obj.name);

                    if (!skillOverrides.TryGetValue(charSkill.character, out var character))
                    {
                        character = new Dictionary<string, SkillOverrideOld>();
                        skillOverrides[charSkill.character] = character;
                    }

                    if (!character.TryGetValue(charSkill.name, out var data))
                    {
                        data = new SkillOverrideOld();
                        character[charSkill.name] = data;
                    }

                    if (data.range == null) data.range = obj.fRange;
                    else typeof(SkillData).GetField("m_fRange", AccessTools.all).SetValue(obj, data.range.Value);

                    if (data.cooldown == null) data.cooldown = obj.m_fCooldown;
                    else obj.m_fCooldown = data.cooldown.Value;
                }
            }
        }
    }

    internal struct CharacterSkill
    {
        public string character;
        public string name;

        public string Combine()
        {
            if (name == "Heal") return "SkillHeal" + CharTo3Letters(character);
            if (name == "ThrowBody") return "SkillCarryThrow";
            return "Skill" + CharTo3Letters(character) + "Key" + name;
        }

        public CharacterSkill(string skill)
        {
            skill = skill.Substring(5);
            if (skill.StartsWith("Heal"))
            {
                name = "Heal";
                character = CharFrom3Letters(skill.Substring(4));
                return;
            }
            if (skill == "CarryThrow")
            {
                name = "ThrowBody";
                character = "Hector";
                return;
            }
            character = CharFrom3Letters(skill.Remove(3));
            name = skill.Substring(6);
        }

        static string CharFrom3Letters(string letters)
        {
            switch (letters.ToLower())
            {
                case "cop": return "Cooper";
                case "cpy": return "Cooper Young";
                case "kat": return "Kate";
                case "mcc": return "McCoy";
                case "tra": return "Hector";
                case "voo": return "Isabelle";
            }
            throw new ArgumentException("Invalid 3 letter character code: " + letters);
        }

        static string CharTo3Letters(string character)
        {
            switch (character.ToLower())
            {
                case "cooper": return "Cop";
                case "cooper young": return "Cpy";
                case "kate": return "Kat";
                case "mccoy": return "Mcc";
                case "hector": return "Tra";
                case "isabelle": return "Voo";
            }
            throw new ArgumentException("Invalid character: " + character);
        }
    }
}
