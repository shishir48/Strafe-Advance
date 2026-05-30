Shader "Strafe/Dissolve"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5, 0.5, 0.55, 1)
        _EdgeColor ("Edge Color", Color) = (3.0, 1.0, 0.15, 1)
        _Dissolve ("Dissolve", Range(0, 1)) = 0.0
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.08
        _NoiseScale ("Noise Scale", Float) = 8.0
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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _Dissolve;
                float _EdgeWidth;
                float _NoiseScale;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(wp);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float n = vnoise(IN.uv * _NoiseScale);
                clip(n - _Dissolve);
                float edge = smoothstep(_Dissolve + _EdgeWidth, _Dissolve, n);

                Light ml = GetMainLight();
                float3 N = normalize(IN.normalWS);
                float ndl = saturate(dot(N, ml.direction)) * 0.8 + 0.2;
                float3 lit = _BaseColor.rgb * ml.color * ndl + SampleSH(N) * _BaseColor.rgb * 0.3;

                float3 col = lerp(lit, _EdgeColor.rgb, edge);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
