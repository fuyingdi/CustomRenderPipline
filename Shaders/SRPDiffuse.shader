Shader "CRP/Diffuse"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        //_Glossiness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "Pipeline" }
        LOD 200

        Pass
        {
            Name "SRPUnLit"
            Tags{"LightMode" = "SRPDeafaultUnlit"}
        HLSLPROGRAM

        //CBUFFER_START(UnityPerDraw)
        float4x4 unity_MatrixVP;
        float4x4 unity_ObjectToWorld;
        float4x4 UNITY_MATRIX_M;
        float4 _MainLightPosition;
        float4 _MainLightColor;
        //CBUFFER_END
        float4 _Color;
        Texture2D _MainTex;
        SamplerState sampler_MainTex;

#pragma target 3.5
#pragma multi_compile_instancing

#pragma vertex vert
#pragma fragment frag

        struct vertexInput
    {
        float4 pos:POSITION;
        float3 normal:NORMAL;
        float2 uv:TEXCOORD0;
};
    struct vertexOutput
    {
        float4 clipPos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : TEXCOORD1;
        };

    vertexOutput vert(vertexInput input)
    {
        vertexOutput output;
        float4 worldPos = mul(unity_ObjectToWorld, input.pos);
        output.clipPos = mul(unity_MatrixVP, worldPos);
        output.normal = mul((float3x3)UNITY_MATRIX_M, input.normal);
        output.uv = input.uv;
        return output;
    }

    float4 frag(vertexOutput input) :SV_TARGET
    {
        float ndotl = saturate(dot(input.normal,_MainLightPosition.xyz));
        float3 color = ndotl* _MainLightColor.rgb;
        float4 albedo = _MainTex.Sample(sampler_MainTex, input.uv);
        color *= albedo.rgb;
        return float4(color,1);
    }

        ENDHLSL

        }
    }
    //FallBack "Diffuse"
}
