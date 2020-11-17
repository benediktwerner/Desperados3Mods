using BepInEx.Configuration;
using System.Linq;

namespace Desperados3Mods.ExtendedCheats
{
    static class Extensions
    {
        public static string ToJson(this int[] array)
        {
            return "[" + string.Join(", ", array.Select(val => val.ToString())) + "]";
        }

        public static int[] ToDifficultyArray(this int[] value, int[] valueDefault)
        {
            if (value == null || valueDefault == null) return valueDefault;
            if (value.Length == 4) return value;

            var result = new int[4];
            for (var i = 0; i < value.Length; i++) result[i] = value[i];
            for (var i = value.Length; i < 4; i++) result[i] = valueDefault[i];

            return result;
        }

        internal static ConfigEntry<ToggleableFloat> BindToggleableFloat(this ConfigFile config, string category, string description, ToggleableFloat initial)
        {
            return config.Bind(category, description, initial,
                new ConfigDescription(description, null,
                    new ConfigurationManagerAttributes
                    {
                        CustomDrawer = ToggleableFloat.Draw,
                        DefaultValue = initial,
                    }
                )
            );
        }

        internal static bool SetIfEnabled(this ConfigEntry<ToggleableFloat> entry, ref float value)
        {
            return entry.Value.SetIfEnabled(ref value);
        }
    }
}
