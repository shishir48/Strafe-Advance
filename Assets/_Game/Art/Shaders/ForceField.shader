Shader "Strafe/ForceField"
{
    Properties
    {
        _Color ("Core Color", Color) = (0.25, 0.6, 1.0, 1)
        _RimColor ("Rim Color", Color) = (0.6, 0.9, 1.0, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.0
        _Pulse ("Pulse Speed", Float) = 2.0
        _Alpha ("Base Alpha", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RimColor;
                float _RimPower;
                float _Pulse;
                float _Alpha;
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
                float fres = pow(saturate(1.0 - saturate(dot(N, V))), _RimPower);
                float pulse = 0.5 + 0.5 * sin(_Time.y * _Pulse);
                float3 col = _Color.rgb + _RimColor.rgb * fres * (0.6 + 0.4 * pulse);
                float a = saturate(_Alpha + fres);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
