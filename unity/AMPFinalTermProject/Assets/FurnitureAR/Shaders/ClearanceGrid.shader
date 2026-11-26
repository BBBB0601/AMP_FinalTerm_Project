// [SFR-006] 통행 동선 가이드 바닥 표시: 노란 반투명 면 + 10cm 격자 + 점선 테두리 (URP Unlit)
// 메시에서 받는 값 (cshFurniture.MakeGuide)
//   uv0   : 가이드 영역 안에서의 위치 (m). 왼쪽아래 모서리가 (0,0)
//   uv1   : 가이드 영역 크기 (m) (가로, 세로)
//   color : 테두리를 그릴 변 (r=왼쪽, g=오른쪽, b=아래, a=위). 사방 모드에서 이어 붙인 변은 0
Shader "RoomFitAR/ClearanceGrid"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (1, 0.85, 0, 0.18)
        _LineColor ("Line Color", Color) = (1, 0.85, 0, 0.95)
        _GridSize ("Grid Size (m)", Float) = 0.1
        _GridLineWidth ("Grid Line Width (m)", Float) = 0.004
        _GridStrength ("Grid Line Strength", Range(0, 1)) = 0.55
        _BorderWidth ("Border Width (m)", Float) = 0.015
        _DashLength ("Dash Length (m)", Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ClearanceGrid"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _LineColor;
                float _GridSize;
                float _GridLineWidth;
                float _GridStrength;
                float _BorderWidth;
                float _DashLength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 size : TEXCOORD1;
                half4 edgeMask : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 size : TEXCOORD1;
                half4 edgeMask : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.size = input.size;
                output.edgeMask = input.edgeMask;
                return output;
            }

            // dist(m)가 halfWidth 안쪽이면 1, 바깥이면 0 (경계는 화면 픽셀 크기만큼 부드럽게)
            float LineMask(float dist, float halfWidth)
            {
                float aa = max(fwidth(dist), 1e-5);
                return 1.0 - smoothstep(halfWidth - aa, halfWidth + aa, dist);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 size = input.size;

                // 10cm 격자: 가장 가까운 격자선까지 거리(m)
                float2 toGrid = abs(frac(uv / _GridSize + 0.5) - 0.5) * _GridSize;
                float grid = max(LineMask(toGrid.x, _GridLineWidth * 0.5), LineMask(toGrid.y, _GridLineWidth * 0.5));

                // 점선: 대시 길이만큼 켜고 끄기를 반복
                float dashAlongX = step(frac(uv.x / (_DashLength * 2.0)), 0.5);
                float dashAlongY = step(frac(uv.y / (_DashLength * 2.0)), 0.5);

                float left = LineMask(uv.x, _BorderWidth) * dashAlongY * input.edgeMask.r;
                float right = LineMask(size.x - uv.x, _BorderWidth) * dashAlongY * input.edgeMask.g;
                float bottom = LineMask(uv.y, _BorderWidth) * dashAlongX * input.edgeMask.b;
                float top = LineMask(size.y - uv.y, _BorderWidth) * dashAlongX * input.edgeMask.a;
                float border = max(max(left, right), max(bottom, top));

                float lineAmount = max(grid * _GridStrength, border);
                return lerp(_FillColor, _LineColor, lineAmount);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
