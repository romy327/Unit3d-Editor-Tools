Shader "Custom/URP/BoxPerFace_Lit"
{
    Properties
    {
        _TopColor("Top Color", Color) = (1,1,1,1)
        _BottomColor("Bottom Color", Color) = (1,1,1,1)
        _LeftColor("Left Color", Color) = (1,1,1,1)
        _RightColor("Right Color", Color) = (1,1,1,1)
        _FrontColor("Front Color", Color) = (1,1,1,1)
        _BackColor("Back Color", Color) = (1,1,1,1)

        _TopTex("Top Texture", 2D) = "white" {}
        _BottomTex("Bottom Texture", 2D) = "white" {}
        _LeftTex("Left Texture", 2D) = "white" {}
        _RightTex("Right Texture", 2D) = "white" {}
        _FrontTex("Front Texture", 2D) = "white" {}
        _BackTex("Back Texture", 2D) = "white" {}

        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        HLSLPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/PBR.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS : NORMAL;
            float3 positionWS : TEXCOORD0;
            float2 uv : TEXCOORD1;
        };

        // Colors
        float4 _TopColor;
        float4 _BottomColor;
        float4 _LeftColor;
        float4 _RightColor;
        float4 _FrontColor;
        float4 _BackColor;

        // Textures
        sampler2D _TopTex;
        sampler2D _BottomTex;
        sampler2D _LeftTex;
        sampler2D _RightTex;
        sampler2D _FrontTex;
        sampler2D _BackTex;

        float _Metallic;
        float _Smoothness;

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
            OUT.uv = IN.uv;
            return OUT;
        }

        float4 frag(Varyings IN) : SV_Target
        {
            float3 n = normalize(IN.normalWS);
            float2 uv = IN.uv;
            float4 baseColor = float4(1,1,1,1);

            if (n.y > 0.9)
                baseColor = _TopColor * tex2D(_TopTex, uv);
            else if (n.y < -0.9)
                baseColor = _BottomColor * tex2D(_BottomTex, uv);
            else if (n.x > 0.9)
                baseColor = _RightColor * tex2D(_RightTex, uv);
            else if (n.x < -0.9)
                baseColor = _LeftColor * tex2D(_LeftTex, uv);
            else if (n.z > 0.9)
                baseColor = _FrontColor * tex2D(_FrontTex, uv);
            else if (n.z < -0.9)
                baseColor = _BackColor * tex2D(_BackTex, uv);

            // --- URP Lit PBR ---
            SurfaceData surfaceData;
            surfaceData.baseColor = baseColor.rgb;
            surfaceData.metallic = _Metallic;
            surfaceData.smoothness = _Smoothness;
            surfaceData.normalWS = n;

            float3 viewDirWS = normalize(_WorldSpaceCameraPos - IN.positionWS);
            float3 lighting = LightingStandard(surfaceData, n, viewDirWS);

            return float4(lighting, 1);
        }

        ENDHLSL
    }
}