﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    public static class Commands
    {
        static bool show = false;

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
                DrawAdjustableInt("Max Health", character.m_charHealth.iHealthMax.ToString(),
                    () => character.ChangeMaxHealth(-1),
                    () => character.ChangeMaxHealth(+1)
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

            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        static MiCharacterInventory.ItemType GetAmmoType(PlayerSkill skill)
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

        static void ChangeMaxHealth(this MiCharacter character, int change)
        {
            character.m_charHealth.m_iHealthMaxOverride = character.m_charHealth.iHealthMax + change;

            var uiData = character.uiData;
            uiData.m_iHealthMax.value = character.m_charHealth.m_iHealthMaxOverride;

            var slotHandlerMouse = UnityEngine.Object.FindObjectOfType<UICharacterSlotHandlerMouse>();
            if (slotHandlerMouse != null)
            {
                var mouseSlot = slotHandlerMouse.arUICharacterSlots[uiData.m_iSlotIndexMouse.value] as UICharacterSlotMouse;
                if (mouseSlot != null)
                {
                    typeof(UICharacterSlotMouse).GetMethod("updateHealthBar", AccessTools.all).Invoke(mouseSlot, new object[] { true });
                }
            }

            var slotHandlerController = UnityEngine.Object.FindObjectOfType<UICharacterSlotHandlerController>();
            if (slotHandlerController != null)
            {
                var controllerSlot = slotHandlerController.arUICharacterSlots[uiData.m_iSlotIndexController.value] as UICharacterSlotWheel;
                if (controllerSlot != null)
                {
                    typeof(UICharacterSlotMouse).GetMethod("updateHealthBar", AccessTools.all).Invoke(controllerSlot, new object[] { true });
                }
            }
        }
    }
}