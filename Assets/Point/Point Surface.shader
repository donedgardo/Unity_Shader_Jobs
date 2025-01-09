Shader "Graph/Point Surface"
{
	Properties
	{
		_Smoothness  ("Smoothness", Range(0,1)) = 0.5
	}
    SubShader
    {
        CGPROGRAM
   		#pragma surface configure_surface Standard fullforwardshadows
   		#pragma target 3.0

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

