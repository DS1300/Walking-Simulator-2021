
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#define _SPECULAR_SETUP 1
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST, _HueVariation, _SpecColor, _WindMultipliers, _BaseWindMultipliers;
float4 _AdvancedEdgeBending, _Translucency;
float _BumpScale, _Smoothness, _Cutoff, _LeafNoise, _EnableNormalRotation;
float _EnableAdvancedEdgeBending, _TumbleFrequency, _TumbleStrength, _LeafTurbulence;
float _EdgeFlutterInfluence, _AmbientTranslucency, _AlphaLeak, _ThicknessRemap;
float _BlendBB, _BlendBBDithering, _WindStrength, _WindPower;
CBUFFER_END
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpSpecMap); SAMPLER(sampler_BumpSpecMap);
TEXTURE2D(_BumpOcclusionMap); SAMPLER(sampler_BumpOcclusionMap);
TEXTURE2D_ARRAY(_BaseMapArray); SAMPLER(sampler_BaseMapArray);
TEXTURE2D_ARRAY(_BumpOcclusionMapArray); SAMPLER(sampler_BumpOcclusionMapArray);
float3 GetWorldSpaceNormalizeViewDir(float3 p) { return SafeNormalize(_WorldSpaceCameraPos - p); }
#if defined(CTI_BILLBOARD)
    #define SHADERPASS_SHADOWCASTER 1
    #if defined(CTI_SHADOW)
        #define SHADERPASS 1
    #else
        #define SHADERPASS 0
    #endif
    #include "../Includes/CTI URP SG BillboardVertex.hlsl"
    float CTIBillboardNoise(float4 p, float frame) { return InterleavedGradientNoise(p.xy, (int)frame); }
    #define InterleavedGradientNoise CTIBillboardNoise
    #include "../Includes/CTI URP SG BillboardFragment.hlsl"
    #undef InterleavedGradientNoise
#else
    #if defined(CTI_LEAVES)
        #define _LEAFTUMBLING 1
        #define _LEAFTURBULENCE 1
    #endif
    #include "../Includes/CTI URP SG Wind.hlsl"
#endif
#include "../Includes/CTI URP SG Lighting.hlsl"
struct Attributes {
    float4 positionOS : POSITION;
    half3 normalOS : NORMAL;
    half4 tangentOS : TANGENT;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float3 uv2 : TEXCOORD1;
    float3 uv3 : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct Varyings {
    float4 positionCS : SV_POSITION;
    float4 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    half3 cv : TEXCOORD4;
    float4 shadowCoord : TEXCOORD5;
    half4 fogVertexLight : TEXCOORD6;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
float3 _LightDirection;
Varyings Vert(Attributes v) {
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v,o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 p; half3 n; half3 tangent = v.tangentOS.xyz; half signT = v.tangentOS.w;
    half2 cv;
    #if defined(CTI_BILLBOARD)
        CTI_BillboardVert_float(v.positionOS.xyz,v.uv,v.uv2,p,n,tangent,o.uv,cv);
        signT = 1;
    #else
        #if defined(CTI_LEAVES)
            CTI_AnimateVertexSG_float(v.positionOS.xyz,v.normalOS,v.color,v.uv2.xy,v.uv3,
                _LeafNoise,_WindMultipliers.xyz,_EnableNormalRotation != 0,
                _EnableAdvancedEdgeBending != 0,_AdvancedEdgeBending.xy,
                float3(_Time.y,sin(_Time.y),cos(_Time.y)),true,p,n,cv);
        #else
            CTI_AnimateVertexSG_float(v.positionOS.xyz,v.normalOS,v.color,v.uv2.xy,v.uv3,
                0,_BaseWindMultipliers.xyz,false,false,float2(0,0),
                float3(_Time.y,sin(_Time.y),cos(_Time.y)),false,p,n,cv);
        #endif
        o.uv = float4(TRANSFORM_TEX(v.uv,_BaseMap),0,0);
    #endif
    o.positionWS = TransformObjectToWorld(p);
    o.normalWS = TransformObjectToWorldNormal(n);
    o.tangentWS = half4(TransformObjectToWorldDir(tangent),signT * GetOddNegativeScale());
    o.cv = half3(cv,v.color.b);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    #if defined(CTI_SHADOW)
        o.positionCS = TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,_LightDirection));
        #if UNITY_REVERSED_Z
            o.positionCS.z = min(o.positionCS.z,o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
        #else
            o.positionCS.z = max(o.positionCS.z,o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
        #endif
    #endif
    o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
    o.fogVertexLight = half4(ComputeFogFactor(o.positionCS.z),VertexLighting(o.positionWS,o.normalWS));
    return o;
}
half4 Frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target {
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    #if defined(LOD_FADE_CROSSFADE)
        LODDitheringTransition(i.positionCS.xyz,unity_LODFade.x);
    #endif
    half3 albedo,normalTS; half alpha,smoothness,ao,thickness=0;
    #if defined(CTI_BILLBOARD)
        CTIBillboardFragment_half(i.uv,i.cv.xy,ComputeScreenPos(TransformWorldToHClip(i.positionWS)),albedo,alpha,normalTS,smoothness,ao,thickness);
    #else
        #if defined(CTI_ARRAY)
            half4 a = SAMPLE_TEXTURE2D_ARRAY(_BaseMapArray,sampler_BaseMapArray,i.uv.xy,i.cv.z);
            half4 b = SAMPLE_TEXTURE2D_ARRAY(_BumpOcclusionMapArray,sampler_BumpOcclusionMapArray,i.uv.xy,i.cv.z);
        #else
            half4 a = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv.xy);
            #if defined(CTI_LEAVES)
                half4 b = SAMPLE_TEXTURE2D(_BumpSpecMap,sampler_BumpSpecMap,i.uv.xy);
            #else
                half4 b = SAMPLE_TEXTURE2D(_BumpOcclusionMap,sampler_BumpOcclusionMap,i.uv.xy);
            #endif
        #endif
        albedo = lerp(a.rgb,(a.rgb + _HueVariation.rgb)*.5,i.cv.x*_HueVariation.a);
        alpha = a.a;
        normalTS = UnpackNormalAG(b,_BumpScale);
        #if defined(CTI_LEAVES)
            normalTS.z *= IS_FRONT_VFACE(face,1,-1);
            smoothness = b.b*_Smoothness; ao=i.cv.y; thickness=b.r;
        #else
            smoothness = a.a*_Smoothness; ao=b.b*i.cv.y;
        #endif
    #endif
    #if defined(CTI_LEAVES) || defined(CTI_BILLBOARD)
        clip(alpha-_Cutoff);
    #endif
    #if defined(CTI_SHADOW) || defined(CTI_DEPTH)
        return 0;
    #else
        half3 tangent = normalize(i.tangentWS.xyz);
        half3 normal = normalize(i.normalWS);
        half3 bitangent = cross(normal,tangent)*i.tangentWS.w;
        half3 emission=0;
        #if defined(CTI_LEAVES) || defined(CTI_BILLBOARD)
            CTI_SimpleTranslucentLighting_half(i.positionWS,normal,tangent,bitangent,albedo,normalTS,
                _Translucency.xyz,thickness,ao,normal,emission);
        #else
            normal = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,half3x3(tangent,bitangent,normal)));
        #endif
        InputData inputData = (InputData)0;
        inputData.positionWS=i.positionWS; inputData.normalWS=normal;
        inputData.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
        inputData.shadowCoord=i.shadowCoord; inputData.fogCoord=i.fogVertexLight.x;
        inputData.vertexLighting=i.fogVertexLight.yzw; inputData.bakedGI=SampleSH(normal);
        half4 result=UniversalFragmentPBR(inputData,albedo,0,_SpecColor.rgb,smoothness,ao,emission,1);
        result.rgb=MixFog(result.rgb,inputData.fogCoord);
        return result;
    #endif
}
