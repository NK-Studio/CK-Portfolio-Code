using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using Plugins.BuildMessage.Editor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Utility;

class Builder
{
    private static readonly string BuildSettingPath = $"{Directory.GetCurrentDirectory()}/BuildSetting.config";

    [MenuItem("Build/빌드하기")]
    public static void Build()
    {
        UnityBuildMessage.BuildMessageOn();
        var stream = new FileStream(BuildSettingPath,FileMode.Open);
        var reader = new StreamReader(stream);

        Debug.Log($"BuildNumberValue on Builder.Build - #{BuildNumber.BuildNumberValue}");
        var dir = $"{reader.ReadLine()}/{PlayerSettings.productName}_{DateTime.Now:MMddHHmm}_{Application.version}-#{BuildNumber.BuildNumberValue}/{PlayerSettings.productName}.exe";
        
        reader.Close();
        
        ForceSaveDirtyScenesOnBatchMode();
        GenericBuild(FindEnabledEditorScenes(), dir, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    // Addressable이 dirty scene 저장 과정에서 Batch 모드 예외 처리를 안 해서 빌드 시 addressable 빌드를 실패함
    // 따라서 Batch 모드 한정으로 강제 저장
    private static void ForceSaveDirtyScenesOnBatchMode()
    {
        if (!Application.isBatchMode)
        {
            return;
        }
        var dirtyScenes = new List<Scene>();
        for (int i = 0; i < SceneManager.sceneCount; ++i)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isDirty)
            {
                dirtyScenes.Add(scene);
            }
        }
        Debug.Log($"Batch mode로 인한 강제 씬 저장 실행: ({dirtyScenes.Count}개)\n{dirtyScenes.JoinToString("\n", it => "- "+it.path)}");
        if (EditorSceneManager.SaveScenes(dirtyScenes.ToArray()))
        {
            Debug.Log($"씬 강제 저장 성공!");
        }
        else
        {
            Debug.LogWarning($"씬 강제 저장 실패?? :thinking:");
        }
    }

    public static void GenericBuild(string[] scenes, string targetDir, BuildTargetGroup buildGroup, BuildTarget buildTarget, BuildOptions buildOptions)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(buildGroup, buildTarget);
        var res = BuildPipeline.BuildPlayer(scenes, targetDir, buildTarget, buildOptions);

        if (res.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + res.summary.totalSize + " bytes");
        }
        else if (res.summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }

    private static string[] FindEnabledEditorScenes()
    {
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
            {
                continue;
            }

            scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }
}