Shader "CRP/SRPUnlit"
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
        //CBUFFER_END
        float4 _Color;

#pragma target 3.5
#pragma vertex vert
#pragma fragment frag

        struct vertexInput
    {
        float4 pos:POSITION;
};
    struct vertexOutput
    {
        float4 clipPos : SV_POSITION;
        };

    vertexOutput vert(vertexInput input)
    {
        vertexOutput output;
        float4 worldPos = mul(unity_ObjectToWorld, input.pos);
        output.clipPos = mul(unity_MatrixVP, worldPos);
        //output.clipPos = input.pos; 
        return output;
    }

    float4 frag(vertexOutput input) :SV_TARGET
    {
        return _Color;
    }

        ENDHLSL

        }
    }
    //FallBack "Diffuse"
}
