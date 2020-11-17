using HarmonyLib;
using System.Globalization;
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

        public SkillOverride(string name, float? range = null, float? cooldown = null)
        {
            this.name = name;
            this.range = rangeDefault = range;
            this.cooldown = cooldownDefault = cooldown;
        }

        public void Draw()
        {
            range = DrawFloatField(name, "Range", range, rangeDefault);
            cooldown = DrawFloatField(name, "Cooldown", cooldown, cooldownDefault);
        }

        public void Reset()
        {
            range = rangeDefault;
            cooldown = cooldownDefault;
        }

        public void Apply(PlayerSkillData data)
        {
            if (rangeDefault != null) typeof(SkillData).GetField("m_fRange", AccessTools.all).SetValue(data, range ?? rangeDefault.Value);
            if (cooldownDefault != null) data.m_fCooldown = cooldown ?? cooldownDefault.Value;
        }

        public void FromJson(SimpleJSON.JSONClass json)
        {
            Reset();
            if (json == null) return;
            range = json["range"]?.AsFloat ?? rangeDefault;
            cooldown = json["cooldown"]?.AsFloat ?? cooldownDefault;
        }

        public string ToJson(string indent)
        {
            var indentInner = indent + "  ";
            string content = "{\n";
            if (rangeDefault != null) content += indentInner + "\"range\": " + (range ?? rangeDefault.Value).ToString(CultureInfo.InvariantCulture) + ",\n";
            if (cooldownDefault != null) content += indentInner + "\"cooldown\": " + (cooldown ?? cooldownDefault.Value).ToString(CultureInfo.InvariantCulture) + "\n";
            content += indent + "}";
            return content;
        }

        float? DrawFloatField(string mainLabel, string subLabel, float? value, float? valueDefault)
        {
            if (valueDefault == null) return null;

            GUILayout.BeginHorizontal();
            GUILayout.Label(mainLabel, GUILayout.Width(100));
            GUILayout.Label(subLabel, GUILayout.Width(120));

            var strVal = GUILayout.TextField(value?.ToString("f2", CultureInfo.InvariantCulture) ?? "", Main.SkinFloatField, GUILayout.Width(100));
            float newVal = valueDefault.Value;
            if (!string.IsNullOrWhiteSpace(strVal) && float.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValFloat))
            {
                newVal = newValFloat;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset", GUILayout.Width(100)))
            {
                newVal = valueDefault.Value;
            }

            GUILayout.EndHorizontal();

            return newVal;
        }
    }
}
