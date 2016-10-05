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
				UNITY_FOG_COORDS(0)
			};
			
			s_rasterization_vertex vertex_shader(
				float3 position : POSITION)
			{
				s_rasterization_vertex result;

				result.projected_position = mul(UNITY_MATRIX_MVP, float4(position, 1));
				
				UNITY_TRANSFER_FOG(result, result.projected_position);
				
				return result;
			}
			
			float4 fragment_shader(
				s_rasterization_vertex raster_state) : 
					SV_Target
			{
				float4 result = float4(1, 1, 1, 1);
				
				UNITY_APPLY_FOG(raster_state.fogCoord, result);

				return result;
			}

			ENDCG
		}
	}
}
