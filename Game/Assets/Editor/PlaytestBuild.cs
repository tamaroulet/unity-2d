// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorScripts
{
    /// <summary>
    /// プレイ確認（H2）用の Windows ビルド。harness の Unity アダプタがバッチモードで呼ぶ。
    ///
    ///   Unity.exe -batchmode -nographics -quit -projectPath &lt;wt&gt;/Game -buildTarget Win64
    ///     -executeMethod Game.EditorScripts.PlaytestBuild.BuildWindows -playtestOutput &lt;exe&gt;
    ///
    /// 非破壊であること: harness はビルド後にワークツリーの追跡中のファイルが 1 つも
    /// 変わっていないことを確かめる。そのため、ビルドターゲットの切り替え・
    /// EditorBuildSettings・PlayerSettings の変更はせず、成果物もリポジトリの外にだけ置く。
    /// 終了コード: 0 = 成功 / 1 = 失敗（引数の不備を含む）
    /// </summary>
    public static class PlaytestBuild
    {
        private const string Tag = "[PlaytestBuild]";
        private const string OutputArg = "-playtestOutput";

        // Unity プロジェクトからの相対パス（契約の playtest.scene はリポジトリ直下からの相対）
        private const string ScenePath = "Assets/Scenes/MainGame.unity";

        public static void BuildWindows()
        {
            try
            {
                ExitIfBatchMode(Build() ? 0 : 1);
            }
            catch (Exception e)
            {
                // 例外で抜けると -quit の終了コードが成否を表さないので、ここで失敗にする
                Debug.LogError($"{Tag} FAILED with exception: {e}");
                ExitIfBatchMode(1);
            }
        }

        private static bool Build()
        {
            string exe = ReadOutputArg();
            if (exe == null)
            {
                return false;
            }

            // 作業ツリーの中に成果物を置くと、未追跡ファイルとして非破壊の検査に掛かる
            string projectDir = Path.GetFullPath(Directory.GetCurrentDirectory());
            string repoDir = Path.GetFullPath(Path.Combine(projectDir, ".."));
            if (IsUnder(exe, repoDir))
            {
                Debug.LogError($"{Tag} {OutputArg} must be outside the repository ({repoDir}): {exe}");
                return false;
            }

            // 切り替えは Library と設定の再インポートを伴い、呼び方で動きが変わる。
            // 呼び出し側の -buildTarget Win64 に任せ、違っていたら止める
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                Debug.LogError($"{Tag} active build target is {EditorUserBuildSettings.activeBuildTarget}, " +
                    "not StandaloneWindows64. Pass -buildTarget Win64 on the command line.");
                return false;
            }

            if (!File.Exists(Path.Combine(projectDir, ScenePath)))
            {
                Debug.LogError($"{Tag} scene not found: {ScenePath}");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(exe));

            // シーンは EditorBuildSettings を経由せず直接渡す（設定ファイルを書き換えない）
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"{Tag} Starting Windows build to: {exe}");
            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;

            if (summary.result == BuildResult.Succeeded && summary.totalErrors == 0)
            {
                Debug.Log($"{Tag} Windows build SUCCEEDED: {summary.totalSize} bytes, " +
                    $"{summary.totalTime.TotalSeconds:F1}s, warnings={summary.totalWarnings}");
                return true;
            }
            Debug.LogError($"{Tag} Windows build FAILED with result: {summary.result}, errors: {summary.totalErrors}");
            return false;
        }

        /// <summary>-playtestOutput の値（絶対パスの .exe）。不備があればログを出して null。</summary>
        private static string ReadOutputArg()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, OutputArg);
            string value = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith("-", StringComparison.Ordinal))
            {
                Debug.LogError($"{Tag} {OutputArg} <absolute path to .exe> is required");
                return null;
            }
            if (!Path.IsPathRooted(value) || !value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"{Tag} {OutputArg} must be an absolute path ending with .exe: {value}");
                return null;
            }
            return Path.GetFullPath(value);
        }

        private static bool IsUnder(string path, string dir)
        {
            string d = dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(d, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// -batchmode のときだけ、指定した終了コードでエディタを終了する（WebGlBuildScript と同じ）。
        /// </summary>
        private static void ExitIfBatchMode(int exitCode)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }
    }
}
#endif
