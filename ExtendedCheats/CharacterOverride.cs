using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    class CharacterOverride
    {
        public readonly string name;
        public readonly string internalName;
        public readonly MiCharacter.CharacterType characterType;
        public int[] maxHealth;
        public readonly int[] maxHealthDefault;
        public readonly SkillOverride[] skillOverrides;

        public CharacterOverride(string name, string internalName, MiCharacter.CharacterType characterType, int[] maxHealth, params SkillOverride[] skillOverrides)
        {
            this.name = name;
            this.internalName = internalName;
            this.characterType = characterType;
            this.skillOverrides = skillOverrides;
            this.maxHealth = maxHealth;
            maxHealthDefault = maxHealth.Clone() as int[];
        }

        public void Draw(ref string charToShow)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, Main.SkinBold, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            var showChar = charToShow == name;
            if (GUILayout.Button(showChar ? "Hide" : "Show", GUILayout.Width(200)))
            {
                showChar = !showChar;
                charToShow = showChar ? name : null;
            }
            GUILayout.EndHorizontal();
            if (!showChar) return;

            GUILayout.BeginVertical("box");
            SkillOverride.DrawIntArrayField("Max Health", "", ref maxHealth, maxHealthDefault);
            foreach (var o in skillOverrides) o.Draw();
            GUILayout.EndVertical();
        }

        public void Reset()
        {
            foreach (var o in skillOverrides) o.Reset();
            maxHealth = maxHealthDefault.Clone() as int[];
        }

        public void Apply(MiCharacter character, bool start)
        {
            foreach (var o in skillOverrides)
            {
                var combinedName = new CharacterSkillName(name, o.name).Combined;
                SetMaxHealth(character, maxHealth[(int)MissionSetupSettings.difficultySettings.m_ePlayerHealth]);
                foreach (var skill in character.controller.lSkills)
                {
                    if (!(skill is PlayerSkill playerSkill)) continue;
                    if (combinedName == skill.m_skillData.name)
                    {
                        o.Apply(playerSkill, start);
                    }
                }
            }
        }

        public void FromJson(SimpleJSON.JSONClass json)
        {
            if (json == null) return;
            maxHealth = (json["maxHealth"]?.AsArray.Childs.Select(c => c.AsInt).ToArray()).ToDifficultyArray(maxHealthDefault);
            foreach (var o in skillOverrides) o.FromJson(json[o.name]?.AsObject);
        }

        public string ToJson(string indent)
        {
            var indentInner = indent + "  ";
            var firstSkill = true;
            string content = "{\n";
            content += indentInner + "\"maxHealth\": " + (maxHealth ?? maxHealthDefault).ToJson() + ",\n";
            foreach (var skill in skillOverrides)
            {
                if (firstSkill) firstSkill = false;
                else content += ",\n";

                content += indentInner + "\"" + skill.name + "\": " + skill.ToJson(indentInner);
            }
            content += indent + "}";
            return content;
        }

        static void SetMaxHealth(MiCharacter character, int value)
        {
            character.m_charHealth.m_iHealthMaxOverride = value;

            var uiData = character.uiData;
            if (uiData == null) return;

            uiData.m_iHealthCurrent.value = character.m_charHealth.m_iHealthMaxOverride;
            uiData.m_iHealthMax.value = character.m_charHealth.m_iHealthMaxOverride;

            //var slotHandlerMouse = Object.FindObjectOfType<UICharacterSlotHandlerMouse>();
            //if (slotHandlerMouse != null)
            //{
            //    var mouseSlot = slotHandlerMouse.arUICharacterSlots[uiData.m_iSlotIndexMouse.value] as UICharacterSlotMouse;
            //    if (mouseSlot != null)
            //    {
            //        typeof(UICharacterSlotMouse).GetMethod("updateHealthBar", AccessTools.all).Invoke(mouseSlot, new object[] { true });
            //    }
            //}

            //var slotHandlerController = Object.FindObjectOfType<UICharacterSlotHandlerController>();
            //if (slotHandlerController != null)
            //{
            //    var controllerSlot = slotHandlerController.arUICharacterSlots[uiData.m_iSlotIndexController.value] as UICharacterSlotWheel;
            //    if (controllerSlot != null)
            //    {
            //        typeof(UICharacterSlotWheel).GetMethod("updateHealthBar", AccessTools.all).Invoke(controllerSlot, new object[] { true });
            //    }
            //}
        }

        public static CharacterOverride[] GetAll()
        {
            return new CharacterOverride[] {
                new CharacterOverride("Cooper", "cooper", MiCharacter.CharacterType.Cooper,
                    new int[]{4,3,2,2},
                    new SkillOverride(
                      name: "Kill",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Stun",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "ThrowKnife",
                      range: 12,
                      cooldown: 8
                    ),
                    new SkillOverride(
                      name: "WhistleStone",
                      range: 12,
                      cooldown: 9
                    ),
                    new SkillOverride(
                      name: "GunLeft",
                      range: 17,
                      cooldown: 12,
                      maxAmmo: 8,
                      startingAmmo: new int[]{8,8,6,4}
                    ),
                    new SkillOverride(
                      name: "GunRight",
                      range: 17,
                      cooldown: 12
                    )
                ),
                new CharacterOverride("Hector", "trapper", MiCharacter.CharacterType.Trapper,
                    new int[]{6,5,4,4},
                    new SkillOverride(
                      name: "Kill",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Stun",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Trap",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Whistle",
                      cooldown: 6
                    ),
                    new SkillOverride(
                      name: "Gun",
                      range: 12,
                      cooldown: 18,
                      maxAmmo: 4,
                      startingAmmo: new int[]{4,3,2,2}
                    ),
                    new SkillOverride(
                      name: "Heal",
                      cooldown: 20
                    ),
                    new SkillOverride(
                      name: "ThrowBody",
                      range: 5,
                      cooldown: 0
                    )
                ),
                new CharacterOverride("McCoy", "mccoy", MiCharacter.CharacterType.McCoy,
                    new int[]{4,3,2,2},
                    new SkillOverride(
                      name: "Kill",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Stun",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Gun",
                      range: 55,
                      cooldown: 6,
                      maxAmmo: 5,
                      startingAmmo: new int[]{5,4,3,2}
                    ),
                    new SkillOverride(
                      name: "Stunbox",
                      range: 5,
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "StunGrenade",
                      range: 8,
                      cooldown: 6,
                      maxAmmo: 2,
                      startingAmmo: new int[]{1,1,1,1 }
                    ),
                    new SkillOverride(
                      name: "Heal",
                        cooldown: 20
                    )
                ),
                new CharacterOverride("Isabelle", "voodoo", MiCharacter.CharacterType.Voodoo,
                    new int[]{4,3,2,2},
                    new SkillOverride(
                      name: "Kill",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Stun",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Connect",
                      range: 10,
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Control",
                      range: 10,
                      cooldown: 8,
                      maxAmmo: 5,
                      startingAmmo: new int[]{4,3,3,3}
                    ),
                    new SkillOverride(
                      name: "Pet",
                      range: 14,
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Heal",
                      cooldown: 10
                    )
                ),
                new CharacterOverride("Kate", "kate", MiCharacter.CharacterType.Kate,
                    new int[]{4,3,2,2},
                    new SkillOverride(
                      name: "Stun",
                      cooldown: 0
                    ),
                    new SkillOverride(
                      name: "Blind",
                      range: 12,
                      cooldown: 10
                    ),
                    new SkillOverride(
                      name: "Gun",
                      range: 9,
                      cooldown: 6,
                      maxAmmo: 5,
                      startingAmmo: new int[]{5,4,3,2}
                    ),
                    new SkillOverride(
                      name: "Disguise",
                      cooldown: 3
                    ),
                    new SkillOverride(
                      name: "Distract",
                      range: 1.2f,
                      cooldown: 1
                    ),
                    new SkillOverride(
                      name: "Follow",
                      cooldown: 10
                    )
                )
            };
        }
    }
}
