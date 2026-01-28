Shader "Custom/SlimeWobble"
{
    Properties
    {
        _BaseColor ("Base Color (A = Alpha)", Color) = (0.2, 1, 0.5, 0.65)

        _Stretch ("Stretch", Range(0, 1)) = 0.35
        _Squash  ("Squash",  Range(0, 1)) = 0.25

        _Distortion ("Distortion", Range(0, 0.2)) = 0.10
        _FresnelPower ("Fresnel Power", Range(0.5, 10)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 1.0
        _Thickness ("Thickness", Range(0, 2)) = 1.2

        _Spec ("Spec", Range(0, 2)) = 0.8
        _SpecPower ("Spec Power", Range(8, 256)) = 80
        _Rim ("Rim", Range(0, 1)) = 0.25

        [HideInInspector]_DeformDir ("Deform Dir", Vector) = (0,0,1,0)
        [HideInInspector]_DeformAmp ("Deform Amp", Range(-1,1)) = 0
        [HideInInspector]_Wobble    ("Wobble", Range(-1,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float _Stretch;
                float _Squash;

                float _Distortion;
                float _FresnelPower;
                float _FresnelStrength;
                float _Thickness;

                float _Spec;
                float _SpecPower;
                float _Rim;

                float4 _DeformDir;
                float  _DeformAmp;
                float  _Wobble;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
            };

            void DeformOS(in float3 posOS, in float3 normalOS, out float3 outPosOS, out float3 outNormalOS)
            {
                float3 dirWS = normalize(_DeformDir.xyz);
                float3 dirOS = mul((float3x3)unity_WorldToObject, dirWS);
                dirOS = normalize(dirOS + 1e-6);

                float amp = clamp(_DeformAmp, -1.0, 1.0);
                float wob = _Wobble;

                float3 alongP = dirOS * dot(posOS, dirOS);
                float3 perpP  = posOS - alongP;

                float s = 1.0 + (_Stretch * amp) * (1.0 + wob);
                float p = 1.0 - (_Squash  * amp) * (1.0 - wob * 0.7);

                outPosOS = alongP * s + perpP * p;

                float3 n = normalOS;
                float3 alongN = dirOS * dot(n, dirOS);
                float3 perpN  = n - alongN;

                outNormalOS = normalize(alongN / max(1e-4, s) + perpN / max(1e-4, p));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS, nOS;
                DeformOS(IN.positionOS.xyz, IN.normalOS, posOS, nOS);

                VertexPositionInputs vp = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vn = GetVertexNormalInputs(nOS);

                OUT.positionHCS = vp.positionCS;
                OUT.positionWS  = vp.positionWS;
                OUT.normalWS    = normalize(vn.normalWS);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 N = normalize(IN.normalWS);

                float ndv = saturate(dot(N, V));
                float fresnel = pow(1.0 - ndv, _FresnelPower) * _FresnelStrength;

                float2 uv = IN.screenPos.xy / IN.screenPos.w;

                float distort = _Distortion * (0.25 + 0.75 * saturate(fresnel));
                float2 offset = normalize(N.xy + 1e-6) * distort;

                half3 sceneCol = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + offset).rgb;

                float thickness = saturate(_Thickness * (0.25 + 0.75 * saturate(fresnel)));
                half3 col = lerp(sceneCol, sceneCol * _BaseColor.rgb, thickness);

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), _SpecPower) * _Spec * mainLight.shadowAttenuation;

                col += spec;
                col += fresnel * _Rim;

                half alpha = saturate(_BaseColor.a + fresnel * 0.10);

                return half4(col, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float _Stretch;
                float _Squash;

                float _Distortion;
                float _FresnelPower;
                float _FresnelStrength;
                float _Thickness;

                float _Spec;
                float _SpecPower;
                float _Rim;

                float4 _DeformDir;
                float  _DeformAmp;
                float  _Wobble;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            void DeformOS(in float3 posOS, in float3 normalOS, out float3 outPosOS, out float3 outNormalOS)
            {
                float3 dirWS = normalize(_DeformDir.xyz);
                float3 dirOS = mul((float3x3)unity_WorldToObject, dirWS);
                dirOS = normalize(dirOS + 1e-6);

                float amp = clamp(_DeformAmp, -1.0, 1.0);
                float wob = _Wobble;

                float3 alongP = dirOS * dot(posOS, dirOS);
                float3 perpP  = posOS - alongP;

                float s = 1.0 + (_Stretch * amp) * (1.0 + wob);
                float p = 1.0 - (_Squash  * amp) * (1.0 - wob * 0.7);

                outPosOS = alongP * s + perpP * p;

                float3 n = normalOS;
                float3 alongN = dirOS * dot(n, dirOS);
                float3 perpN  = n - alongN;

                outNormalOS = normalize(alongN / max(1e-4, s) + perpN / max(1e-4, p));
            }

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;

                float3 posOS, nOS;
                DeformOS(IN.positionOS.xyz, IN.normalOS, posOS, nOS);

                VertexPositionInputs vp = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vn = GetVertexNormalInputs(nOS);

                float3 positionWS = vp.positionWS;
                float3 normalWS = normalize(vn.normalWS);

                OUT.positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
