Shader "Custom/URP/BoxPerFace"
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
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float2 uv = input.uv;
                float4 color = float4(1,1,1,1);

                if (n.y > 0.9)
                    color = _TopColor * tex2D(_TopTex, uv);
                else if (n.y < -0.9)
                    color = _BottomColor * tex2D(_BottomTex, uv);
                else if (n.x > 0.9)
                    color = _RightColor * tex2D(_RightTex, uv);
                else if (n.x < -0.9)
                    color = _LeftColor * tex2D(_LeftTex, uv);
                else if (n.z > 0.9)
                    color = _FrontColor * tex2D(_FrontTex, uv);
                else if (n.z < -0.9)
                    color = _BackColor * tex2D(_BackTex, uv);

                return color;
            }

            ENDHLSL
        }
    }
}