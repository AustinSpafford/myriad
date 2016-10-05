Shader "Myriad/ceiling_shader"
{
	Properties
	{
		u_tile_edge_length("TileEdgeLength", Float) = 0.5

		u_pit_edge_length_fraction("PitEdgeLengthFraction", Range(0, 1)) = 0.8
		u_pit_depth_to_width_ratio("PitDepthToWidthRatio", Float) = 0.5

		u_surface_color("SurfaceColor", Color) = (0.5, 0.5, 1, 1)

		u_pit_side_color("PitSideColor", Color) = (0.5, 1, 1, 1)
		u_pit_bottom_color("PitBottomColor", Color) = (1, 0.5, 0.5, 1)
		u_pit_fog_color("PitFogColor", Color) = (1, 1, 0.5, 1)
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
			
			#include "UnityCG.cginc"

			static const float k_one_over_sin_60 = 1.15470f; // http://www.wolframalpha.com/input/?i=1+%2F+(sin+60)
			
			struct s_rasterization_vertex
			{
				float4 projected_position : SV_POSITION;
				float3 world_normal : NORMAL;
				float3 world_tangent : TANGENT;
				float3 world_binormal : BINORMAL;
				float2 texture_coord : TEXCOORD0;
				float3 world_position_to_camera : TEXCOORD1;
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
				float3 normal : NORMAL,
				float3 tangent : TANGENT,
				float2 texture_coord : TEXCOORD0)
			{
				s_rasterization_vertex result;

				float3 binormal = cross(normal, tangent);

				result.projected_position = mul(UNITY_MATRIX_MVP, position);
				result.world_normal = normalize(mul(unity_ObjectToWorld, normal));
				result.world_tangent = normalize(mul(unity_ObjectToWorld, tangent));
				result.world_binormal = normalize(mul(unity_ObjectToWorld, binormal));
				result.world_position_to_camera = (_WorldSpaceCameraPos - mul(unity_ObjectToWorld, position)).xyz;
				result.texture_coord = ((texture_coord - 0.5f) / u_tile_edge_length);
				
				return result;
			}

			float3 convert_square_coord_to_triangle_coord(
				float2 square_coord)
			{
				// Skew the coordinate system into (60, 120, 60, 120) parallelograms.
				float2 skewed_square_coord = frac(float2(
					(square_coord.x * k_one_over_sin_60),
					(square_coord.y + (square_coord.x * 0.5f))));

				// Split the parallelogram into two equalateral triangles.
				float3 triangle_coord =
					(skewed_square_coord.x < skewed_square_coord.y) ?
						float3(
							skewed_square_coord.x,
							(skewed_square_coord.y - skewed_square_coord.x),
							(1.0f - skewed_square_coord.y)) :
						float3(
							(1.0f - skewed_square_coord.x),
							(skewed_square_coord.x - skewed_square_coord.y),
							skewed_square_coord.y);

				return triangle_coord;
			}
			
			float4 fragment_shader(
				s_rasterization_vertex raster_state) : 
					SV_Target
			{
				float3 triangle_coord = 
					convert_square_coord_to_triangle_coord(raster_state.texture_coord.xy);

				float distance_from_edge = min(triangle_coord.x, min(triangle_coord.y, triangle_coord.z));

				float3 albedo_color = u_surface_color;
				float3 world_normal = normalize(raster_state.world_normal);
				float3 world_tangent = normalize(raster_state.world_tangent);
				float3 world_binormal = normalize(raster_state.world_binormal);

				// If we're rendering the top of the pit.
				if (distance_from_edge >= lerp((1.0f / 3.0f), 0.0f, u_pit_edge_length_fraction))
				{
					float3 world_direction_towards_camera = normalize(raster_state.world_position_to_camera);

					albedo_color = (u_pit_bottom_color * dot(world_direction_towards_camera, world_normal));
				}

				float4 result = float4(albedo_color, 1);
				
				// Apply fog.
				{
					float squared_distance_to_camera =
						dot(raster_state.world_position_to_camera, raster_state.world_position_to_camera);

					float camera_proximity = 
						exp2(-1 * (unity_FogParams.x * unity_FogParams.x) * squared_distance_to_camera);

					result.rgb = lerp(unity_FogColor.rgb, result.rgb, saturate(camera_proximity));
				}

				// Debug-views.
				//result = float4(triangle_coord, 1);
				//result = float4(world_normal, 1);

				return result;
			}

			ENDCG
		}
	}
}


