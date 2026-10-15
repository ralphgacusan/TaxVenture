Shader "Hidden/TaxVenture/InteractionOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.68, 0.70, 0.72, 0.58)
        _OutlineWidth ("Width in Pixels", Range(1, 8)) = 5
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor;
            float _OutlineWidth;
        CBUFFER_END
        ENDHLSL

        // Draw a union of the visible surfaces, including two-sided paper and animated meshes.
        Pass
        {
            Name "Visible silhouette"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One One
            BlendOp Max

            HLSLPROGRAM
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment
            #pragma target 3.0
            #pragma multi_compile_instancing

            TEXTURE2D_X_FLOAT(_InteractionOutlineDepth);
            float _InteractionOutlineStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings MaskVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 MaskFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = SAMPLE_TEXTURE2D_X(_InteractionOutlineDepth, sampler_PointClamp, uv).r;
                // Compare eye-space distances so tolerance does not grow with distance.
                float sceneEye = IsPerspectiveProjection() ? LinearEyeDepth(sceneDepth, _ZBufferParams) : LinearDepthToEyeDepth(sceneDepth);
                float objectEye = IsPerspectiveProjection() ? LinearEyeDepth(input.positionCS.z, _ZBufferParams) : LinearDepthToEyeDepth(input.positionCS.z);
                clip(sceneEye + 0.003 - objectEye);
                return _InteractionOutlineStrength;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Soft gray edge"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment OutlineFragment
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).r;
            }
            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 pixel = rcp(_ScaledScreenParams.xy);
                half inner = 0;
                half outer = 0;
                // Two eight-tap rings soften the outside edge without a blur or glowing fill.
                const float2 directions[8] = {
                    float2(1,0), float2(-1,0), float2(0,1), float2(0,-1),
                    float2(0.7071,0.7071), float2(-0.7071,0.7071),
                    float2(0.7071,-0.7071), float2(-0.7071,-0.7071)
                };
                [unroll] for (int i = 0; i < 8; i++)
                {
                    inner = max(inner, Mask(uv + directions[i] * pixel * max(0.5, _OutlineWidth - 0.75)));
                    outer = max(outer, Mask(uv + directions[i] * pixel * _OutlineWidth));
                }
                half edge = saturate(max(inner, outer * 0.55) - Mask(uv));
                return half4(_OutlineColor.rgb, _OutlineColor.a * edge);
            }
            ENDHLSL
        }
    }
}
