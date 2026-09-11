Shader "TaxVenture/OutlineSilhouette"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.5, 0.5, 0.5, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Outline"

            // Only render the back side of the expanded mesh.
            Cull Front

            // We only want the outline to appear around the
            // original object, not write depth over it.
            ZWrite Off
            ZTest LEqual

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _OutlineColor;
                float _OutlineWidth;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Convert position and normal into world space.
                float3 positionWS =
                    TransformObjectToWorld(input.positionOS);

                float3 normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                // Expand the mesh along its surface normal.
                positionWS += normalWS * _OutlineWidth;

                // Convert back to clip space.
                output.positionCS =
                    TransformWorldToHClip(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }
}