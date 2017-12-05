Shader "Myriad/fog_only_shader"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }

		LOD 100

		Pass
		{
			CGPROGRAM

			// Identify the entry-point functions.
			#pragma vertex vertex_shader
			#pragma fragment fragment_shader

			// Enable support for fog-calculations.
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct s_rasterization_vertex
			{
				float4 projected_position : SV_POSITION;
			};
			
			s_rasterization_vertex vertex_shader(
				float3 position : POSITION)
			{
				s_rasterization_vertex result;

				result.projected_position = UnityObjectToClipPos(float4(position, 1));
				
				return result;
			}
			
			float4 fragment_shader(
				s_rasterization_vertex raster_state) : 
					SV_Target
			{
				return unity_FogColor;
			}

			ENDCG
		}
	}
}
