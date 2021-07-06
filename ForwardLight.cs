using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CRP;

using CommandBufferPool = CRP.CommandBufferPool;
using Unity.Collections;

public class ForwardLight
{
    static class LightConstantBuffer
    {
        public static int _MainLightPosition;
        public static int _MainLightColor;

        public static int _AdditionalLightsCount;
        public static int _AdditionalLightsPosition;
        public static int _AdditionalLightsColor;
        public static int _AdditionalLightsAttenuation;
        public static int _AdditionalLightsSpotDir;

        public static int _AdditionalLightOcclusionProbeChannel;
    }
    const string k_SetupLightConstants = "Setup Light Constants";

    Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
    Vector4 k_DefaultLightColor = Color.black;

    //prepare to use after pbr finished
    Vector4 k_DefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
    Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
    Vector4 k_DefaultLightsProbeChannel = new Vector4(-1.0f, 1.0f, -1.0f, -1.0f);

    public ForwardLight()
    {
        LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
        LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
    }

    public void Setup(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_SetupLightConstants);
        camera.TryGetCullingParameters(out ScriptableCullingParameters p);
        CullingResults cullingResults = context.Cull(ref p);
        SetupShaderLightConstants(cmd, cullingResults);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void SetupShaderLightConstants(CommandBuffer cmd, CullingResults cull)
    {
        SetupMainLightConstants(cmd, cull);
    }

    void SetupMainLightConstants(CommandBuffer cmd, CullingResults cull)
    {
        Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
        NativeArray<VisibleLight> lights = cull.visibleLights;
        int mainLightIndex = GetMainLightIndex(lights);
        if(mainLightIndex!=-1)
        {
            InitializeLightConstants(lights, mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);
        }else
        {
            lightPos = new Vector4(0, 0, 0, 0);
            lightColor = new Vector4(0, 0, 0, 0);
        }

        cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
        cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
    }

    void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
    {
        lightPos = k_DefaultLightPosition;
        lightColor = k_DefaultLightColor;
        lightAttenuation = k_DefaultLightAttenuation;
        lightSpotDir = k_DefaultLightSpotDirection;
        lightOcclusionProbeChannel = k_DefaultLightsProbeChannel;

        VisibleLight lightData = lights[lightIndex];
        if (lightData.lightType == LightType.Directional)
        {
            Vector4 dir = -lightData.localToWorldMatrix.GetColumn(2);
            lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
        }
        else
        {
            Vector4 pos = lightData.localToWorldMatrix.GetColumn(3);
            lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
        }

        lightColor = lightData.finalColor;
    }

    static int GetMainLightIndex(NativeArray<VisibleLight> visibleLights)
    {
        int totalVisibleLights = visibleLights.Length;
        if (totalVisibleLights == 0)
            return -1;
        Light sunLight = RenderSettings.sun;
        int brightestDirectionalLightIndex = -1;
        float brightestLightIntensity = 0.0f;
        for (int i = 0; i < totalVisibleLights; ++i)
        {
            VisibleLight currVisibleLight = visibleLights[i];
            Light currLight = currVisibleLight.light;

            if (currLight == null)
                break;

            if (currLight == sunLight)
                return i;

            if (currVisibleLight.lightType == LightType.Directional && currLight.intensity > brightestLightIntensity)
            {
                brightestLightIntensity = currLight.intensity;
                brightestDirectionalLightIndex = i;
            }
        }
        return brightestDirectionalLightIndex;
    }
}
