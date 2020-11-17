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

        readonly CooperOverrides cooperOverrides = new CooperOverrides();
        readonly CooperYoungOverrides cooperYoungOverrides = new CooperYoungOverrides();
        readonly McCoyOverrides mcCoyOverrides = new McCoyOverrides();
        readonly HectorOverrides hectorOverrides = new HectorOverrides();
        readonly KateOverrides kateOverrides = new KateOverrides();
        readonly IsabelleOverrides isabelleOverrides = new IsabelleOverrides();

        CharacterSkillOverrides[] Overrides => new CharacterSkillOverrides[]{
            cooperOverrides,
            cooperYoungOverrides,
            mcCoyOverrides,
            hectorOverrides,
            kateOverrides,
            isabelleOverrides
        };

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
                Overrides.ForEach(o => o.Reset());
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload skills.json"))
            {
                Load();
                Apply();
            }
            if (GUILayout.Button("Apply changes and Save to skills.json"))
            {
                Apply();
                Store();
            }
            GUILayout.EndHorizontal();

            Overrides.ForEach(o => o.Draw(ref charToShow));

            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                Store();
                return;
            }

            try
            {
                var config = JSON.Parse(File.ReadAllText(ConfigPath)).AsObject;

                foreach (var character in Overrides)
                {
                    character.FromJson(config[character.Name]?.AsObject);
                }
            }
            catch (Exception e)
            {
                Main.StaticLogger.LogError("Error when loading skills.json:");
                Main.StaticLogger.LogError(e);
            }
        }

        void Store()
        {
            var content = "// Delete values to reset them\n";
            content += "{\n";
            var firstChar = true;

            foreach (var character in Overrides)
            {
                if (firstChar) firstChar = false;
                else content += ",\n";

                content += "  \"" + character.Name + "\": " + character.ToJson("  ");
            }
            content += "\n}";

            File.WriteAllText(ConfigPath, content);
        }

        internal void OnLevelLoad()
        {
            Apply();
        }

        void Apply()
        {
            var objs = new Dictionary<string, PlayerSkillData>();
            foreach (var obj in Resources.FindObjectsOfTypeAll<PlayerSkillData>()) objs[obj.name] = obj;
            Overrides.ForEach(o => o.Apply(objs));
        }
    }
}
