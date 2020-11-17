using System;

namespace Desperados3Mods.ExtendedCheats
{
    struct CharacterSkillName
    {
        public string character;
        public string skill;

        public string Combined {
            get {
                if (skill == "Heal") return "SkillHeal" + CharTo3Letters(character);
                if (skill == "ThrowBody") return "SkillCarryThrow";
                return "Skill" + CharTo3Letters(character) + "Key" + skill;
            }
        }

        public CharacterSkillName(string character, string skill)
        {
            this.character = character;
            this.skill = skill;
        }

        public CharacterSkillName(string combinedName)
        {
            combinedName = combinedName.Substring(5);
            if (combinedName.StartsWith("Heal"))
            {
                skill = "Heal";
                character = CharFrom3Letters(combinedName.Substring(4));
                return;
            }
            if (combinedName == "CarryThrow")
            {
                skill = "ThrowBody";
                character = "Hector";
                return;
            }
            character = CharFrom3Letters(combinedName.Remove(3));
            skill = combinedName.Substring(6);
        }

        static string CharFrom3Letters(string letters)
        {
            switch (letters.ToLower())
            {
                case "cop": return "Cooper";
                case "cpy": return "Young Cooper";
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
                case "young cooper": return "Cpy";
                case "kate": return "Kat";
                case "mccoy": return "Mcc";
                case "hector": return "Tra";
                case "isabelle": return "Voo";
            }
            throw new ArgumentException("Invalid character: " + character);
        }
    }
}
