// Soft additive rim glow drawn over an object's own mesh, used to highlight the coffin.
// _Intensity is driven from script (0 = invisible).
Shader "Walking Simulator/Rim Highlight"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.82, 0.58, 1)
        _Power ("Rim Power", Range(0.5, 8)) = 2.5
        _Intensity ("Intensity", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend One One
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Power;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetCameraPositionWS() - positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half rim = 1.0h - saturate(dot(normalize(input.normalWS), normalize(input.viewWS)));
                return _Color * (pow(rim, _Power) * _Intensity);
            }
            ENDHLSL
        }
    }
}
