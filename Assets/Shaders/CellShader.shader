Shader "ThousandAnt/Cell"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _BaseMap("Main Texture", 2D) = "white" {}
        _EmissionMap("Emission Texture", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)

        _Alpha("Shadow Alpha", Range(0, 1)) = 1

        [HDR] _AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
        [HDR] _SpecularColor("Specular Color", Color) = (0.4, 0.4, 0.4, 1)
        _Glossiness("Glosiness", Float) = 32

        [HDR] _RimColor("Rim Color", Color) = (0.9, 0.9, 0.9, 1)
        _RimStrength("Rim Strength", Range(0, 1)) = 0.7
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
                "PassFlags" = "OnlyDirectional"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct Attributes
            {
                half4 vertex: POSITION;
                half4 uv:     TEXCOORD0;
                half3 normal: NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                half4 pos:     SV_POSITION;
                half3 normal:  NORMAL;
                half2 uv:      TEXCOORD0;
                half3 viewDir: TEXCOORD1;

                SHADOW_COORDS(2)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _BaseMap;
            half4 _BaseMap_ST;

            sampler2D _BumpMap;
            half4 _BumpMap_ST;

            sampler2D _EmissionMap;
            half4 _EmissionMap_ST;

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(half4, _AmbientColor)
                UNITY_DEFINE_INSTANCED_PROP(half4, _SpecularColor)
                UNITY_DEFINE_INSTANCED_PROP(half, _Glossiness)
                UNITY_DEFINE_INSTANCED_PROP(half4, _RimColor)
                UNITY_DEFINE_INSTANCED_PROP(half, _RimStrength)
                UNITY_DEFINE_INSTANCED_PROP(half, _RimThreshold)
                UNITY_DEFINE_INSTANCED_PROP(half4, _EmissionColor)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.pos     = UnityObjectToClipPos(input.vertex);
                output.normal  = UnityObjectToWorldNormal(input.normal);
                output.viewDir = WorldSpaceViewDir(input.vertex);
                output.uv      = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                half3 normal = normalize(input.normal);
                half3 viewDir = normalize(input.viewDir);

                half nDotL = dot(_WorldSpaceLightPos0, normal);
                half shadow = SHADOW_ATTENUATION(input);

                half lightIntensity = smoothstep(0, 0.01, nDotL * shadow);
                half4 light = lightIntensity * _LightColor0;

                half3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
                half nDotH = dot(normal, halfVector);

                half glossiness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Glossiness);

                half specularIntensity = pow(nDotH * lightIntensity, glossiness * glossiness);
                half specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
                half4 specular = specularIntensitySmooth * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecularColor);

                half rimDot = 1.0 - dot(viewDir, normal);
                half rimIntensity = rimDot * pow(nDotL, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimThreshold));

                half rimStr = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimStrength);

                rimIntensity = smoothstep(rimStr - 0.01, rimStr + 0.01, rimIntensity);
                half4 rim = rimIntensity * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimColor);

                half4 sampled = tex2D(_BaseMap, input.uv);
                half4 emission = tex2D(_EmissionMap, input.uv);

                half4 finalColor = sampled + (emission * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor));

                return (light + UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AmbientColor) + specular + rim) *
                    UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color) * finalColor;
            }

            ENDCG
        }

        Pass
        {
            Tags
            {
                "Queue" = "Geometry+1"
            }
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            struct Attributes
            {
                half4 vertex : POSITION;
            };

            struct Varyings
            {
                half4 pos : SV_POSITION;
                SHADOW_COORDS(0)
            };

            half _Alpha;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                TRANSFER_SHADOW(output);

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                half shadow = SHADOW_ATTENUATION(input);

                return half4(0, 0, 0, (1 - shadow) * _Alpha);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
