using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Driftworld.EditorTools
{
    // Cloud build entry points. The pipeline (game-ci unity-builder) calls
    // these methods; they create the main scene on first run so the repo
    // never needs a hand saved scene file, register it for builds, and run
    // the player build. The scene itself is an empty stage: Bootstrap fills
    // it in code at load.
    public static class BuildSetup
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Driftworld/Create Main Scene")]
        public static void CreateMainScene()
        {
            EnsureScene();
        }

        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        public static void BuildWebGL()
        {
            // GitHub Pages does not send a Content Encoding header for
            // compressed build files, and the WebGL loader then refuses to
            // parse them. Ship the playable build uncompressed so it runs
            // on plain static hosting.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            Build(BuildTarget.WebGL, "builds/WebGL/WebGL");
            PatchWebGLIndex("builds/WebGL/WebGL/index.html");
        }

        // MECH-001 on a real phone browser: the default template never
        // sets touch-action on the canvas and the framework does not
        // preventDefault drags, so the browser claims every drag as a
        // scroll gesture and cancels it before the game can use it. The
        // hero then never receives movement input and the combat buttons
        // lose their pointer events the same way, while the player sees
        // only the browser view shifting. Declaring touch-action: none
        // hands all drags to the game, which is what the drag movement
        // spec assumes. Proven 2026-11-23: simulated mouse and touch
        // drags moved the hero in Chromium (inp 1.00), while Vashon's
        // phone playtest of the same build could not move him at all.
        private static void PatchWebGLIndex(string indexPath)
        {
            if (!File.Exists(indexPath)) return;
            string html = File.ReadAllText(indexPath);
            if (html.Contains("touch-action")) return;
            const string style = "    <style>#unity-canvas { touch-action: none; } html, body { overscroll-behavior: none; }</style>\n";
            html = html.Replace("</head>", style + "  </head>");
            File.WriteAllText(indexPath, html);
            Debug.Log("Driftworld: patched index.html with touch-action: none for mobile drag input.");
        }

        public static void BuildIOS()
        {
            Build(BuildTarget.iOS, "builds/iOS/iOS");
        }

        // The greybox builds every material in code at runtime, so no scene
        // material ever tells the build pipeline to compile the Standard
        // shader, and runtime materials then render in the magenta error
        // color. Shipping one real material under Resources forces the
        // shader into the build; runtime code clones it (see Diag).
        public static void EnsureGreyboxMaterials()
        {
            const string dir = "Assets/Resources/Greybox";
            const string path = dir + "/White.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Directory.CreateDirectory(dir);
            var mat = new Material(Shader.Find("Standard")) { color = Color.white };
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }

        private static void Build(BuildTarget target, string path)
        {
            EnsureScene();
            EnsureGreyboxMaterials();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Driftworld build failed for " + target + ": " + report.summary.result);
            }
            Debug.Log("Driftworld build succeeded for " + target + " at " + path);
        }
    }
}
