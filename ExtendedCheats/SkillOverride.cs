using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    class SkillOverride
    {
        public string name;

        float? range;
        readonly float? rangeDefault;

        float? cooldown;
        readonly float? cooldownDefault;

        int[] startingAmmo;
        readonly int[] startingAmmoDefault;

        int? maxAmmo;
        readonly int? maxAmmoDefault;

        public SkillOverride(string name, float? range = null, float? cooldown = null, int? maxAmmo = null, int[] startingAmmo = null)
        {
            this.name = name;
            this.range = rangeDefault = range;
            this.cooldown = cooldownDefault = cooldown;
            this.maxAmmo = maxAmmoDefault = maxAmmo;
            this.startingAmmo = startingAmmo;
            startingAmmoDefault = startingAmmo?.Clone() as int[];
        }

        public void Draw()
        {
            range = DrawFloatField(name, "Range", range, rangeDefault);
            cooldown = DrawFloatField(name, "Cooldown", cooldown, cooldownDefault);
            maxAmmo = DrawIntField(name, "Max Ammo", maxAmmo, maxAmmoDefault);
            DrawIntArrayField(name, "Starting Ammo", ref startingAmmo, startingAmmoDefault);
        }

        public void Reset()
        {
            range = rangeDefault;
            cooldown = cooldownDefault;
            maxAmmo = maxAmmoDefault;
            startingAmmo = startingAmmoDefault?.Clone() as int[];
        }

        public void Apply(PlayerSkill skill, bool start)
        {
            if (rangeDefault != null) typeof(SkillData).GetField("m_fRange", AccessTools.all).SetValue(skill.m_skillData, range ?? rangeDefault.Value);
            if (cooldownDefault != null && skill.m_skillData is PlayerSkillData data) data.m_fCooldown = cooldown ?? cooldownDefault.Value;
            if (maxAmmoDefault != null) skill.character.m_charInventory.SetItemInfo(Commands.GetAmmoType(skill), (uint)(maxAmmo ?? maxAmmoDefault.Value));

            if (!start) return;

            if (startingAmmoDefault != null)
            {
                var i = (int)MissionSetupSettings.difficultySettings.m_eAmmunition;
                var ammoType = Commands.GetAmmoType(skill);
                var toAdd = (startingAmmo ?? startingAmmoDefault)[i] - (int)skill.character.m_charInventory.Count(ammoType);
                if (toAdd > 0) skill.character.m_charInventory.Insert(ammoType, (uint)toAdd, true);
                else if (toAdd < 0) skill.character.m_charInventory.Remove(ammoType, (uint)(-toAdd), true);
            }
        }

        public void FromJson(SimpleJSON.JSONClass json)
        {
            Reset();
            if (json == null) return;
            range = json["range"]?.AsFloat ?? rangeDefault;
            cooldown = json["cooldown"]?.AsFloat ?? cooldownDefault;
            maxAmmo = json["maxAmmo"]?.AsInt ?? maxAmmoDefault;
            startingAmmo = (json["startingAmmo"]?.AsArray.Childs.Select(c => c.AsInt).ToArray()).ToDifficultyArray(startingAmmoDefault);
        }

        public string ToJson(string indent)
        {
            var indentInner = indent + "  ";
            string content = "{\n";
            var lines = new List<string>();
            if (rangeDefault != null) lines.Add(indentInner + "\"range\": " + (range ?? rangeDefault.Value).ToString(CultureInfo.InvariantCulture));
            if (cooldownDefault != null) lines.Add(indentInner + "\"cooldown\": " + (cooldown ?? cooldownDefault.Value).ToString(CultureInfo.InvariantCulture));
            if (maxAmmoDefault != null) lines.Add(indentInner + "\"maxAmmo\": " + (maxAmmo ?? maxAmmoDefault.Value).ToString(CultureInfo.InvariantCulture));
            if (startingAmmoDefault != null) lines.Add(indentInner + "\"startingAmmo\": " + (startingAmmo ?? startingAmmoDefault).ToJson());
            content += string.Join(",\n", lines) + "\n" + indent + "}\n";
            return content;
        }

        internal static float? DrawFloatField(string mainLabel, string subLabel, float? value, float? valueDefault)
        {
            if (valueDefault == null) return null;
            DrawFieldHeader(mainLabel, subLabel);
            var strVal = GUILayout.TextField(value?.ToString("f2", CultureInfo.InvariantCulture) ?? "", Main.SkinFloatField, GUILayout.Width(100));
            var newVal = valueDefault.Value;
            if (!string.IsNullOrWhiteSpace(strVal) && float.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var newValFloat))
            {
                newVal = newValFloat;
            }
            DrawFieldFooter(ref newVal, valueDefault.Value);
            return newVal;
        }

        internal static int? DrawIntField(string mainLabel, string subLabel, int? value, int? valueDefault)
        {
            if (valueDefault == null) return null;
            DrawFieldHeader(mainLabel, subLabel);
            var strVal = GUILayout.TextField(value?.ToString() ?? "", Main.SkinFloatField, GUILayout.Width(100));
            var newVal = valueDefault.Value;
            if (!string.IsNullOrWhiteSpace(strVal) && int.TryParse(strVal, out var newValInt))
            {
                newVal = newValInt;
            }
            DrawFieldFooter(ref newVal, valueDefault.Value);
            return newVal;
        }

        internal static void DrawIntArrayField(string mainLabel, string subLabel, ref int[] value, int[] valueDefault)
        {
            if (valueDefault == null) return;
            DrawFieldHeader(mainLabel, subLabel);
            if (value == null) value = valueDefault.Clone() as int[];
            for (var i = 0; i < value.Length; i++)
            {
                var strVal = GUILayout.TextField(value[i].ToString(), Main.SkinFloatField, GUILayout.Width(30));
                if (!string.IsNullOrWhiteSpace(strVal) && int.TryParse(strVal, out var newValInt))
                {
                    value[i] = newValInt;
                }
                else value[i] = valueDefault[i];
            }
            DrawFieldFooter(ref value, valueDefault.Clone() as int[]);
        }

        internal static void DrawFieldHeader(string mainLabel, string subLabel)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(mainLabel, GUILayout.Width(100));
            GUILayout.Label(subLabel, GUILayout.Width(120));
        }

        internal static void DrawFieldFooter<T>(ref T value, T valueDefault)
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset", GUILayout.Width(100)))
            {
                value = valueDefault;
            }

            GUILayout.EndHorizontal();
        }
    }
}
