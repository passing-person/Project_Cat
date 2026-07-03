Shader "Custom/OklchLutTest"
{
    Properties
    {
        _TestColor("Test Color", Color) = (1, 0, 0, 1)
        _TestColor2("Test Color2", Color) = (0, 1, 0, 1)
        [ToggleUI] _RoundTrip("Round Trip (OKLCH encode/decode)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./OklchColorSpace.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TestColor;
                half _RoundTrip;
                half4 _TestColor2;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 rgb1 = _TestColor.rgb;
                half3 rgb2 = _TestColor2.rgb;

                half3 oklch1 = Oklch_LinearRgbToOklch(rgb1);
                half3 oklch2 = Oklch_LinearRgbToOklch(rgb2);

                if(input.uv.x < 0.5)
                {
                    half4 lerpRGB = lerp(half4(rgb1, 1.0), half4(rgb2, 1.0), 1.0-input.uv.y);
                    return lerpRGB;
                }
                else
                {
                    half3 lerpOKLCH = Oklch_LerpOklch(half4(oklch1, 1.0), half4(oklch2, 1.0), 1.0-input.uv.y);
                    float3 rgb = Oklch_OklchToLinearRgb(lerpOKLCH.rgb);
                    return half4(rgb, 1.0);
                }
            }
            ENDHLSL
        }
    }
}
