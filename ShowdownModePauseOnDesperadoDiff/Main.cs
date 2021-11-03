using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Desperados3Mods.ShowdownModePauseOnDesperadoDiff
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.showdownmodepauseondesperadodiff";
        public const string Name = "ShowdownModePauseOnDeperadoDiff";
        public const string Version = "1.0";

        public static BepInEx.Logging.ManualLogSource StaticLogger;

        public void Awake()
        {
            StaticLogger = Logger;
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }

    [HarmonyPatch(typeof(UIConfirmDialogDifficulty), "onDifficultyChanged")]
    class Hooks
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var index = 0;

            foreach (var instruction in instructions)
            {
                switch (index++)
                {
                    case 0 when instruction.ToString() == "ldarg.0 NULL": break;
                    case 1 when instruction.ToString() == "ldflda DifficultySettings UIConfirmDialogDifficulty::m_difficultySettings": break;
                    case 2 when instruction.ToString() == "ldfld Difficulty DifficultySettings::m_eDifficulty": break;
                    case 3 when instruction.ToString() == "ldc.i4.3 NULL":
                        instruction.opcode = OpCodes.Ldc_I4_8;
                        break;
                    default:
                        if (index < 4) index = 0;
                        break;
                }
                yield return instruction;
            }

            if (index < 4) Main.StaticLogger?.LogError("ERROR: Failed to find code to patch. This might be caused by an update of the game.");
        }
    }
}
