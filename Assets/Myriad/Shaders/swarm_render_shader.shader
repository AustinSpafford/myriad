Shader "Myriad/swarm_render_shader"
{
	Properties
	{
		u_main_texture("Texture", 2D) = "white" {}
		u_color("Color", Color) = (1, 1, 1, 1)
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

			// Enable support for fog-calculations.
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			#include "swarm_shader_types.cginc"
			
			struct s_rasterization_vertex
			{
				float4 position : SV_POSITION;
				float4 normal : NORMAL;
				float4 albedo_color : COLOR0;
				float2 texture_coord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
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

				float4x4 model_to_perspective_matrix = 
					mul(UNITY_MATRIX_VP, 
					mul(u_swarm_to_world_matrix, swarmer_state.cached_model_to_swarm_matrix));

				result.position = mul(model_to_perspective_matrix, float4(model_vertex.position, 1.0f));
				result.normal = mul(model_to_perspective_matrix, float4(model_vertex.normal, 0.0f));
				result.albedo_color = model_vertex.albedo_color;
				result.texture_coord = model_vertex.texture_coord;
				
				UNITY_TRANSFER_FOG(result, result.position);

				return result;
			}
			
			fixed4 fragment_shader(
				s_rasterization_vertex interpolated_vertex) : 
					SV_Target
			{
				fixed4 result = (
					tex2D(u_main_texture, interpolated_vertex.texture_coord) *
					interpolated_vertex.albedo_color *
					u_color);
				
				UNITY_APPLY_FOG(interpolated_vertex.fogCoord, result);

				return result;
			}

			ENDCG
		}
	}
}
