Shader "Graph/Point Surface GPU"
{
    Properties
    {
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        CGPROGRAM
        #pragma surface configure_surface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:configure_procedural
        #pragma editor_sync_compilation
        #pragma target 4.5

        #include "PointGPU.hlsl"

        struct Input
        {
            float3 worldPos;
        };

        float _Smoothness;

        void configure_surface(Input input, inout SurfaceOutputStandard surface)
        {
            surface.Albedo.rgb = saturate(input.worldPos * 0.5 + 0.5);
            surface.Smoothness = _Smoothness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}