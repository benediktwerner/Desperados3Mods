using System.Collections.Generic;
using UnityEngine;

namespace Desperados3Mods.ExtendedCheats
{
    class CooperOverrides : CharacterSkillOverrides
    {
        public override string Name => "Cooper";

        public SkillOverride Kill = new SkillOverride(
          name: "Kill",
          cooldown: 0
        );
        public SkillOverride Stun = new SkillOverride(
          name: "Stun",
          cooldown: 0
        );
        public SkillOverride ThrowKnife = new SkillOverride(
          name: "ThrowKnife",
          range: 12,
          cooldown: 8
        );
        public SkillOverride WhistleStone = new SkillOverride(
          name: "WhistleStone",
          range: 12,
          cooldown: 9
        );
        public SkillOverride GunLeft = new SkillOverride(
          name: "GunLeft",
          range: 17,
          cooldown: 12
        );
        public SkillOverride GunRight = new SkillOverride(
          name: "GunRight",
          range: 17,
          cooldown: 12
        );
    }

    class CooperYoungOverrides : CharacterSkillOverrides
    {
        public override string Name => "Young Cooper";

        public SkillOverride ThrowKnife = new SkillOverride(
          name: "ThrowKnife",
          range: 8,
          cooldown: 8
        );
    }

    class HectorOverrides : CharacterSkillOverrides
    {
        public override string Name => "Hector";

        public SkillOverride Kill = new SkillOverride(
          name: "Kill",
          cooldown: 0
        );
        public SkillOverride Stun = new SkillOverride(
          name: "Stun",
          cooldown: 0
        );
        public SkillOverride Trap = new SkillOverride(
          name: "Trap",
          cooldown: 0
        );
        public SkillOverride Whistle = new SkillOverride(
          name: "Whistle",
          cooldown: 6
        );
        public SkillOverride Gun = new SkillOverride(
          name: "Gun",
          range: 12,
          cooldown: 18
        );
        public SkillOverride Heal = new SkillOverride(
          name: "Heal",
          cooldown: 20
        );
        public SkillOverride ThrowBody = new SkillOverride(
          name: "ThrowBody",
          range: 5,
          cooldown: 0
        );
    }

    class McCoyOverrides : CharacterSkillOverrides
    {
        public override string Name => "McCoy";

        public SkillOverride Kill = new SkillOverride(
          name: "Kill",
          cooldown: 0
        );
        public SkillOverride Stun = new SkillOverride(
          name: "Stun",
          cooldown: 0
        );
        public SkillOverride Gun = new SkillOverride(
          name: "Gun",
          range: 55,
          cooldown: 6
        );
        public SkillOverride Stunbox = new SkillOverride(
          name: "Stunbox",
          range: 5,
          cooldown: 0
        );
        public SkillOverride StunGrenade = new SkillOverride(
          name: "StunGrenade",
          range: 8,
          cooldown: 6
        );
        public SkillOverride Heal = new SkillOverride(
          name: "Heal",
            cooldown: 20
        );
    }

    class IsabelleOverrides : CharacterSkillOverrides
    {
        public override string Name => "Isabelle";

        public SkillOverride Kill = new SkillOverride(
          name: "Kill",
          cooldown: 0
        );
        public SkillOverride Stun = new SkillOverride(
          name: "Stun",
          cooldown: 0
        );
        public SkillOverride Connect = new SkillOverride(
          name: "Connect",
          range: 10,
          cooldown: 0
        );
        public SkillOverride Control = new SkillOverride(
          name: "Control",
          range: 10,
          cooldown: 8
        );
        public SkillOverride Pet = new SkillOverride(
          name: "Pet",
          range: 14,
          cooldown: 0
        );
        public SkillOverride Heal = new SkillOverride(
          name: "Heal",
          cooldown: 10
        );
    }

    class KateOverrides : CharacterSkillOverrides
    {
        public override string Name => "Kate";

        public SkillOverride Stun = new SkillOverride(
          name: "Stun",
          cooldown: 0
        );
        public SkillOverride Blind = new SkillOverride(
          name: "Blind",
          range: 12,
          cooldown: 10
        );
        public SkillOverride Gun = new SkillOverride(
          name: "Gun",
          range: 9,
          cooldown: 6
        );
        public SkillOverride Disguise = new SkillOverride(
          name: "Disguise",
          cooldown: 3
        );
        public SkillOverride Distract = new SkillOverride(
          name: "Distract",
          range: 1.2f,
          cooldown: 1
        );
        public SkillOverride Follow = new SkillOverride(
          name: "Follow",
          cooldown: 10
        );
    }

    abstract class CharacterSkillOverrides
    {
        public abstract string Name { get; }

        public IEnumerable<SkillOverride> SkillOverrides()
        {
            foreach (var field in GetType().GetFields())
            {
                if (field.GetValue(this) is SkillOverride fieldOverride) yield return fieldOverride;
            }
        }

        public void Draw(ref string charToShow)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Name, Main.SkinBold, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            var showChar = charToShow == Name;
            if (GUILayout.Button(showChar ? "Hide" : "Show", GUILayout.Width(200)))
            {
                showChar = !showChar;
                charToShow = showChar ? Name : null;
            }
            GUILayout.EndHorizontal();
            if (!showChar) return;

            GUILayout.BeginVertical("box");
            foreach (var o in SkillOverrides()) o.Draw();
            GUILayout.EndVertical();
        }

        public void Reset()
        {
            foreach (var o in SkillOverrides()) o.Reset();
        }

        public void Apply(Dictionary<string, PlayerSkillData> data)
        {
            foreach (var o in SkillOverrides())
            {
                var name = new CharacterSkillName(Name, o.name).Combined;
                if (data.TryGetValue(name, out var value)) o.Apply(value);
            }
        }

        public void FromJson(SimpleJSON.JSONClass json)
        {
            if (json == null) return;
            foreach (var o in SkillOverrides()) o.FromJson(json[o.name]?.AsObject);
        }

        public string ToJson(string indent)
        {
            var indentInner = indent + "  ";
            var firstSkill = true;
            string content = "{\n";
            foreach (var skill in SkillOverrides())
            {
                if (firstSkill) firstSkill = false;
                else content += ",\n";

                content += indentInner + "\"" + skill.name + "\": " + skill.ToJson(indentInner);
            }
            content += indent + "}";
            return content;
        }
    }
}
