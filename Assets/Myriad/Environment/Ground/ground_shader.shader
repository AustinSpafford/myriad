Shader "Myriad/ground_shader"
{
	Properties
	{
		u_tile_edge_length("Tile Edge Length", Float) = 0.5

		u_pit_edge_length_fraction("Pit Edge Length Fraction", Range(0, 1)) = 0.8
		u_pit_depth_to_width_ratio("Pit Depth-to-Width Ratio", Float) = 0.5

		u_surface_color("Surface Color", Color) = (0.5, 0.5, 1, 1)

		u_pit_side_color("Pit Side Color", Color) = (0.5, 1, 1, 1)
		u_pit_bottom_color("Pit Bottom Color", Color) = (1, 0.5, 0.5, 1)
		u_pit_fog_color("Pit Fog Color", Color) = (1, 1, 0.5, 1)
		u_pit_specular_color("Pit Specular Color", Color) = (1, 1, 1, 1)
		u_pit_specular_intensity("Pit Specular Intensity", Range(0, 1)) = 1.0
		u_pit_specular_power("Pit Specular Power", Range(1, 50)) = 3.0
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
			uniform float4 u_pit_specular_color;			
			uniform float u_pit_specular_intensity;
			uniform float u_pit_specular_power;

			s_rasterization_vertex vertex_shader(
				float4 position : POSITION,
				float3 normal : NORMAL,
				float3 tangent : TANGENT,
				float2 texture_coord : TEXCOORD0)
			{
				s_rasterization_vertex result;

				float3 binormal = cross(tangent, normal);

				result.projected_position = UnityObjectToClipPos(position);
				result.world_normal = normalize((float4)mul(unity_ObjectToWorld, normal));
				result.world_tangent = normalize((float4)mul(unity_ObjectToWorld, tangent));
				result.world_binormal = normalize((float4)mul(unity_ObjectToWorld, binormal));
				result.world_position_to_camera = (_WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, position).xyz);
				result.texture_coord = ((texture_coord - 0.5f) / u_tile_edge_length);
				
				return result;
			}

			void convert_square_coord_to_triangle_coord(
				float2 square_coord,
				out float3 out_triangle_coord,
				out float out_square_coord_tangent_radians)
			{
				// Skew the coordinate system into (60, 120, 60, 120) parallelograms.
				float2 skewed_square_coord = frac(float2(
					(square_coord.x * k_one_over_sin_60),
					(square_coord.y + ((square_coord.x * k_one_over_sin_60) * 0.5f))));

				// Split the parallelogram into two equalateral triangles.
				out_triangle_coord =
					(skewed_square_coord.x < skewed_square_coord.y) ?
						float3(
							skewed_square_coord.x,
							(skewed_square_coord.y - skewed_square_coord.x),
							(1.0f - skewed_square_coord.y)) :
						float3(
							(1.0f - skewed_square_coord.x),
							(skewed_square_coord.x - skewed_square_coord.y),
							skewed_square_coord.y);

				out_square_coord_tangent_radians =
					(skewed_square_coord.x > skewed_square_coord.y) ?
						radians(60.0f) :
						radians(240.0f);
			}
			
			float4 fragment_shader(
				s_rasterization_vertex raster_state) : 
					SV_Target
			{
				float3 triangle_coord;
				float square_coord_tangent_radians;
				convert_square_coord_to_triangle_coord(
					raster_state.texture_coord.xy, 
					triangle_coord,
					square_coord_tangent_radians);

				float distance_from_edge = min(triangle_coord.x, min(triangle_coord.y, triangle_coord.z));

				float3 albedo_color = u_surface_color;
				float3 world_normal = normalize(raster_state.world_normal);
				float3 world_tangent = normalize(raster_state.world_tangent);
				float3 world_binormal = normalize(raster_state.world_binormal);

				// If we're rendering the middle of the cell (the opening of the pit).
				if (distance_from_edge >= lerp((1.0f / 3.0f), 0.0f, u_pit_edge_length_fraction))
				{
					float3 world_direction_towards_camera = normalize(raster_state.world_position_to_camera);

					float3 xy_edge_normal =
						(world_tangent * cos(square_coord_tangent_radians)) +
						(world_binormal * sin(square_coord_tangent_radians));
					
					float3 yz_edge_normal =
						(world_tangent * cos(square_coord_tangent_radians + radians(120.0f))) +
						(world_binormal * sin(square_coord_tangent_radians + radians(120.0f)));					
					
					float3 zx_edge_normal =
						(world_tangent * cos(square_coord_tangent_radians + radians(240.0f))) +
						(world_binormal * sin(square_coord_tangent_radians + radians(240.0f)));

					float3 edge_weights = smoothstep(0.5f, 0.0f, triangle_coord);

					float3 naive_pit_world_normal =
						(xy_edge_normal * edge_weights.z) +
						(yz_edge_normal * edge_weights.x) +
						(zx_edge_normal * edge_weights.y);

					float3 softened_pit_world_normal =
						normalize(lerp(naive_pit_world_normal, world_normal, 0.5f));

					float3 light_direction = float3(0.0f, -1.0f, 0.0f);

					float diffuse_fraction = 
						saturate(-1 * dot(light_direction, softened_pit_world_normal));

					float specular_fraction = (
						u_pit_specular_intensity *
						pow(
							saturate(dot(
								world_direction_towards_camera,
								reflect(light_direction, softened_pit_world_normal))),
							u_pit_specular_power));

					albedo_color = (
						(u_pit_bottom_color * diffuse_fraction) +
						(u_pit_specular_color * specular_fraction));
					
					//albedo_color = smoothstep(-1.0f, 1.0f, softened_pit_world_normal);
					//albedo_color = smoothstep(-1.0f, 1.0f, xy_edge_normal) * edge_weights.z;
					//albedo_color = xy_edge_normal * edge_weights.z;
					//albedo_color = world_binormal;
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
