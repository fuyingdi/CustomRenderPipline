using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CRPRenderer
{
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    ScriptableRenderContext context;
    Camera camera;
    static Material errorMaterial;

    //Passes
    ForwardLight forwardLight = new ForwardLight();
    OpaquePass opaquePass=new OpaquePass();
    PostProcessingPass processingPass = new PostProcessingPass();

    const string bufferName = "render camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    /// Use to clear Color RT
    public void Setup()
    {
        
        buffer.BeginSample(bufferName);
        buffer.ClearRenderTarget(true, true, Color.clear);
        context.ExecuteCommandBuffer(buffer);
        buffer.EndSample(bufferName);
        buffer.Clear();
        context.Submit();        
    }

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        context.SetupCameraProperties(camera);
        Setup();

        buffer.BeginSample(bufferName);

        context.DrawSkybox(camera);

        DrawUnsupportedShaders();

        forwardLight.Setup(context, camera);
        opaquePass.Execute(context, camera);
        processingPass.Execute(context, camera);

        if (!IsGameCamera(camera))
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
        buffer.Clear();
        buffer.EndSample(bufferName);
        context.Submit();
        
        
    }

    public static bool IsGameCamera(Camera camera)
    {
        if (camera == null)
            throw new ArgumentNullException("camera");

        return camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR;
    }

    void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial =
                new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var drawingSettings = new DrawingSettings(
            legacyShaderTagIds[0], new SortingSettings(camera)
        )
        { overrideMaterial = errorMaterial };

        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        camera.TryGetCullingParameters(out ScriptableCullingParameters p);
        CullingResults cullingResults = context.Cull(ref p);
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }
}
