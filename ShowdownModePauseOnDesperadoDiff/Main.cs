using static UnityModManagerNet.UnityModManager;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ShowdownModePauseOnDesperadoDiff
{
    public class Main
    {
        public static ModEntry.ModLogger Logger;

        static void Load(ModEntry modEntry)
        {
            Logger = modEntry.Logger;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(UIConfirmDialogDifficulty), "onDifficultyChanged")]
    class Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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

            if (index < 4) Main.Logger.Log("ERROR: Failed to find code to patch. This might be caused by an update of the game.");
        }
    }
}
