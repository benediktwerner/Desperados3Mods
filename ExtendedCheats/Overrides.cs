using BepInEx;
using BepInEx.Configuration;
using SimpleJSON;
using System;
using System.IO;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    class Overrides
    {
        const string CONFIG_FILENAME = "overrides.json";
        static string ConfigPath => Path.Combine(Paths.ConfigPath, CONFIG_FILENAME);

        readonly CharacterOverride[] CharacterOverrides = CharacterOverride.GetAll();

        bool show = false;
        string charToShow = null;

        readonly ConfigEntry<bool> configEnabled;

        internal Overrides(ConfigFile config)
        {
            configEnabled = config.Bind("General", "Enable Overrides", false,
                new ConfigDescription(
                    "Enable overriding skills and character attributes. Use the in-game settings menu provided by BepInEx.ConfigurationManager or edit overrides.json to modify values (enter a level to generate the file).",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        Category = "4. Overrides",
                        CustomDrawer = DrawOverrides,
                        HideDefaultButton = true,
                        HideSettingName = true
                    }
                )
            );
            Load();
        }

        void DrawOverrides(ConfigEntryBase _)
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
            configEnabled.Value = GUILayout.Toggle(configEnabled.Value, "Apply overrides on startup");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset all overrides", GUILayout.Width(200)))
            {
                CharacterOverrides.ForEach(o => o.Reset());
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload overrides.json"))
            {
                Load();
                Apply();
            }
            if (GUILayout.Button("Apply changes and Save to overrides.json"))
            {
                Apply();
                Store();
            }
            GUILayout.EndHorizontal();

            CharacterOverrides.ForEach(o => o.Draw(ref charToShow));

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
                var config = JSON.Parse(File.ReadAllText(ConfigPath))?.AsObject;
                if (config == null)
                {
                    Store();
                    return;
                }

                foreach (var character in CharacterOverrides)
                {
                    character.FromJson(config[character.name]?.AsObject);
                }
            }
            catch (Exception e)
            {
                Main.StaticLogger.LogError("Error when loading overrides.json:");
                Main.StaticLogger.LogError(e);
                Store();
            }
        }

        void Store()
        {
            var content = "// Delete values to reset them\n";
            content += "{\n";
            var firstChar = true;

            foreach (var character in CharacterOverrides)
            {
                if (firstChar) firstChar = false;
                else content += ",\n";

                content += "  \"" + character.name + "\": " + character.ToJson("  ");
            }
            content += "\n}";

            File.WriteAllText(ConfigPath, content);
        }

        internal void OnLevelLoad()
        {
            if (Main.configEnabled.Value && configEnabled.Value && SceneStatistics.instance.iLoadCount == 0) Apply(start: true);
        }

        void Apply(bool start = false)
        {
            var gameInput = MiGameInput.instance;
            if (gameInput == null) return;

            foreach (var character in gameInput.lPlayerCharacter)
            {
                foreach (var characterOverride in CharacterOverrides)
                {
                    if (character.m_eCharacter == characterOverride.characterType)
                    {
                        characterOverride.Apply(character, start);
                    }
                }
            }
        }
    }
}
