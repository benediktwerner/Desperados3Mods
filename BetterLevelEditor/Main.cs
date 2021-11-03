using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MiCoreServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace Desperados3Mods.BetterLevelEditor
{
    [BepInPlugin(GUID, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "de.benediktwerner.desperados3.betterleveleditor";
        public const string Name = "BetterLevelEditor";
        public const string Version = "1.0";

        internal static ManualLogSource StaticLogger;

        private static bool isSpawning = false;
        private static MiCharacter.CharacterType optionUsableByCharType;
        private static MiCharacter.CharacterType[] playerChars =
        {
            MiCharacter.CharacterType.Cooper,
            MiCharacter.CharacterType.McCoy,
            MiCharacter.CharacterType.Trapper,
            MiCharacter.CharacterType.Kate,
            MiCharacter.CharacterType.Voodoo,
        };

        public ConfigEntry<KeyboardShortcut> ShowHotkey { get; private set; }
        public ConfigEntry<KeyboardShortcut> SpawnHotkey { get; private set; }
        private ConfigEntry<Favorites> FavoritesConfig { get; set; }
        private Favorites favorites;

        private SpawnCode[] SpawnCodes;
        private Dictionary<int, string> SpawnCodesDict = new Dictionary<int, string>();

        public Rect WindowRect;

        private string error = null;
        private string search = "";
        private int selectedCode = -1;
        private Vector2 scrollPosition;

        private readonly int _windowId;
        private GameObject _clickBlockerCanvas;
        private RectTransform _clickBlockerRect;

        public Main()
        {
            _windowId = GetHashCode();
            optionUsableByCharType = playerChars.Aggregate((a, b) => a | b);
        }

        private bool _show = false;
        public bool Show
        {
            get => _show; set
            {
                if (Show != value)
                {
                    _show = value;
                    if (value)
                    {
                        WindowRect = new Rect(10, 30, 400, Screen.height - 555);

                        _clickBlockerCanvas = new GameObject("BetterLevelEditor Click Blocker", typeof(Canvas), typeof(GraphicRaycaster));
                        var canvas = _clickBlockerCanvas.GetComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvas.sortingOrder = short.MaxValue;
                        DontDestroyOnLoad(_clickBlockerCanvas);
                        var panel = new GameObject("BetterLevelEditor Click Blocker Image", typeof(Image));
                        panel.transform.SetParent(_clickBlockerCanvas.transform);
                        _clickBlockerRect = panel.GetComponent<RectTransform>();
                        UpdateClickBlockerSize();
                        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
                    }
                    else
                    {
                        _clickBlockerRect = null;
                        Destroy(_clickBlockerCanvas);
                    }
                }
            }
        }

        private void Awake()
        {
            StaticLogger = Logger;

            TomlTypeConverter.AddConverter(typeof(Favorites), new TypeConverter
            {
                ConvertToObject = (str, type) =>
                {
                    if (str[0] != '[' || str[str.Length - 1] != ']') throw new Exception("Invalid format. Expected array.");
                    if (str.Length == 2) return new Favorites(new List<int>());
                    return new Favorites(str.Substring(1, str.Length - 2).Split(',').Select(int.Parse).ToList());
                },
                ConvertToString = (obj, type) =>
                {
                    return "[" + string.Join(", ", ((Favorites)obj).List) + "]";
                }
            });

            ShowHotkey = Config.Bind("General", "Open/close editor", new KeyboardShortcut(KeyCode.F2));
            SpawnHotkey = Config.Bind("General", "Spawn selected entitty", new KeyboardShortcut(KeyCode.F3));
            FavoritesConfig = Config.Bind("General", "Favorites", new Favorites(new List<int>()), new ConfigDescription("Favorites", null, new ConfigurationManagerAttributes
            {
                CustomDrawer = _ => GUILayout.Label("Use the editor window to add or remove favorites")
            }));
            FavoritesConfig.SettingChanged += (sender, args) => favorites = FavoritesConfig.Value;
            favorites = FavoritesConfig.Value;

            try
            {
                var path = Path.Combine(Path.GetDirectoryName(Info.Location), "spawn_codes.txt");
                SpawnCodes = File.ReadAllLines(path).Select(SpawnCode.Parse).ToArray();
                foreach (var code in SpawnCodes) SpawnCodesDict[code.Code] = code.Name;
            }
            catch (Exception e)
            {
                error = "Failed to load 'spawn_codes.txt': " + e;
                Logger.LogError(error);
            }

            Harmony.CreateAndPatchAll(typeof(Patch));
        }

        private void Update()
        {
            if (Input.GetKeyDown(ShowHotkey.Value.MainKey))
            {
                Show = !Show;
            }

            if (Input.GetKeyDown(SpawnHotkey.Value.MainKey) && selectedCode != -1 && MiGameInput.instance.iPlayerCharacterCount > 0)
            {
                var inputs = AccessTools.Field(typeof(UIManager), "m_dicPlayerInputs").GetValue(UIManager.instance) as Dictionary<GameUser, MiPlayerInput>;
                foreach (var input in inputs.Values)
                {
                    isSpawning = true;
                    input.devSpawn(selectedCode / 100, selectedCode % 100);
                    isSpawning = false;
                    break;
                }
            }
        }

        private void OnGUI()
        {
            if (!Show) return;

            WindowRect = GUILayout.Window(_windowId, WindowRect, DisplayWindow, "Better level Editor");
        }

        private void DisplayWindow(int id)
        {
            if (error != null)
            {
                GUILayout.Label(error);
                return;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            search = GUILayout.TextField(search);
            GUILayout.EndHorizontal();

            if (favorites.List.Count != 0)
            {
                var removeFav = -1;
                GUILayout.BeginVertical("box");
                foreach (var fav in favorites.List)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Toggle(selectedCode == fav, $"{fav:D4} {SpawnCodesDict[fav]}", GUILayout.Width(300))) selectedCode = fav;
                    if (GUILayout.Button("X")) removeFav = fav;
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                if (removeFav != -1) RemoveFavorite(removeFav);
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var code in search.Length > 2 ? SpawnCodes.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) : SpawnCodes)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(selectedCode == code.Code, $"{code.Code:D4} {code.Name}", GUILayout.Width(300))) selectedCode = code.Code;
                if (!IsFavorite(code.Code) && GUILayout.Button("<3"))
                {
                    AddFavorite(code.Code);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            DrawOptions();
            GUILayout.EndVertical();

            GUI.DragWindow();
            UpdateClickBlockerSize();
        }

        private void DrawOptions()
        {
            // TODO: Enemy health?
            if (selectedCode == 6118)
            {
                GUILayout.BeginVertical("box");
                foreach (var c in playerChars)
                {
                    var isActive = (optionUsableByCharType & c) != 0;
                    var shouldActive = GUILayout.Toggle(isActive, c.ToNiceString());
                    if (shouldActive != isActive) optionUsableByCharType ^= c;
                }
                GUILayout.EndVertical();
            }
        }

        private void UpdateClickBlockerSize()
        {
            if (_clickBlockerRect == null) return;
            _clickBlockerRect.anchorMin = new Vector2(WindowRect.x / Screen.width, 1 - WindowRect.y / Screen.height);
            _clickBlockerRect.anchorMax = new Vector2(WindowRect.xMax / Screen.width, 1 - WindowRect.yMax / Screen.height);
            _clickBlockerRect.offsetMin = Vector2.zero;
            _clickBlockerRect.offsetMax = Vector2.zero;
            _clickBlockerRect.offsetMax = Vector2.zero;
        }

        private void AddFavorite(int fav)
        {
            if (!favorites.Set.Add(fav)) return;
            favorites.List.Add(fav);
            favorites.List.Sort();
            favorites.Version += 1;
            FavoritesConfig.Value = favorites;
        }
        private void RemoveFavorite(int fav)
        {
            if (!favorites.Set.Remove(fav)) return;
            favorites.List.Remove(fav);
            favorites.Version += 1;
            FavoritesConfig.Value = favorites;
        }
        private bool IsFavorite(int code)
        {
            return favorites.Set.Contains(code);
        }

        public static void PostSpawnUsable(GameObject go)
        {
            if (!isSpawning) return;
            MiUsable usable = go.GetComponent<MiUsable>();
            usable.m_eCanUsedBy = optionUsableByCharType;
        }
    }

    [HarmonyPatch]
    class Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MiGameInput), nameof(MiGameInput.devSpawnUsable))]
        public static IEnumerable<CodeInstruction> MiGameInput_devSpawnUsable_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Ret))
                .Repeat(matcher => matcher
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldloc_1),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Main), nameof(Main.PostSpawnUsable))))
                    .Advance(2)
                )
                .InstructionEnumeration();
        }
    }

    public struct SpawnCode
    {
        public int Code;
        public string Name;

        public static SpawnCode Parse(string s)
        {
            try
            {
                var parts = s.Split('\t');
                return new SpawnCode()
                {
                    Code = int.Parse(parts[0]),
                    Name = parts[1]
                };
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " on line: " + s);
            }
        }
    }

    internal struct Favorites
    {
        public List<int> List;
        public HashSet<int> Set;
        public int Version;
        public Favorites(List<int> favorites)
        {
            Version = 0;
            List = favorites;
            Set = new HashSet<int>(List);
        }
    }

    static class Extensions
    {
        public static String ToNiceString(this MiCharacter.CharacterType c)
        {
            switch (c)
            {
                case MiCharacter.CharacterType.Cooper: return "Cooper";
                case MiCharacter.CharacterType.McCoy: return "McCoy";
                case MiCharacter.CharacterType.Trapper: return "Hector";
                case MiCharacter.CharacterType.Kate: return "Kate";
                case MiCharacter.CharacterType.Voodoo: return "Isabelle";
                default: return "Non-player character";
            }
        }
    }
}
