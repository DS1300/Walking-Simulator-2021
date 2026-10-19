Shader "Environment/Mountain Conifer"
{
 Properties {
  _BaseMap("Tree atlas",2D)="white"{}
  _BaseColor("Tint",Color)=(.27,.31,.30,1)
  _Cutoff("Alpha cutoff",Range(0,1))=.4
 }
 SubShader {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull Off
  Pass {
   Tags{"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;
   half4 _BaseColor;
   half _Cutoff;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float fog:TEXCOORD1;float distanceWS:TEXCOORD2;};
   Varyings Vert(Attributes input) {
    UNITY_SETUP_INSTANCE_ID(input);
    Varyings o;
    float3 world=TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS=TransformWorldToHClip(world);
    o.uv=input.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;
    o.fog=ComputeFogFactor(o.positionCS.z);
    o.distanceWS=distance(world,GetCameraPositionWS());
    return o;
   }
   half4 Frag(Varyings i):SV_Target {
    half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
    clip(tex.a-_Cutoff);
    half distant=smoothstep(110,240,i.distanceWS);
    half3 fog=unity_FogColor.rgb;
    fog=lerp(fog,dot(fog,half3(.2126,.7152,.0722))*half3(.47,.56,.64),distant);
    half visibility=max(ComputeFogIntensity(i.fog),.28);
    return half4(lerp(fog,tex.rgb*_BaseColor.rgb,visibility),1);
   }
   ENDHLSL
  }
 }
}
