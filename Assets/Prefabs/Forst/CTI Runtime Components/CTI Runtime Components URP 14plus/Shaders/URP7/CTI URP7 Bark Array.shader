Shader "CTI/URP7/Bark Array"
{
    Properties {

        _BaseMap ("Albedo / Cutout", 2D) = "white" {}
        _BumpSpecMap ("Normal AG / Thickness R / Smoothness B", 2D) = "bump" {}
        _BumpOcclusionMap ("Normal AG / Occlusion B", 2D) = "bump" {}
        _BaseMapArray ("Bark Albedo Array", 2DArray) = "" {}
        _BumpOcclusionMapArray ("Bark Normal Array", 2DArray) = "" {}
        _BumpScale ("Normal Scale", Float) = 1
        _Smoothness ("Smoothness", Range(0,1)) = 1
        _SpecColor ("Specular", Color) = (.2,.2,.2,1)
        _HueVariation ("Hue Variation", Color) = (0,.9,.825,.1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = .5
        _WindMultipliers ("Leaf Wind Multipliers", Vector) = (.5,2,1,0)
        _BaseWindMultipliers ("Bark Wind Multipliers", Vector) = (.5,2,1,0)
        _LeafNoise ("Leaf Noise", Float) = .1
        [Toggle] _EnableNormalRotation ("Rotate Leaf Normals", Float) = 0
        [Toggle] _EnableAdvancedEdgeBending ("Advanced Edge Bending", Float) = 1
        _AdvancedEdgeBending ("Edge Strength / Frequency", Vector) = (.6,1.3,0,0)
        _TumbleFrequency ("Tumble Frequency", Float) = .6
        _TumbleStrength ("Tumble Strength", Float) = .012
        _LeafTurbulence ("Leaf Turbulence", Float) = .1
        _EdgeFlutterInfluence ("Edge Flutter Influence", Float) = 0
        _Translucency ("Translucency Strength / Power / Distortion", Vector) = (1.5,2,.01,0)
        _AmbientTranslucency ("Ambient Translucency", Float) = 2
        _AlphaLeak ("Billboard Alpha Leak", Range(0,1)) = .6
        _ThicknessRemap ("Billboard Thickness", Float) = 1
        [Toggle] _BlendBB ("Blend Billboard Views", Float) = 1
        [Toggle] _BlendBBDithering ("Dither Billboard Views", Float) = 0
        _WindStrength ("Billboard Wind Strength", Float) = .16
        _WindPower ("Billboard Wind Power", Float) = 1.5
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma multi_compile _ LOD_FADE_CROSSFADE LOD_FADE_PERCENTAGE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #define CTI_ARRAY 1
            #include "CTI URP7 Common.hlsl"
            ENDHLSL
        }
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma multi_compile _ LOD_FADE_CROSSFADE LOD_FADE_PERCENTAGE
            #define CTI_ARRAY 1
            #define CTI_SHADOW 1
            #include "CTI URP7 Common.hlsl"
            ENDHLSL
        }
        Pass {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma multi_compile _ LOD_FADE_CROSSFADE LOD_FADE_PERCENTAGE
            #define CTI_ARRAY 1
            #define CTI_DEPTH 1
            #include "CTI URP7 Common.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
