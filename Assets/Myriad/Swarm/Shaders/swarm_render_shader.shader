﻿Shader "Myriad/swarm_render_shader"
{
	Properties
	{
		u_lighting_ground_falloff_distance("Ground Falloff Distance", Float) = 2.0
		u_lighting_ground_diffuse_color("Ground Diffuse Color", Color) = (0.3, 0.25, 0.1, 1)
		u_lighting_ground_specular_color("Ground Specular Color", Color) = (0.2, 0.1, 0.1, 1)

		u_lighting_sky_diffuse_color("Sky Diffuse Color", Color) = (0.3, 0.3, 0.5, 1)
		u_lighting_sky_specular_color("Sky Specular Color", Color) = (0.3, 0.3, 0.5, 1)
	}

	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }

		LOD 100

		Pass
		{
			CGPROGRAM

			// TODO: Try targetting just "4.5", since that seems that it might be the actual minimum needed.
			// https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
			#pragma target 5.0

			// Identify the entry-point functions.
			#pragma vertex vertex_shader
			#pragma fragment fragment_shader
			
			#include "UnityCG.cginc"

			#include "swarm_shader_types.cginc"

			//#define ENABLE_NEIGHBORHOOD_OVERCROWDING_DEBUGGING
			
			struct s_rasterization_vertex
			{
				float4 projected_position : SV_POSITION;
				float3 world_normal : NORMAL;
				float4 diffuse_color : COLOR0;
				float4 emission_color : COLOR1;
				float4 edge_distances : TEXCOORD0;
				float3 world_position_to_camera : TEXCOORD1;
			};

			uniform StructuredBuffer<s_swarmer_state> u_swarmers;
			uniform StructuredBuffer<s_swarmer_model_vertex> u_swarmer_model_vertices;

			uniform float4x4 u_swarm_to_world_matrix; // We have to supply this manually because DrawProcedural is outside the normal mesh-rendering pipeline, so "unity_ObjectToWorld" is unfortunately always the identity matrix.
			
			uniform float u_lighting_ground_falloff_distance;
			uniform float4 u_lighting_ground_diffuse_color;
			uniform float4 u_lighting_ground_specular_color;

			uniform float4 u_lighting_sky_diffuse_color;
			uniform float4 u_lighting_sky_specular_color;
			
			s_rasterization_vertex vertex_shader(
				uint vertex_index : SV_VertexID,
				uint swarmer_index : SV_InstanceID)
			{
				s_rasterization_vertex result;
				
				s_swarmer_model_vertex model_vertex = u_swarmer_model_vertices[vertex_index];
				s_swarmer_state swarmer_state = u_swarmers[swarmer_index];

				float4x4 model_to_world_matrix = 
					mul(u_swarm_to_world_matrix, swarmer_state.cached_model_to_swarm_matrix);

				float3 world_position = 
					mul(model_to_world_matrix, float4(model_vertex.position, 1.0f)).xyz;

				float3 world_normal = 
					normalize(mul(model_to_world_matrix, float4(model_vertex.normal, 0.0f))).xyz;

				float3 diffuse_light_color = 0.0f;
				{
					float vertical_dot = dot(world_normal, float3(0, 1, 0));
					
					diffuse_light_color += (
						u_lighting_sky_diffuse_color *
						saturate(vertical_dot));

					diffuse_light_color += (
						u_lighting_ground_diffuse_color *
						saturate(-1 * vertical_dot) *
						saturate(1.0f - (world_position.y / u_lighting_ground_falloff_distance))); // HACK! We're hard-coding the ground to Y=0.
				}

				float3 diffuse_surface_color = (diffuse_light_color * model_vertex.albedo_color);

				result.projected_position = mul(UNITY_MATRIX_VP, float4(world_position, 1.0f));
				result.world_normal = world_normal;
				result.diffuse_color = float4(diffuse_surface_color, 1.0f);
				result.emission_color = model_vertex.emission_color;
				result.edge_distances = model_vertex.edge_distances;
				result.world_position_to_camera = (_WorldSpaceCameraPos.xyz - world_position);

				#ifdef ENABLE_NEIGHBORHOOD_OVERCROWDING_DEBUGGING
				result.albedo_color = 
					lerp(
						float4(1, 0, 1, 1),
						result.albedo_color,
						smoothstep(0.5, 1.0f, swarmer_state.debug_accepted_candidates_fraction));
				#endif

				return result;
			}

			float get_specular_fraction(
				float3 light_direction,
				float3 surface_normal,
				float3 surface_position_to_camera_direction)
			{
				float3 reflected_light_ray = reflect(light_direction, surface_normal);
				float cos_to_reflected_light = dot(reflected_light_ray, surface_position_to_camera_direction);
						
				float specular_intensity = (
					(cos_to_reflected_light >= 0.0f) ?
						(cos_to_reflected_light * cos_to_reflected_light * cos_to_reflected_light) : // Hard-coded specular_cofficient.
						0.0f);

				return specular_intensity;
			}
			
			float4 fragment_shader(
				s_rasterization_vertex raster_state) :
				SV_Target
			{
				float distance_to_edge = min(
					min(raster_state.edge_distances[0], raster_state.edge_distances[1]),
					min(raster_state.edge_distances[2], raster_state.edge_distances[3]));

				float edge_fraction = smoothstep(0.05, 0.0, distance_to_edge);

				float3 diffuse_color =
					lerp(
						raster_state.diffuse_color,
						0, // (0.2 * raster_state.albedo_color),
						edge_fraction);

				float3 specular_color = 0.0f;
				{
					float3 surface_position_to_camera_direction = 
						normalize(raster_state.world_position_to_camera);

					specular_color += (
						u_lighting_sky_specular_color *
						get_specular_fraction(
							float3(0, -1, 0),
							raster_state.world_normal,
							surface_position_to_camera_direction));

					specular_color += (
						u_lighting_ground_specular_color *
						get_specular_fraction(
							float3(0, 1, 0),
							raster_state.world_normal,
							surface_position_to_camera_direction));
				}

				float3 result = (
					diffuse_color + 
					specular_color +
					raster_state.emission_color);
				
				// Apply fog.
				{
					float squared_distance_to_camera =
						dot(raster_state.world_position_to_camera, raster_state.world_position_to_camera);

					float camera_proximity = 
						exp2(-1 * (unity_FogParams.x * unity_FogParams.x) * squared_distance_to_camera);

					result.rgb = lerp(unity_FogColor.rgb, result.rgb, saturate(camera_proximity));
				}

				return float4(result, 1);
			}

			ENDCG
		}
	}
}
