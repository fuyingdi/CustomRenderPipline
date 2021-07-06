//BRDF functions

#define real half
#define real3 half3
#define real4 half4
#define real3x3 half3x3
#define FLOAT_MIN 6.103515625e-5

#ifndef UNITY_SPECCUBE_LOD_STEPS
    #define UNITY_SPECCUBE_LOD_STEPS 6
#endif

#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04)

TextureCube unity_SpecCube0;
SamplerState samplerunity_SpecCube0;
float4 _MainLightPosition;
float4 _MainLightColor;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;

float4 unity_WorldTransformParams;

struct BRDFData
{
    half3 diffuse;
    half3 specular;
    half perceptualRoughness;
    half roughness;
    half roughness2;
    half grazingTerm;

    half normalizationTerm;
    half roughness2MinusOne;
};

struct InputData
{
    float3 positionWS;
    float3 normalWS;
    float3 viewDirectionWS;
    //float4 shadowCoord;
    //half fogCoord;
    //half3 vertexLighting;
    float3 bakedGI;
};

real Pow4(real x)
{
    return (x * x) * (x * x);
}

float BlinnPhongBRDF(float3 viewdir,float3 lightdir,float3 normal,float smoothness)
{
    float3 halfView = normalize(lightdir + viewdir);
    float ndoth = max(dot(normal, halfView),0);
    float specular = pow(ndoth, max(1 / (max(smoothness, 0.001)), 2));
    //float specular = pow(ndoth, 32.0);
    return specular;
}

float GGXBRDF(float3 viewdir, float3 lightdir, float3 normal, float smoothness)
{
    float roughness = saturate((1 - smoothness) + 0.0001);
    float roughness2 = roughness * roughness;
    float normalizationTerm = roughness * 4.0 + 2.0;
    float roughness2MinusOne = roughness2 - 1.0;
    
    float3 H = normalize(lightdir + viewdir);
    float NoH = saturate(dot(normal, H));
    half LoH = saturate(dot(lightdir, H));
    float d = NoH * NoH * roughness2MinusOne + 1.00001f;
    half LoH2 = LoH * LoH;
    half specularTerm = roughness2 / ((d * d) * max(0.1h, LoH2) * normalizationTerm);
    return specularTerm;

}

real3 SHEvalLinearL0L1(real3 N, real4 shAr, real4 shAg, real4 shAb)
{
    real4 vA = real4(N, 1.0);

    real3 x1;
    // Linear (L1) + constant (L0) polynomial terms
    x1.r = dot(shAr, vA);
    x1.g = dot(shAg, vA);
    x1.b = dot(shAb, vA);

    return x1;
}

real3 SHEvalLinearL2(real3 N, real4 shBr, real4 shBg, real4 shBb, real4 shC)
{
    real3 x2;
    // 4 of the quadratic (L2) polynomials
    real4 vB = N.xyzz * N.yzzx;
    x2.r = dot(shBr, vB);
    x2.g = dot(shBg, vB);
    x2.b = dot(shBb, vB);

    // Final (5th) quadratic (L2) polynomial
    real vC = N.x * N.x - N.y * N.y;
    real3 x3 = shC.rgb * vC;

    return x2 + x3;
}

float3 SampleSH9(float4 SHCoefficients[7], float3 N)
{
    float4 shAr = SHCoefficients[0];
    float4 shAg = SHCoefficients[1];
    float4 shAb = SHCoefficients[2];
    float4 shBr = SHCoefficients[3];
    float4 shBg = SHCoefficients[4];
    float4 shBb = SHCoefficients[5];
    float4 shCr = SHCoefficients[6];

    // Linear + constant polynomial terms
    float3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

    return res;
}

float3 SampleSH(half3 normalWS)
{
    float4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}

real PerceptualRoughnessToMipmapLevel(real perceptualRoughness, uint mipMapCount)
{
    perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);

    return perceptualRoughness * mipMapCount;
}

real PerceptualRoughnessToMipmapLevel(real perceptualRoughness)
{
    return PerceptualRoughnessToMipmapLevel(perceptualRoughness, UNITY_SPECCUBE_LOD_STEPS);
}

#define SAMPLE_TEXTURECUBE_LOD(textureName, samplerName, coord3, lod)                    textureName.SampleLevel(samplerName, coord3, lod)

float3 EnviromentReflection(float3 reflectdir,float roughness)
{
    float mip = PerceptualRoughnessToMipmapLevel(roughness);
    float4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectdir, mip);
    return encodedIrradiance.rgb;
}
//_____________________Normal

real3 UnpackNormalRGB(real4 packedNormal, real scale = 1.0)
{
    real3 normal;
    normal.xyz = packedNormal.rgb * 2.0 - 1.0;
    normal.xy *= scale;
    return normal;
}

real GetOddNegativeScale()
{
    return unity_WorldTransformParams.w;
}

real3x3 CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)
{
    real sgn = flipSign * GetOddNegativeScale();
    real3 bitangent = cross(normal, tangent) * sgn;

    return real3x3(tangent, bitangent, normal);
}

real3 TransformTangentToWorld(real3 dirTS, real3x3 tangentToWorld)
{
    return mul(dirTS, tangentToWorld);
}

float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
    float3x3 tangentToWorld =
		CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld);
}

//_____________________
half OneMinusReflectivityMetallic(half metallic)
{
    half oneMinusDielectricSpec = kDieletricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

real PerceptualRoughnessToRoughness(real perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

float3 DirecBRDF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
    float3 H = normalize(lightDirectionWS + viewDirectionWS);
    float NoH = saturate(dot(normalWS, H));
    half LoH = saturate(dot(lightDirectionWS, H));
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;
    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);
    
    float3 color = specularTerm * brdfData.specular + brdfData.diffuse;
    return color;
}

float3 LightingPhysicallyBased(BRDFData brdfData, float3 lightColor, float3 lightDirectionWS, float3 normalWS, float3 viewDirectionWS)
{
    float nol = saturate(dot(normalWS, lightDirectionWS));
    float3 radiance = lightColor * nol;
    return DirecBRDF(brdfData, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
}

half3 EnvironmentBRDF(BRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half fresnelTerm)
{
    half3 c = indirectDiffuse * brdfData.diffuse;
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    c += surfaceReduction * indirectSpecular * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
    return c;
}

float3 GI(BRDFData brdfData, half3 bakedGI, half occlusion, half3 normalWS, half3 viewDirectionWS)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half fresnelTerm = Pow4(1.0 - saturate(dot(normalWS, viewDirectionWS)));
    half3 indirectDiffuse = bakedGI * occlusion;
    half3 indirectSpecular = EnviromentReflection(reflectVector, brdfData.perceptualRoughness);
    
    return EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
}

float3 PBR(InputData inputData,float3 albedo,float metallic,float3 specular,float smoothness,float occlusion,float3 emission)
{
    BRDFData brdfData;
    float oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
    float reflectivity = 1.0 - oneMinusReflectivity;
    
    brdfData.diffuse = albedo * oneMinusReflectivity;
    brdfData.specular = lerp(kDieletricSpec.rgb, albedo, metallic);

    brdfData.grazingTerm = saturate(smoothness + reflectivity);
    brdfData.perceptualRoughness = 1.0 - smoothness;
    brdfData.roughness = max(PerceptualRoughnessToRoughness(brdfData.perceptualRoughness), FLOAT_MIN);
    brdfData.roughness2 = brdfData.roughness * brdfData.roughness;

    brdfData.normalizationTerm = brdfData.roughness * 4 + 2;
    brdfData.roughness2MinusOne = brdfData.roughness2 - 1;

    float3 bakeGI = SampleSH(inputData.normalWS);

    float3 color = GI(brdfData, bakeGI, 1, inputData.normalWS, inputData.viewDirectionWS);
    color += LightingPhysicallyBased(brdfData, _MainLightColor.xyz, _MainLightPosition.xyz, inputData.normalWS, inputData.viewDirectionWS);

    return color;
}