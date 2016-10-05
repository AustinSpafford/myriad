Shader "Myriad/ground_shader"
{
	Properties
	{
		u_tile_edge_length ("TileEdgeLength", Float) = 0.5
		
		u_pit_edge_length_fraction ("PitEdgeLengthFraction", Range(0, 1)) = 0.8
		u_pit_depth_to_width_ratio ("PitDepthToWidthRatio", Float) = 0.5

		u_surface_color ("SurfaceColor", Color) = (0.5, 0.5, 1, 1)

		u_pit_side_color ("PitSideColor", Color) = (0.5, 1, 1, 1)
		u_pit_bottom_color ("PitBottomColor", Color) = (1, 0.5, 0.5, 1)
		u_pit_fog_color ("PitFogColor", Color) = (1, 1, 0.5, 1)
	}

	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }

		LOD 100

		Pass
		{
			CGPROGRAM

			// TODO: Try targetting a lower version, since that seems that it might be the actual minimum needed.
			// https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
			#pragma target 5.0

			// Identify the entry-point functions.
			#pragma vertex vertex_shader
			#pragma fragment fragment_shader

			// Enable support for fog-calculations.
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			
			struct s_rasterization_vertex
			{
				float4 position : SV_POSITION;
				float4 world_normal : NORMAL;
				float2 texture_coord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

			uniform float u_tile_edge_length;
		
			uniform float u_pit_edge_length_fraction;
			uniform float u_pit_depth;

			uniform float4 u_surface_color;

			uniform float4 u_pit_side_color;
			uniform float4 u_pit_bottom_color;
			uniform float4 u_pit_fog_color;

			s_rasterization_vertex vertex_shader(
				float4 position : POSITION,
				float4 normal : NORMAL,
				float2 texture_coord : TEXCOORD0)
			{
				s_rasterization_vertex result;

				result.position = mul(UNITY_MATRIX_MVP, position);
				result.world_normal = normalize(mul(unity_ObjectToWorld, normal));
				result.texture_coord = ((texture_coord - 0.5f) / u_tile_edge_length);
				
				UNITY_TRANSFER_FOG(result, result.position);
				
				return result;
			}
			
			fixed4 fragment_shader(
				s_rasterization_vertex raster_state) : 
					SV_Target
			{
				fixed4 result = (
					u_surface_color *
					fixed4(frac(raster_state.texture_coord.xy), 0, 1));
				
				UNITY_APPLY_FOG(raster_state.fogCoord, result);

				return result;
			}

			ENDCG
		}
	}
}
