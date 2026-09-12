// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class WebGlBuildScript
    {
        [MenuItem("Tools/Build WebGL")]
        public static void BuildWebGL()
        {
            string buildPath = Path.Combine(Directory.GetCurrentDirectory(), "Builds/WebGL");
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            // アクティブなビルドターゲットが WebGL でない場合、コマンドラインからの
            // -buildTarget 指定漏れ等でエディタ用アセットのままビルドされてしまう
            // 事故を防ぐため、明示的に切り替える。
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[WebGlBuildScript] Switching active build target to WebGL...");
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                if (!switched)
                {
                    Debug.LogError("[WebGlBuildScript] Failed to switch active build target to WebGL. " +
                        "WebGL Build Support module may not be installed.");
                    ExitIfBatchMode(1);
                    return;
                }
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainGame.unity" },
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[WebGlBuildScript] Starting WebGL build to: {buildPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded && summary.totalErrors == 0)
            {
                Debug.Log($"[WebGlBuildScript] WebGL build SUCCEEDED: {summary.totalSize} bytes, " +
                    $"{summary.totalTime.TotalSeconds:F1}s, warnings={summary.totalWarnings}");
                ExitIfBatchMode(0);
            }
            else
            {
                Debug.LogError($"[WebGlBuildScript] WebGL build FAILED with result: {summary.result}, errors: {summary.totalErrors}");
                ExitIfBatchMode(1);
            }
        }

        /// <summary>
        /// -batchmode 経由のコマンドライン実行時のみ、指定した終了コードでエディタを終了する。
        /// CI 等が Unity プロセスの終了コードからビルド成否を判定できるようにするため
        /// （バッチモードでない通常のエディタ操作時は何もしない）。
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
