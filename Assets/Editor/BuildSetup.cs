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
            Build(BuildTarget.WebGL, "builds/WebGL/WebGL");
        }

        public static void BuildIOS()
        {
            Build(BuildTarget.iOS, "builds/iOS/iOS");
        }

        private static void Build(BuildTarget target, string path)
        {
            EnsureScene();
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
