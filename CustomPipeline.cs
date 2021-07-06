using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

static internal class PerCameraBuffer
{
    // TODO: This needs to account for stereo rendering
    public static int _InvCameraViewProj;
    public static int _ScaledScreenParams;
    public static int _ScreenParams;
    public static int _WorldSpaceCameraPos;
}

class CameraDataComparer : IComparer<Camera>
{
    public int Compare(Camera lhs, Camera rhs)
    {
        return (int)lhs.depth - (int)rhs.depth;
    }
}

public class CustomPipeline : RenderPipeline
{
    CRPRenderer crprender = new CRPRenderer();

    public CustomPipeline()
    {
        PerCameraBuffer._InvCameraViewProj = Shader.PropertyToID("_InvCameraViewProj");
        PerCameraBuffer._ScreenParams = Shader.PropertyToID("_ScreenParams");
        PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
        PerCameraBuffer._WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);
        SortCameras(cameras);
        foreach (Camera camera in cameras)
        {
            SetupPerCameraShaderConstants(camera);
            crprender.Render(context, camera);
        }
        EndFrameRendering(context, cameras);

    }

    void SortCameras(Camera[] cameras)
    {
        if (cameras.Length <= 1)
            return;
        Array.Sort(cameras, new CameraDataComparer());
    }

    static void SetupPerCameraShaderConstants(Camera camera)
    {
        Rect pixelRect = camera.pixelRect;
        float scaledCameraWidth = (float)pixelRect.width;
        float scaledCameraHeight = (float)pixelRect.height;
        Shader.SetGlobalVector(PerCameraBuffer._ScaledScreenParams, new Vector4(scaledCameraWidth, scaledCameraWidth, 1.0f + 1.0f / scaledCameraWidth, 1.0f + 1.0f / scaledCameraHeight));
        Shader.SetGlobalVector(PerCameraBuffer._WorldSpaceCameraPos, camera.transform.position);
    }

}
