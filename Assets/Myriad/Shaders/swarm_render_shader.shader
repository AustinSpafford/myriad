Shader "Myriad/orbiters_render_shader"
{
	Properties
	{
		u_main_texture("Texture", 2D) = "white" {}
		u_color("Color", Color) = (1, 1, 1, 1)
		u_radius("Radius", Float) = 0.1

		// Constants that our parent can query.
		// NOTE: When this value changes, unfortunately materials referencing this shader will have to be updated, as there's currently no support for [NonSerialized].
		[HideInInspector] k_vertices_per_orbiter("<hidden>", Int) = 6
	}

	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }

		LOD 100

		Pass
		{
			CGPROGRAM

			// TODO: Try targetting just "4.5", since that seems to be the actual minimum needed.
			// https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
			#pragma target 5.0

			// Identify our entry-point functions.
			#pragma vertex vertex_shader
			#pragma geometry geometry_shader
			#pragma fragment fragment_shader

			// Enable support for fog-calculations.
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct s_orbiter_state
			{
				float3 position;
				float3 velocity;
				float3 acceleration;
			};

			struct s_vertex
			{
				float4 position : SV_POSITION;
				float3 normal : NORMAL;
				float3 tangent : TANGENT;
				float3 binormal : BINORMAL;
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

			uniform sampler2D u_main_texture;
			uniform float4 u_main_texture_ST; // Contains texture's (scale.x, scale.y, offset.x, offset.y)
			uniform float4 u_color;
			uniform float u_radius;

			uniform StructuredBuffer<s_orbiter_state> u_orbiters;

			uniform float4x4 u_model_to_world_matrix; // We have to supply this manually because DrawProcedural is outside the normal mesh-rendering pipeline, so "unity_ObjectToWorld" is unfortunately always the identity matrix.
			
			s_vertex vertex_shader(
				uint orbiter_index : SV_VertexID)
			{
				s_vertex result;
				
				result.position = float4(u_orbiters[orbiter_index].position, 1.0f);

				// Keep the nose of the orbiter pointed in its direction of motion.
				result.binormal = normalize(u_orbiters[orbiter_index].velocity);

				// Set the orbiter's roll such that "down" points to the center of attraction.
				result.tangent =
					normalize(cross(
						result.binormal,
						(-1.0f * u_orbiters[orbiter_index].acceleration)));

				// The normal is now strictly implied.
				result.normal = cross(result.tangent, result.binormal);

				// Initializing all remaining members to silence compilation failures.
				result.uv = float2(0.0f, 0.0f);

				return result;
			}

			float4 build_geometry_vertex_position(
				float3 vertex_in_instance_space,
				float4 instance_position_in_model_space,
				float3 instance_x_axis,
				float3 instance_y_axis,
				float3 instance_z_axis)
			{
				float3 result = instance_position_in_model_space.xyz;

				result += (vertex_in_instance_space.x * instance_x_axis);
				result += (vertex_in_instance_space.y * instance_y_axis);
				result += (vertex_in_instance_space.z * instance_z_axis);

				return float4(result, instance_position_in_model_space.w);
			}

			[maxvertexcount(6)]
			void geometry_shader(
				point s_vertex source_vertex[1],
				inout TriangleStream<s_vertex> triangle_stream)
			{
				float4x4 model_to_projection_matrix = 
					mul(UNITY_MATRIX_VP, u_model_to_world_matrix);

				s_vertex scratch_vertex;
				scratch_vertex.normal = source_vertex[0].normal;
				scratch_vertex.tangent = source_vertex[0].tangent;
				scratch_vertex.binormal = source_vertex[0].binormal;

				float4 forward_position = 
					mul(
						model_to_projection_matrix,
						build_geometry_vertex_position(
							float3(0.0f, 0.0f, u_radius),
							source_vertex[0].position,
							scratch_vertex.tangent,
							scratch_vertex.normal,
							scratch_vertex.binormal));

				float4 right_position = 
					mul(
						model_to_projection_matrix,
						build_geometry_vertex_position(
							float3(u_radius, 0.0f, (-1.0f * u_radius)),
							source_vertex[0].position,
							scratch_vertex.tangent,
							scratch_vertex.normal,
							scratch_vertex.binormal));
				
				float4 left_position = 
					mul(
						model_to_projection_matrix,
						build_geometry_vertex_position(
							float3((-1.0f * u_radius), 0.0f, (-1.0f * u_radius)),
							source_vertex[0].position,
							scratch_vertex.tangent,
							scratch_vertex.normal,
							scratch_vertex.binormal));

				// Emit the top-face.
				{
					scratch_vertex.position = forward_position;
					scratch_vertex.uv = TRANSFORM_TEX(float2(0.5f, 1.0f), u_main_texture);
					UNITY_TRANSFER_FOG(scratch_vertex, scratch_vertex.position);
					triangle_stream.Append(scratch_vertex);
					
					scratch_vertex.position = right_position;
					scratch_vertex.uv = TRANSFORM_TEX(float2(1.0f, 0.0f), u_main_texture);
					UNITY_TRANSFER_FOG(scratch_vertex, scratch_vertex.position);
					triangle_stream.Append(scratch_vertex);
					
					scratch_vertex.position = left_position;
					scratch_vertex.uv = TRANSFORM_TEX(float2(0.0f, 0.0f), u_main_texture);
					UNITY_TRANSFER_FOG(scratch_vertex, scratch_vertex.position);
					triangle_stream.Append(scratch_vertex);
					
					triangle_stream.RestartStrip();
				}

				// Emit the bottom-face.
				{
					scratch_vertex.normal *= -1.0f;
					scratch_vertex.tangent *= -1.0f;

					scratch_vertex.position = forward_position;
					scratch_vertex.uv = TRANSFORM_TEX(float2(0.5f, 1.0f), u_main_texture);
					UNITY_TRANSFER_FOG(scratch_vertex, scratch_vertex.position);
					triangle_stream.Append(scratch_vertex);
					
					scratch_vertex.position = left_position;
					scratch_vertex.uv = TRANSFORM_TEX(float2(0.0f, 0.0f), u_main_texture);
					UNITY_TRANSFER_FOG(scratch_vertex, scratch_vertex.position);
					triangle_stream.Append(scratch_vertex);
					
					scratch_vertex.position = right_position;
					scratch_vertex.uv = TRANSFORM_TEX(float2(1.0f, 0.0f), u_main_texture);
					UNITY_TRANSFER_FOG(scratch_vertex, scratch_vertex.position);
					triangle_stream.Append(scratch_vertex);
					
					triangle_stream.RestartStrip();

					scratch_vertex.normal *= -1.0f;
					scratch_vertex.tangent *= -1.0f;
				}
			}
			
			fixed4 fragment_shader(
				s_vertex interpolated_vertex) : 
					SV_Target
			{
				fixed4 result = (
					tex2D(u_main_texture, interpolated_vertex.uv) *
					u_color);
				
				UNITY_APPLY_FOG(interpolated_vertex.fogCoord, result);

				return result;
			}

			ENDCG
		}
	}
}
