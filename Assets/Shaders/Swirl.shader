// Fundo em redemoinho portado do passe de tela do Rebound: tinta torcida em volta do centro,
// dobrada sobre si mesma algumas vezes, em blocos de dois pixels logicos e tres tons chapados.
// Fica preso a tela (usa a posicao de pixel), entao nao anda com a camera.
Shader "ColdEmber/Swirl"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            float _CE_Time;
            float _CE_Pulse;
            float _CE_Dim;
            float4 _CE_Logical;
            float4 _CE_Bg0;
            float4 _CE_Bg1;
            float4 _CE_Bg2;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                return o;
            }

            float3 backdrop(float2 uv)
            {
                float2 grid = _CE_Logical.xy * 0.5;
                float2 p = (floor(uv * grid) + 0.5) / grid;
                float2 q = (p - 0.5) * float2(_CE_Logical.x / _CE_Logical.y, 1.0) * 2.3;
                float t = _CE_Time * 0.06;
                float r = length(q);
                float a = atan2(q.y, q.x) + 0.75 * sin(r * 1.3 - t * 2.0) + t * 0.8;
                q = r * float2(cos(a), sin(a));
                float fold = 0.62 + _CE_Pulse * 0.3;
                [unroll] for (int i = 1; i < 4; i++)
                {
                    float f = (float)i;
                    q += fold / f * float2(
                        sin(q.y * f * 1.3 + t * 3.0 + f * 1.7),
                        cos(q.x * f * 1.1 - t * 2.3 + f * 2.9));
                }
                float v = 0.5 + 0.5 * sin(q.x * 1.15 + q.y * 0.9 + sin(q.y * 0.7 - t));
                v = saturate(v + _CE_Pulse * 0.14);
                if (v < 0.3) return _CE_Bg0.rgb;
                if (v < 0.56) return _CE_Bg1.rgb;
                if (v < 0.82) return lerp(_CE_Bg1.rgb, _CE_Bg2.rgb, 0.5);
                return _CE_Bg2.rgb;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionCS.xy / _ScaledScreenParams.xy;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif
                return half4(backdrop(uv) * (1.0 - _CE_Dim), 1.0);
            }
            ENDHLSL
        }
    }
}
