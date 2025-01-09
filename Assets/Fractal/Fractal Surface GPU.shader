Shader "Graph/Fractal Surface GPU"
{
    SubShader
    {
        CGPROGRAM
        #pragma surface configure_surface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:configure_procedural
        #pragma editor_sync_compilation
        #pragma target 4.5

        #include "FractalGPU.hlsl"

        struct Input
        {
            float3 worldPos;
        };

        float _Smoothness;

        void configure_surface(Input input, inout SurfaceOutputStandard surface)
        {
            surface.Albedo = GetFractalColor().rgb;
            surface.Smoothness = GetFractalColor().a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}