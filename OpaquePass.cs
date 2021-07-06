using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OpaquePass : ScriptableRenderPass
{
    static ShaderTagId[] ShaderTagIds = { new ShaderTagId("SRPDeafaultUnlit"),
        new ShaderTagId("CRPDiffuse"),
        new ShaderTagId("CRPLit")};

    public override void Execute(ScriptableRenderContext context,Camera camera)
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        camera.TryGetCullingParameters(out ScriptableCullingParameters p);
        CullingResults cullingResults = context.Cull(ref p);

        for(int i=0;i< ShaderTagIds.Length;i++)
        {
            var drawingSettings = new DrawingSettings(ShaderTagIds[i], sortingSettings)
            {
                perObjectData=PerObjectData.LightProbe|PerObjectData.ReflectionProbes
            };
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }
    }

}
