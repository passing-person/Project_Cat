Shader "Custom/Rainbow_20"
{
    Properties
    {
        [Header(Color)]
        _BrightColor("Bright Color", Color) = (1,1,1,1)
        _MidColor("Mid Color", Color) = (0.5,0.5,0.5,1)
        _DarkShadeColor("Dark Shade Color", Color) = (0,0,0,1)
        _DarkZoneColor("Dark Zone Color", Color) = (0,0,0,1)

        [Header(Light)]
        _LightAcceptance("Light Acceptance", Range(0,100)) = 50.0
        _LightDarkShadeRate("Light Dark Shade Rate", Range(0,1000)) = 50
        _ScatterOverSurface("Scatter Over Surface", Range(0,1)) = 0.5

        [Header(Sheen)]
        _SheenColor("Sheen Color", Color) = (1,1,0,1)
        _SheenPower("Sheen Power", Range(0,5)) = 2.0

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _Width("Specular Width", Range(0,0.3829787)) = 0.2
        _Fallof("Specular Fallof", Range(-1,1)) = 0

        [Header(Reflection)]
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.5

        [Header(Normal Map)]
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalMap_ST("Normal Map Tiling/Offset", Vector) = (1,1,0,0)
        _NormalMapStrength("Normal Map Strength", Range(0,1)) = 1

        [Header(Surface Options)]
        [Enum(Off,0,Front,1,Back,2,FrontAndBack,3)]_Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Cull [_Cull]
            Blend SrcAlpha OneMinusSrcAlpha
            //Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            // Required for point/spot additional light realtime shadows (shadowAttenuation in GetAdditionalLight)
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _NORMALMAP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "ColorSpace/OklchColorSpace.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent:TANGENT;
                float2 uv : TEXCOORD0; // Add UV coordinates
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 positionCS : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 tangent : TEXCOORD2;
                float4 worldPos : TEXCOORD3;
                float2 uv : TEXCOORD4; // Add UV coordinates
            };

            float3 blinnPhongSpecular(float3 normal, float3 lightDir, float3 viewDir, float3 specularColor, float specularWidth, float specularFallof)
            {
                float3 halfwayDir = normalize(lightDir + viewDir);
                float NdotH = max(dot(normal, halfwayDir), 0.0);
                float specularIntensity = pow(NdotH, 1.0/(specularWidth+0.0001)/(specularWidth+0.0001)) * exp(specularFallof * NdotH);
                specularIntensity = saturate(specularIntensity);
                return specularColor * specularIntensity;
            }

            float4 _BrightColor;
            float4 _MidColor;
            float4 _DarkShadeColor;
            float4 _DarkZoneColor;
            float4 _SheenColor;
            float _SheenPower;
            float _LightAcceptance;
            float _LightDarkShadeRate;
            float _ScatterOverSurface;
            float4 _SpecularColor;
            float _Width;
            float _Fallof;

            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            float _NormalMapStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos;
                o.normal = mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz;
                o.tangent = mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0)).xyz;
                o.positionCS = ComputeScreenPos(o.pos);
                //o.uv = v.uv; // Pass UV coordinates to the fragment shader
                o.uv = TRANSFORM_TEX(v.uv, _NormalMap);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
            // NORMAL
                float3 normal = UnpackNormal(tex2D(_NormalMap, i.uv));
                float3 bitangent = -cross(i.normal, i.tangent);
                normal=normalize(normal.x * i.tangent + normal.y * bitangent + normal.z * i.normal);
                normal = lerp(normalize(i.normal), normal, _NormalMapStrength);

            // LIGHT
                float totalBrightness = 0;
                float currentBrightness = 0;
                float normalAttenuation = 0;
                float3 totalColor = 0;
                float totalAttenuation = 0;

                // Main Light
                float4 LIGHT_COORDS = TransformWorldToShadowCoord(i.worldPos);
                Light mainLight = GetMainLight(LIGHT_COORDS);
                float NdotL = dot(normal, mainLight.direction);
                normalAttenuation = lerp(clamp(NdotL, 0, 1), (NdotL*0.5)+0.5, _ScatterOverSurface);
                currentBrightness = normalAttenuation
                * mainLight.distanceAttenuation 
                * mainLight.shadowAttenuation
                * (mainLight.color.r+mainLight.color.g+mainLight.color.b)/3.0;
                totalBrightness += currentBrightness;
                totalColor += mainLight.color.rgb * currentBrightness;

                // Additional Lights
                int addLightsCount = GetAdditionalLightsCount();
                float4 addcolor = float4(0,0,0,0);
                float3 WS_Pos = i.worldPos.xyz;
                for (int count = 0; count < addLightsCount; count++)
                {
                    Light addlight = GetAdditionalLight(count, WS_Pos, half4(1,1,1,1)); // 获取每个额外光源的信息
                    float3 addLightDir = normalize(addlight.direction);
                    normalAttenuation = lerp(clamp(dot(normal, addLightDir), 0, 1), (dot(normal, addLightDir) * 0.5 + 0.5), _ScatterOverSurface);
                    currentBrightness = normalAttenuation
                    * (addlight.distanceAttenuation) 
                    * addlight.shadowAttenuation
                    * (addlight.color.r+addlight.color.g+addlight.color.b)/3.0; // 计算每个光源的亮度贡献，并累加
                    totalBrightness += currentBrightness;
                    totalColor += addlight.color.rgb * currentBrightness;
                    totalAttenuation += addlight.distanceAttenuation * (addlight.color.r+addlight.color.g+addlight.color.b)/3.0;
                }
                totalBrightness = 1.0-1.0/(1.0+totalBrightness*_LightAcceptance);
                totalAttenuation = 1.0-1.0/(1.0+totalAttenuation*_LightDarkShadeRate);

            // COLOR
                float4 color = 0;
                float maxtotalColor = max(max(totalColor.r, totalColor.g), totalColor.b);
                float totalColorBrightness = (totalColor.r+totalColor.g+totalColor.b)/3.0;
                float3 brightColor = _BrightColor.rgb;
                float3 midColor = _MidColor.rgb;
                if(totalColorBrightness > 1e-8){
                    totalColor /= totalColorBrightness;
                    brightColor *= totalColor;
                    midColor *= totalColor;
                }
                float3 darkColor = Oklch_LerpRgb(_DarkZoneColor.rgb, _DarkShadeColor.rgb, totalAttenuation);
                if(totalBrightness < 0.5){
                    color = float4(Oklch_LerpRgb(darkColor.rgb, midColor.rgb, totalBrightness*2.0), 1.0);
                }else{
                    color = float4(Oklch_LerpRgb(midColor.rgb, brightColor.rgb, (totalBrightness-0.5)*2.1), 1.0);// add more for bright color
                }
                //color.rgb = totalBrightness;

            // SHEEN
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos.xyz);
                float viewDot = dot(normal, viewDir);
                color += max(_SheenColor * pow(1.0 - abs(viewDot), _SheenPower),0);

            // AO
                float2 screenUV = i.positionCS.xy / i.positionCS.w;
                AmbientOcclusionFactor ambientOcclusion = GetScreenSpaceAmbientOcclusion(screenUV);
                float ao = ambientOcclusion.indirectAmbientOcclusion;
                //ao = 1.0-(1.0-ao)*(1.0-shadow);
                ao = lerp(ao, 1.0, totalBrightness);
                color.rgb *= ao;

            // SPECULAR
                float3 lightDir = normalize(GetAdditionalLight(0, i.worldPos.xyz).direction);
                color.rgb += blinnPhongSpecular(normal, lightDir, viewDir, _SpecularColor.rgb, _Width, _Fallof) * totalBrightness;

                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags { "LightMode"="UniversalGBuffer" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent:TANGENT;
                float2 uv : TEXCOORD0; // Add UV coordinates
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 positionCS : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 tangent : TEXCOORD2;
                float4 worldPos : TEXCOORD3;
                float2 uv : TEXCOORD4; // Add UV coordinates
            };

            float _Smoothness;
            float _Metallic;

            sampler2D _NormalMap;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos;
                o.normal = mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz;
                o.tangent = mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0)).xyz;
                o.positionCS = ComputeScreenPos(o.pos);
                o.uv = v.uv; // Pass UV coordinates to the fragment shader
                return o;
            }

            float3 UnpackNormalMap(float3 normal, float3 tangent, float3 bitangent, float2 uv)
            {
                float3 normalTex = tex2D(_NormalMap, uv).xyz * 2.0 - 1.0;
                return normalize(normalTex.x * tangent + normalTex.y * bitangent + normalTex.z * normal);
            }

            struct MyFragmentOutput
            {
                float4 GBuffer0 : SV_Target0;  // RGB=Albedo, A=遮罩
                float4 GBuffer1 : SV_Target1;  // RGB=Specular, A=光滑度
                float4 GBuffer2 : SV_Target2;  // RGB=世界法线, A=保留
                float4 GBuffer3 : SV_Target3;  // 自发光等
                float4 GBuffer4 : SV_Target4;  // 深度（可选）
            };

            MyFragmentOutput frag (v2f i) : SV_Target
            {
                
                MyFragmentOutput output;
        
                // GBuffer0: Albedo颜色 + 遮罩（这里放alpha）
                output.GBuffer0 = float4(0,0,0,0);
                
                // GBuffer1: Specular颜色 + 光滑度（SSR关键！）
                // 你的Shader没有镜面高光，所以specular设为0
                output.GBuffer1 = float4(_Metallic, 0.0, 0.0, _Smoothness);
                
                // GBuffer2: 世界空间法线（SSR关键！）
                // Calculate tangent space normal using UV mapping
                float3 normal = UnpackNormal(tex2D(_NormalMap, i.uv));
                float3 bitangent = cross(i.normal, i.tangent);
                normal=normalize(normal.x * i.tangent + normal.y * bitangent + normal.z * i.normal);
                float3 worldNormal = normalize(normal);
                // 编码法线到[0,1]范围
                output.GBuffer2 = float4(worldNormal, 1.0);
                
                // GBuffer3: 自发光 + 其他（如环境光遮蔽）
                output.GBuffer3 = float4(0.0, 0.0, 0.0, 1.0);
                
                // GBuffer4: 深度或其他数据（根据需求）
                // 这里可以输出深度值供高级效果使用
                float depth = i.positionCS.z / i.positionCS.w;
                output.GBuffer4 = float4(depth, 0.0, 0.0, 1.0);
                
                return output;
            }
            ENDHLSL
        }
        Pass
       {
           Name "ShadowCast"
           Tags { "LightMode" = "ShadowCaster" }
           HLSLPROGRAM
           #pragma vertex vert
           #pragma fragment frag
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           struct appdata
           {
               float4 vertex : POSITION;
           };
           struct v2f
           {
               float4 pos : SV_POSITION;
           };
           v2f vert(appdata v)
           {
               v2f o;
               o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
               return o;
           }
           float4 frag(v2f i) : SV_Target
           {
               return float4(0.0, 0.0, 0.0, 1.0);
           }
           ENDHLSL
       }
       // 写入深度图 来自Unlit 打开FrameDebugger来查看这些算法细节
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
