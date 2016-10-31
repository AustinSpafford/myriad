float4x4 build_matrix_from_columns(
	float4 x_basis,
	float4 y_basis,
	float4 z_basis,
	float4 w_basis)
{
	return float4x4(
		x_basis.x, y_basis.x, z_basis.x, w_basis.x,
		x_basis.y, y_basis.y, z_basis.y, w_basis.y,
		x_basis.z, y_basis.z, z_basis.z, w_basis.z,
		x_basis.w, y_basis.w, z_basis.w, w_basis.w);
}

float4x4 build_transform_from_components(
	float3 position,
	float3 local_forward,
	float3 local_up,
	float3 local_right,
	float scale)
{
	return build_matrix_from_columns(
		float4((scale * local_right), 0.0f),
		float4((scale * local_up), 0.0f),
		float4((scale * local_forward), 0.0f),
		float4(position, 1.0f));
}

void ortho_normalize_basis_vectors(
	inout float3 local_forward, // NOTE: Assumed to already be normalized.
	inout float3 inout_local_up,
	out float3 out_local_right)
{
	local_forward = normalize(local_forward);

	out_local_right = normalize(cross(inout_local_up, local_forward));

	inout_local_up = cross(local_forward, out_local_right);
}

float3 rotate_vector_about_axis_via_relative_ortho_normals(
	float3 input_vector,
	float theta_radians,
	float3 ortho_normal_alpha,
	float3 ortho_normal_bravo)
{
	// Project down into the plane orthogonal to the axis of rotation.
	float2 projection = float2(
		dot(input_vector, ortho_normal_alpha),
		dot(input_vector, ortho_normal_bravo));

	// Remember how much of the input vector is unaffected by the rotation.
	float3 input_invariant_component =
		input_vector -
		(
			(projection[0] * ortho_normal_alpha) +
			(projection[1] * ortho_normal_bravo)
		);

	// Rotate the projection.
	{
		float cos_theta = cos(theta_radians);
		float sin_theta = sin(theta_radians);

		projection = float2(
			(projection[0] * cos_theta) - (projection[1] * sin_theta),
			(projection[0] * sin_theta) + (projection[1] * cos_theta));
	}

	float3 result = (
		input_invariant_component +
		(projection[0] * ortho_normal_alpha) +
		(projection[1] * ortho_normal_bravo));

	return result;
}


