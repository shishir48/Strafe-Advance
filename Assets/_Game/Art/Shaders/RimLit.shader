Shader "Strafe/RimLit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.18, 0.4, 0.85, 1)
        _RimColor ("Rim Color", Color) = (0.0, 1.0, 1.0, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.0
        _RimIntensity ("Rim Intensity", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(wp);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewWS = GetWorldSpaceViewDir(wp);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewWS);
                Light ml = GetMainLight();
                float ndl = saturate(dot(N, ml.direction)) * 0.8 + 0.2;
                float3 lit = _BaseColor.rgb * ml.color * ndl + SampleSH(N) * _BaseColor.rgb * 0.4;
                float fres = pow(saturate(1.0 - saturate(dot(N, V))), _RimPower);
                float3 col = lit + _RimColor.rgb * fres * _RimIntensity;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
