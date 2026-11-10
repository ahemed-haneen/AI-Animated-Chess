using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BattleChess.Core;
using Debug = UnityEngine.Debug;

namespace BattleChess.EditorTools
{
    /// <summary>One-click project setup: "Battle Chess" menu in the Unity menu bar.</summary>
    public static class BattleChessMenu
    {
        const string Root = "Assets/BattleChess";
        const string ScenePath = Root + "/Scenes/BattleChess.unity";
        const string ThemePath = Root + "/Resources/Themes/PiratesVsBritish.asset";
        const string LitPath = Root + "/Materials/BC_Lit.mat";
        const string SpritePath = Root + "/Materials/BC_Sprite.mat";

        [MenuItem("Battle Chess/1. Create Game Scene", priority = 1)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Resources/Themes");

            var lit = EnsureMaterial(LitPath, "Universal Render Pipeline/Lit", "Standard");
            var sprite = EnsureMaterial(SpritePath, "Sprites/Default", null);
            var theme = EnsureTheme();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraRig>();
            camGo.transform.position = new Vector3(0, 9, -9);
            camGo.transform.LookAt(Vector3.zero);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52, -35, 0);

            var boot = new GameObject("Battle Chess").AddComponent<Bootstrap>();
            boot.theme = theme;
            boot.litMaterial = lit;
            boot.spriteMaterial = sprite;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == ScenePath)) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Selection.activeObject = theme;
            EditorUtility.DisplayDialog("Battle Chess",
                "Scene created at " + ScenePath + ".\n\nPress Play to start a game.\n\nThe theme asset is selected: assign character prefabs to any piece in the Inspector to replace the placeholders.", "OK");
        }

        [MenuItem("Battle Chess/2. Reset Theme Asset (Pirates vs British)", priority = 2)]
        public static void ResetTheme()
        {
            if (File.Exists(ThemePath) && !EditorUtility.DisplayDialog("Battle Chess", "Overwrite the theme asset? Prefab assignments will be lost.", "Overwrite", "Cancel")) return;
            AssetDatabase.DeleteAsset(ThemePath);
            Selection.activeObject = EnsureTheme();
        }

        [MenuItem("Battle Chess/3. Apply Mobile + TV Player Settings", priority = 3)]
        public static void PlayerSetup()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            if (string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName == "New Unity Project") PlayerSettings.productName = "AI Animated Chess";
            QualitySettings.vSyncCount = 0;
            Debug.Log("Battle Chess: orientation set to auto-rotate (portrait for phones, landscape for TV/mirroring).");
        }

        [MenuItem("Battle Chess/Run Engine Self-Test", priority = 20)]
        public static void SelfTest()
        {
            var sw = Stopwatch.StartNew();
            var results = new List<string>();
            bool ok = true;
            ok &= Check(results, "Start position d3", Chess.Perft(Chess.Initial(), 3), 8902);
            ok &= Check(results, "Kiwipete d2", Chess.Perft(Chess.FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -"), 2), 2039);
            ok &= Check(results, "Endgame d3", Chess.Perft(Chess.FromFen("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -"), 3), 2812);
            ok &= Check(results, "Promotions d3", Chess.Perft(Chess.FromFen("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1"), 3), 9467);
            var mate = ChessAI.BestMove(Chess.FromFen("r1bqkb1r/pppp1ppp/2n2n2/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq -"), 3, 0, null, 1);
            bool mateOk = mate != null && mate.ToString() == "h5f7";
            results.Add((mateOk ? "PASS" : "FAIL") + "  AI finds mate in one: " + (mate != null ? mate.ToString() : "none"));
            ok &= mateOk;
            Debug.Log("Battle Chess engine self-test " + (ok ? "PASSED" : "FAILED") + " in " + sw.ElapsedMilliseconds + " ms\n" + string.Join("\n", results.ToArray()));
        }

        static bool Check(List<string> r, string name, long got, long want)
        {
            bool ok = got == want;
            r.Add((ok ? "PASS" : "FAIL") + "  " + name + ": " + got + (ok ? "" : " (expected " + want + ")"));
            return ok;
        }

        // ---------------- helpers ----------------
        public static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        static Material EnsureMaterial(string path, string shader, string fallback)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = Shader.Find(shader);
            if (sh == null && fallback != null) sh = Shader.Find(fallback);
            if (sh == null) { Debug.LogWarning("Battle Chess: shader not found: " + shader); return null; }
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static ThemeDefinition EnsureTheme()
        {
            var t = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(ThemePath);
            if (t != null) return t;
            EnsureFolder(Root + "/Resources/Themes");
            t = ThemeLibrary.PiratesVsBritish();
            AssetDatabase.CreateAsset(t, ThemePath);
            AssetDatabase.SaveAssets();
            return t;
        }
    }
}
