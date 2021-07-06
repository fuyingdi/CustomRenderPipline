using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    public bool enable = true;

    public static readonly string packagePath = "Assets/CustomRenderPipeline";

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomPipeline();
    }
}
