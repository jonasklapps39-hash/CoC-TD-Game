// Cel-Shading mit Umrisslinien im Grafikstil 7 (Bloons-TD6-Look), für die Built-in-Render-Pipeline.
// Liegt im Ordner Resources, damit Unity den Shader auch in Builds mitnimmt (Shader.Find).
Shader "MuhDefense/Toon"
{
    Properties
    {
        _Color ("Farbe", Color) = (1, 1, 1, 1)
        _UseVertexColor ("Vertexfarben nutzen", Float) = 0
        _ShadowColor ("Schattenfarbe", Color) = (0.58, 0.52, 0.68, 1)
        _ShadowThreshold ("Schattengrenze", Range(-1, 1)) = 0.05
        _ShadowSoftness ("Weiche Kante", Range(0.001, 0.5)) = 0.06
        _AmbientStrength ("Umgebungslicht", Range(0, 1)) = 0.35
        _RimStrength ("Randlicht", Range(0, 1)) = 0.2
        _OutlineColor ("Umrissfarbe", Color) = (0.14, 0.07, 0.03, 1)
        _OutlineWidth ("Umrissbreite (Pixel)", Range(0, 8)) = 2.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "TOON"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _Color;
            float _UseVertexColor;
            fixed4 _ShadowColor;
            float _ShadowThreshold;
            float _ShadowSoftness;
            float _AmbientStrength;
            float _RimStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 color : COLOR;
                SHADOW_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float4 c = v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb = GammaToLinearSpace(c.rgb);
                #endif
                o.color = lerp(float4(1, 1, 1, 1), c, _UseVertexColor);
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float ndl = dot(n, l);
                float shadow = SHADOW_ATTENUATION(i);
                // Zwei Farbstufen mit weicher Kante statt realistischem Verlauf
                float lit = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, ndl) * shadow;
                fixed3 albedo = _Color.rgb * i.color.rgb;
                fixed3 col = albedo * lerp(_ShadowColor.rgb, _LightColor0.rgb, lit);
                col += albedo * ShadeSH9(float4(n, 1)) * _AmbientStrength;
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float rim = pow(1 - saturate(dot(viewDir, n)), 3) * _RimStrength * lit;
                col += rim;
                return fixed4(col, 1);
            }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Rückseiten in Bildschirm-Richtung der Normale verschieben: gleich breite Linie, egal wie weit weg
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float2 clipNormal = mul((float3x3)UNITY_MATRIX_VP, worldNormal).xy;
                float len = length(clipNormal);
                if (len > 0.0001)
                {
                    o.pos.xy += clipNormal / len * (_OutlineWidth * 2.0 / _ScreenParams.xy) * o.pos.w;
                }
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }

    Fallback "Diffuse"
}
