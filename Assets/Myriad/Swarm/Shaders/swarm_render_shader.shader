Shader "Myriad/swarm_render_shader"
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

			// TODO: Try targetting just "4.5", since that seems that it might be the actual minimum needed.
			// https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
			#pragma target 5.0

			// Identify the entry-point functions.
			#pragma vertex vertex_shader
			#pragma fragment fragment_shader
			
			#include "UnityCG.cginc"

			#include "swarm_shader_types.cginc"

			#define ENABLE_NEIGHBORHOOD_OVERCROWDING_DEBUGGING
			
			struct s_rasterization_vertex
			{
				float4 projected_position : SV_POSITION;
				float4 world_normal : NORMAL;
				float4 albedo_color : COLOR0;
				float3 edge_distances : TEXCOORD0;
				float3 world_position_to_camera : TEXCOORD1;
			};

			uniform sampler2D u_main_texture;
			uniform float4 u_main_texture_ST; // Contains texture's (scale.x, scale.y, offset.x, offset.y)
			uniform float4 u_color;

			uniform StructuredBuffer<s_swarmer_state> u_swarmers;
			uniform StructuredBuffer<s_swarmer_model_vertex> u_swarmer_model_vertices;

			uniform float4x4 u_swarm_to_world_matrix; // We have to supply this manually because DrawProcedural is outside the normal mesh-rendering pipeline, so "unity_ObjectToWorld" is unfortunately always the identity matrix.
			
			s_rasterization_vertex vertex_shader(
				uint vertex_index : SV_VertexID,
				uint swarmer_index : SV_InstanceID)
			{
				s_rasterization_vertex result;
				
				s_swarmer_model_vertex model_vertex = u_swarmer_model_vertices[vertex_index];
				s_swarmer_state swarmer_state = u_swarmers[swarmer_index];

				float4x4 model_to_world_matrix = 
					mul(u_swarm_to_world_matrix, swarmer_state.cached_model_to_swarm_matrix);

				float4x4 model_to_perspective_matrix =
					mul(UNITY_MATRIX_VP, model_to_world_matrix);

				result.projected_position = mul(model_to_perspective_matrix, float4(model_vertex.position, 1.0f));
				result.world_normal = normalize(mul(model_to_world_matrix, float4(model_vertex.normal, 0.0f)));
				result.albedo_color = model_vertex.albedo_color;
				result.edge_distances = model_vertex.edge_distances;
				result.world_position_to_camera = (_WorldSpaceCameraPos - mul(model_to_world_matrix, float4(model_vertex.position, 1.0f))).xyz;

				#ifdef ENABLE_NEIGHBORHOOD_OVERCROWDING_DEBUGGING
				result.albedo_color = 
					lerp(
						float4(1, 0, 1, 1),
						result.albedo_color,
						smoothstep(0.5, 1.0f, swarmer_state.debug_accepted_candidates_fraction));
				#endif

				return result;
			}
			
			float4 fragment_shader(
				s_rasterization_vertex raster_state) : 
					SV_Target
			{
				float distance_to_edge = 
					min(raster_state.edge_distances.x, min(raster_state.edge_distances.y, raster_state.edge_distances.z));

				float case_is_edge = smoothstep(0.05, 0.0, distance_to_edge);

				float3 surface_color =
					lerp(
						raster_state.albedo_color,
						0, // (0.2 * raster_state.albedo_color),
						case_is_edge);

				// TODO: Better-than-debug lighting.
				surface_color *= saturate(dot(raster_state.world_normal, float4(0, 1, 0, 0)));

				float4 result = float4(surface_color, 1);
				
				// Apply fog.
				{
					float squared_distance_to_camera =
						dot(raster_state.world_position_to_camera, raster_state.world_position_to_camera);

					float camera_proximity = 
						exp2(-1 * (unity_FogParams.x * unity_FogParams.x) * squared_distance_to_camera);

					result.rgb = lerp(unity_FogColor.rgb, result.rgb, saturate(camera_proximity));
				}

				return result;
			}

			ENDCG
		}
	}
}
