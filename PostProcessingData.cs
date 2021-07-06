using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using System;
using UnityEngine.Rendering;
using UnityEditor;

[Serializable]
public class PostProcessingData:ScriptableObject
{
#if UNITY_EDITOR
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
    internal class CreatePostProcessDataAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var instance = CreateInstance<PostProcessingData>();
            AssetDatabase.CreateAsset(instance, pathName);
            ResourceReloader.ReloadAllNullIn(instance, CustomRenderPipelineAsset.packagePath);
            Selection.activeObject = instance;
        }

        [MenuItem("Assets/Create/Rendering/Post-process Data", priority = CoreUtils.assetCreateMenuPriority3)]
        static void CreatePostProcessData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePostProcessDataAsset>(), "CustomPostProcessData.asset", null, null);
        }
    }
#endif

    [Serializable, ReloadGroup]
    public sealed class ShaderResources
    {
        [Reload("Shaders/PostProcessing/SMAA.shader")]
        public Shader smaa;

        [Reload("Shaders/PostProcessing/FXAA.shader")]
        public Shader fxaa;
    }

    public ShaderResources shaders;
}
