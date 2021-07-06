using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using GraphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat;
public struct RenderTargetHandle
{
    public int id { set; get; }

    public static readonly RenderTargetHandle CameraTarget = new RenderTargetHandle { id = -1 };

    public void Init(string shaderProperty)
    {
        id = Shader.PropertyToID(shaderProperty);
    }

    public RenderTargetIdentifier Identifier()
    {
        if (id == -1)
        {
            return BuiltinRenderTextureType.CameraTarget;
        }
        return new RenderTargetIdentifier(id);
    }

    public bool Equals(RenderTargetHandle other)
    {
        return id == other.id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is RenderTargetHandle && Equals((RenderTargetHandle)obj);
    }

    public override int GetHashCode()
    {
        return id;
    }

    public static bool operator ==(RenderTargetHandle c1, RenderTargetHandle c2)
    {
        return c1.Equals(c2);
    }

    public static bool operator !=(RenderTargetHandle c1, RenderTargetHandle c2)
    {
        return !c1.Equals(c2);
    }
}

public class PostProcessingPass : ScriptableRenderPass
{
    RenderTextureDescriptor m_Descriptor;
    RenderTargetHandle m_Source;
    RenderTargetHandle m_Destination;
    MaterialLibrary m_Materials;
    Material m_BlitMaterial;

    const string k_RenderPostProcessingTag = "Render PostProcessing Effects";
    const string k_RenderFinalPostProcessingTag = "Render Final PostProcessing Pass";

    SMAA SMAAPass=new SMAA();
    public override void Execute(ScriptableRenderContext context, Camera camera)
    {
        var cmd = CommandBufferPool.Get(k_RenderPostProcessingTag);

        m_Source.Init("_FinalColor");
        m_Source.id = (int)BuiltinRenderTextureType.CameraTarget;

        m_Destination.Init("_Dest");
        m_Destination.id = (int)BuiltinRenderTextureType.CurrentActive;
        m_Descriptor.height = camera.pixelHeight;
        m_Descriptor.width = camera.pixelWidth;
        m_Descriptor.graphicsFormat = GraphicsFormat.B8G8R8_SRGB;

        //SMAAPass
        //SMAAPass.m_BlitMaterial = m_Materials.subpixelMorphologicalAntialiasing;
        SMAAPass.Execute(context, camera, cmd);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

public abstract class PostProcessingComponent
{
    public RenderTextureDescriptor m_Descriptor;
    public int m_Source;
    public int m_Destination;
    public Material m_BlitMaterial;

    public abstract void Execute(ScriptableRenderContext context, Camera camera, CommandBuffer cmd);
}

class MaterialLibrary
{
    public readonly Material stopNaN;
    public readonly Material subpixelMorphologicalAntialiasing;
    public readonly Material FastApproximateAntialiasing;
}

static class ShaderConstants
{
    public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");
    public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");
}

public class SMAA : PostProcessingComponent
{
    const int kStencilBit = 64;

    public override void Execute(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
    {

        //cmd.GetTemporaryRT()
    }
}

public class FXAA : PostProcessingComponent
{

    public override void Execute(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
    {

    }
}
