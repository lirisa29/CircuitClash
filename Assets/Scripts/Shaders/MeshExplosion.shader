Shader "Custom/URP_MeshExplosion"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ExplosionStrength ("Explosion Strength", Range(0,1)) = 0
        _ExplosionIntensity ("Explosion Intensity", Float) = 5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _BaseColor;
            float _ExplosionStrength;
            float _ExplosionIntensity;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // Explosion direction from object center
                float3 center = float3(0,0,0);
                float3 dir = normalize(IN.positionOS.xyz - center);

                // Add slight randomness per vertex
                float noise = frac(sin(dot(IN.positionOS.xyz, float3(12.9898, 78.233, 45.164))) * 43758.5453);

                // Displace vertex outward
                IN.positionOS.xyz += (dir + noise * 0.5) * _ExplosionStrength * _ExplosionIntensity;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                return color;
            }

            ENDHLSL
        }
    }
}
