using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace Desperados3Mods.D1Textures
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.d1textures";
        public const string Name = "D1 Textures";
        public const string Version = "1.0.0";

        static ConfigEntry<bool> Enabled;

        void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true);

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        static class Hooks
        {
            static float lastReset = 0f;

            [HarmonyPatch(typeof(MiSaveable), "finishedLoading")]
            [HarmonyPostfix]
            public static void MiSaveable_finishedLoading()
            {
                if (!Enabled.Value || Time.time - lastReset < 2f) return;
                lastReset = Time.time;

                Debug.Log("Changing textures");

                var gameInput = MiGameInput.instance;
                if (gameInput == null) return;

                Debug.Log("Have game input");

                foreach (var c in gameInput.lPlayableCharacter)
                {
                    if (c.m_eCharacter == MiCharacter.CharacterType.Cooper)
                    {
                        var visRoot = FindStartsWith(c.transform, "MiVis")?.Find("ply_gnc_cooper_00");
                        if (visRoot == null) continue;

                        AssignTexture("coop", visRoot.Find("ply_gnc_cooper_00"), visRoot.Find("ply_gnc_cooper_hat_00"));
                        AssignTexture("coop_coat", visRoot.Find("ply_gnc_cooper_coat_00"));
                    }
                    else if (c.m_eCharacter == MiCharacter.CharacterType.Kate)
                    {
                        var visRoot = FindStartsWith(c.transform, "MiVis")?.Find("ply_gnc_kate_00");
                        if (visRoot == null) continue;

                        AssignTexture("kat", visRoot.Find("ply_gnc_kate_00"));
                        AssignTexture("kat_cloth", visRoot.Find("ply_gnc_kate_cloth_00"));
                        AssignTexture("kat_hat", visRoot.Find("ply_gnc_kate_hat_00"));
                    }
                }
            }

            static Transform FindStartsWith(Transform t, string n)
            {
                var count = t.childCount;
                for (var i = 0; i < count; i++)
                {
                    var child = t.GetChild(i);
                    if (child.name.StartsWith(n)) return child;
                }
                return null;
            }

            static void AssignTexture(string name, params Transform[] ts)
            {
                string path = $"Desperados III_Data/Resources/Textures/{name}.png";
                Texture2D texture = new Texture2D(2, 2);
                ImageConversion.LoadImage(texture, File.ReadAllBytes(path));
                foreach (var t in ts)
                {
                    var renderer = t?.GetComponent<SkinnedMeshRenderer>();
                    if (renderer == null) continue;
                    renderer.material.mainTexture = texture;
                }
            }
        }
    }
}
